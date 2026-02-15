using MySqlConnector;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for trend-related statistics.
/// Provides winrate trends and match activity data.
/// </summary>
public class TrendRepository : RepositoryBase, ITrendRepository
{
    private readonly ILogger<TrendRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;

    public TrendRepository(
        IDbConnectionFactory factory,
        ILogger<TrendRepository> logger,
        IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

        // Fetch all games in chronological order (oldest first)
        var sql = $@"
            SELECT
                p.win,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var games = new List<(bool Win, long Timestamp)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var win = reader.GetInt32(0) == 1;
                var timestamp = reader.GetInt64(1);
                games.Add((win, timestamp));
            }
            return 0;
        });

        if (games.Count == 0)
            return Array.Empty<WinrateTrendPoint>();

        // Calculate rolling 20-game average for each game
        const int windowSize = 20;
        var trendPoints = new List<WinrateTrendPoint>();

        for (int i = 0; i < games.Count; i++)
        {
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowGames = games.Skip(windowStart).Take(i - windowStart + 1).ToList();

            var wins = windowGames.Count(g => g.Win);
            var total = windowGames.Count;
            var winRate = total > 0 ? Math.Round((double)wins / total * 100, 1) : 0;

            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(games[i].Timestamp).UtcDateTime;

            trendPoints.Add(new WinrateTrendPoint(
                GameIndex: i + 1,
                WinRate: winRate,
                Timestamp: timestamp
            ));
        }

        // If limit is specified, return the most recent N games at full resolution
        if (limit.HasValue && limit.Value > 0)
        {
            var limitValue = Math.Min(limit.Value, trendPoints.Count);
            return trendPoints.TakeLast(limitValue).ToArray();
        }

        // Downsample if more than 100 data points (only when no limit specified)
        const int maxDataPoints = 100;
        if (trendPoints.Count > maxDataPoints)
        {
            var step = (double)trendPoints.Count / maxDataPoints;
            var downsampled = new List<WinrateTrendPoint>();

            for (int i = 0; i < maxDataPoints; i++)
            {
                var index = (int)(i * step);
                if (index < trendPoints.Count)
                {
                    downsampled.Add(trendPoints[index]);
                }
            }

            // Always include the last data point
            if (downsampled.Count > 0 && downsampled[^1].GameIndex != trendPoints[^1].GameIndex)
            {
                downsampled[^1] = trendPoints[^1];
            }

            return downsampled.ToArray();
        }

        return trendPoints.ToArray();
    }

    /// <inheritdoc />
    public async Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

        // Query to get player's gold at 15 with opponent gold for lane matchup
        var sql = $@"
            SELECT
                p.match_id,
                m.game_start_time,
                pc.gold as player_gold,
                p.champion_name,
                p.role,
                opp_pc.gold as opponent_gold,
                opp_p.champion_name as opponent_champion
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            INNER JOIN participant_checkpoints pc ON pc.participant_id = p.id AND pc.minute_mark = 15
            LEFT JOIN participants opp_p ON opp_p.match_id = p.match_id 
                AND opp_p.team_id != p.team_id 
                AND opp_p.role = p.role
            LEFT JOIN participant_checkpoints opp_pc ON opp_pc.participant_id = opp_p.id AND opp_pc.minute_mark = 15
            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var dataPoints = new List<GoldAt15TrendPoint>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            int gameIndex = 1;
            while (await reader.ReadAsync())
            {
                var matchId = reader.GetString(0);
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)).UtcDateTime;
                var playerGold = reader.GetInt32(2);
                var championName = reader.GetString(3);
                var role = reader.IsDBNull(4) ? null : reader.GetString(4);
                var opponentGold = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                var opponentChampion = reader.IsDBNull(6) ? null : reader.GetString(6);
                var goldDifferential = opponentGold.HasValue ? playerGold - opponentGold.Value : (int?)null;

                dataPoints.Add(new GoldAt15TrendPoint(
                    MatchId: matchId,
                    GameIndex: gameIndex++,
                    Timestamp: timestamp,
                    PlayerGold: playerGold,
                    OpponentGold: opponentGold,
                    GoldDifferential: goldDifferential,
                    ChampionName: championName,
                    Role: role,
                    OpponentChampion: opponentChampion
                ));
            }
            return 0;
        });

        if (dataPoints.Count == 0)
            return Array.Empty<GoldAt15TrendPoint>();

        // If limit is specified, return the most recent N games at full resolution
        if (limit.HasValue && limit.Value > 0)
        {
            var limitValue = Math.Min(limit.Value, dataPoints.Count);
            return dataPoints.TakeLast(limitValue).ToArray();
        }

        // Downsample if more than 100 data points (only when no limit specified)
        const int maxDataPoints = 100;
        if (dataPoints.Count > maxDataPoints)
        {
            var step = (double)dataPoints.Count / maxDataPoints;
            var downsampled = new List<GoldAt15TrendPoint>();

            for (int i = 0; i < maxDataPoints; i++)
            {
                var index = (int)(i * step);
                if (index < dataPoints.Count)
                {
                    downsampled.Add(dataPoints[index]);
                }
            }

            // Always include the last data point
            if (downsampled.Count > 0 && downsampled[^1].GameIndex != dataPoints[^1].GameIndex)
            {
                downsampled[^1] = dataPoints[^1];
            }

            return downsampled.ToArray();
        }

        return dataPoints.ToArray();
    }

    /// <inheritdoc />
    public async Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

        // Query to get CS per minute data - filter out games shorter than 15 minutes (900 seconds)
        var sql = $@"
            SELECT
                p.match_id,
                m.game_start_time,
                p.creep_score,
                m.game_duration_sec,
                p.champion_name,
                p.role
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid 
            AND m.game_duration_sec >= 900 {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var dataPoints = new List<CsPerMinuteTrendPoint>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            int gameIndex = 1;
            while (await reader.ReadAsync())
            {
                var matchId = reader.GetString(0);
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)).UtcDateTime;
                var totalCs = reader.GetInt32(2);
                var gameDurationSec = reader.GetInt32(3);
                var championName = reader.GetString(4);
                var role = reader.IsDBNull(5) ? null : reader.GetString(5);

                var gameDurationMinutes = gameDurationSec / 60.0;
                var csPerMinute = Math.Round(totalCs / gameDurationMinutes, 1);

                dataPoints.Add(new CsPerMinuteTrendPoint(
                    MatchId: matchId,
                    GameIndex: gameIndex++,
                    Timestamp: timestamp,
                    TotalCs: totalCs,
                    CsPerMinute: csPerMinute,
                    GameDurationMinutes: Math.Round(gameDurationMinutes, 1),
                    ChampionName: championName,
                    Role: role
                ));
            }
            return 0;
        });

        if (dataPoints.Count == 0)
            return Array.Empty<CsPerMinuteTrendPoint>();

        // If limit is specified, return the most recent N games at full resolution
        if (limit.HasValue && limit.Value > 0)
        {
            var limitValue = Math.Min(limit.Value, dataPoints.Count);
            return dataPoints.TakeLast(limitValue).ToArray();
        }

        // Downsample if more than 100 data points (only when no limit specified)
        const int maxDataPoints = 100;
        if (dataPoints.Count > maxDataPoints)
        {
            var step = (double)dataPoints.Count / maxDataPoints;
            var downsampled = new List<CsPerMinuteTrendPoint>();

            for (int i = 0; i < maxDataPoints; i++)
            {
                var index = (int)(i * step);
                if (index < dataPoints.Count)
                {
                    downsampled.Add(dataPoints[index]);
                }
            }

            // Always include the last data point
            if (downsampled.Count > 0 && downsampled[^1].GameIndex != dataPoints[^1].GameIndex)
            {
                downsampled[^1] = dataPoints[^1];
            }

            return downsampled.ToArray();
        }

        return dataPoints.ToArray();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);
        var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

        var sql = @"
            SELECT
                DATE(FROM_UNIXTIME(m.game_start_time / 1000)) as game_date,
                COUNT(DISTINCT m.match_id) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND m.game_start_time >= @start_timestamp
            GROUP BY DATE(FROM_UNIXTIME(m.game_start_time / 1000))
            ORDER BY game_date ASC";

        var result = new Dictionary<string, int>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@start_timestamp", startTimestamp);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var gameDate = reader.GetDateTimeUtc(0);
                var matchCount = reader.GetInt32(1);
                result[gameDate.ToString("yyyy-MM-dd")] = matchCount;
            }
            return 0;
        });

        return result;
    }
}

