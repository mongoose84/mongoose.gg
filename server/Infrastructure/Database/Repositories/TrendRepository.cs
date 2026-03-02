using MySqlConnector;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Application.Endpoints.Shared;
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
        => await GetWinrateTrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<WinrateTrendPoint[]> GetWinrateTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        if (puuids.Count == 0)
            return Array.Empty<WinrateTrendPoint>();

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

        // Fetch all games in chronological order (oldest first)
        var sql = $@"
            SELECT
                p.win,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate} {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var games = new List<(bool Win, long Timestamp)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
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
        => await GetGoldAt15TrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        if (puuids.Count == 0)
            return Array.Empty<GoldAt15TrendPoint>();

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

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
            WHERE {puuidPredicate} {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var dataPoints = new List<GoldAt15TrendPoint>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
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
        => await GetCsPerMinuteTrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        if (puuids.Count == 0)
            return Array.Empty<CsPerMinuteTrendPoint>();

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

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
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= 900 {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var dataPoints = new List<CsPerMinuteTrendPoint>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
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
    public async Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        => await GetDeathsTrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        if (puuids.Count == 0)
            return (Array.Empty<DeathsTrendPoint>(), 0, 0, "neutral");

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

        // Query to get death counts per game with champion and role information
        var sql = $@"
            SELECT
                p.match_id,
                m.game_start_time,
                p.deaths,
                p.champion_name,
                p.role,
                m.game_duration_sec
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate} {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var dataPoints = new List<(string MatchId, long Timestamp, int Deaths, string ChampionName, string? Role, int GameDurationSec)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var matchId = reader.GetString(0);
                var timestamp = reader.GetInt64(1);
                var deaths = reader.GetInt32(2);
                var championName = reader.GetString(3);
                var role = reader.IsDBNull(4) ? null : reader.GetString(4);
                var gameDurationSec = reader.GetInt32(5);

                dataPoints.Add((matchId, timestamp, deaths, championName, role, gameDurationSec));
            }
            return 0;
        });

        if (dataPoints.Count == 0)
            return (Array.Empty<DeathsTrendPoint>(), 0, 0, "neutral");

        // Calculate rolling 10-game average for each game
        const int windowSize = 10;
        var trendPoints = new List<DeathsTrendPoint>();

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowGames = dataPoints.Skip(windowStart).Take(i - windowStart + 1).ToList();

            var totalDeaths = windowGames.Sum(g => g.Deaths);
            var rollingAverage = Math.Round((double)totalDeaths / windowGames.Count, 1);

            var point = dataPoints[i];
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(point.Timestamp).UtcDateTime;
            var gameDurationMinutes = Math.Round(point.GameDurationSec / 60.0, 1);

            trendPoints.Add(new DeathsTrendPoint(
                MatchId: point.MatchId,
                GameIndex: i + 1,
                Timestamp: timestamp,
                Deaths: point.Deaths,
                RollingAverage: rollingAverage,
                ChampionName: point.ChampionName,
                Role: point.Role,
                GameDurationMinutes: gameDurationMinutes
            ));
        }

        // Calculate summary statistics
        var overallAverage = Math.Round(dataPoints.Average(d => d.Deaths), 1);
        var recentCount = Math.Min(20, dataPoints.Count);
        var recentAverage = Math.Round(dataPoints.TakeLast(recentCount).Average(d => d.Deaths), 1);

        // Determine trend: improving if recent deaths are lower, worsening if higher
        var trend = "neutral";
        if (recentAverage < overallAverage - 0.5)
            trend = "improving";
        else if (recentAverage > overallAverage + 0.5)
            trend = "worsening";

        // If limit is specified, return the most recent N games at full resolution
        DeathsTrendPoint[] resultPoints;
        if (limit.HasValue && limit.Value > 0)
        {
            var limitValue = Math.Min(limit.Value, trendPoints.Count);
            resultPoints = trendPoints.TakeLast(limitValue).ToArray();
        }
        else
        {
            // Downsample if more than 100 data points (only when no limit specified)
            const int maxDataPoints = 100;
            if (trendPoints.Count > maxDataPoints)
            {
                var step = (double)trendPoints.Count / maxDataPoints;
                var downsampled = new List<DeathsTrendPoint>();

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

                resultPoints = downsampled.ToArray();
            }
            else
            {
                resultPoints = trendPoints.ToArray();
            }
        }

        return (resultPoints, recentAverage, overallAverage, trend);
    }

    /// <inheritdoc />
    public async Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        => await GetDragonParticipationTrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        try
        {
            if (puuids.Count == 0)
                return (Array.Empty<DragonParticipationTrendPoint>(), 0, 0, "neutral");

            queueType = _filterBuilder.ValidateQueueType(queueType);
            var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
            var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
            var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
            var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

            // Query to get dragon participation per game with team dragon counts
            // Use LEFT JOIN to include matches even if objective data is missing
            var sql = $@"
                SELECT
                    p.match_id,
                    m.game_start_time,
                    p.champion_name,
                    p.role,
                    COALESCE(po.dragons_participated, 0) as dragons_participated,
                    COALESCE(tobj.dragons_taken, 0) as dragons_taken
                FROM participants p
                INNER JOIN matches m ON m.match_id = p.match_id
                LEFT JOIN participant_objectives po ON po.participant_id = p.id
                LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
                WHERE {puuidPredicate} {queueFilter} {timeFilter}
                ORDER BY m.game_start_time ASC";

            _logger.LogDebug("Dragon participation SQL: {Sql}", sql);

            var dataPoints = new List<(string MatchId, long Timestamp, string ChampionName, string? Role, int TeamDragons, int DragonsParticipated)>();

            await ExecuteWithConnectionAsync(async conn =>
            {
                await using var cmd = new MySqlCommand(sql, conn);
                foreach (var (name, value) in puuidParams)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }
                _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var matchId = reader.GetString(0);
                    var timestamp = reader.GetInt64(1);
                    var championName = reader.GetString(2);
                    var role = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var dragonsParticipated = reader.GetInt32(4);
                    var dragonsTaken = reader.GetInt32(5);

                    dataPoints.Add((matchId, timestamp, championName, role, dragonsTaken, dragonsParticipated));
                }
                return 0;
            });

            _logger.LogDebug("Dragon participation: Retrieved {Count} dataPoints", dataPoints.Count);

            if (dataPoints.Count == 0)
                return (Array.Empty<DragonParticipationTrendPoint>(), 0, 0, "neutral");

            // Calculate rolling 20-game average for each game
            // Include games with 0 team dragons to show poor objective control
            const int windowSize = 20;
            var trendPoints = new List<DragonParticipationTrendPoint>();

            for (int i = 0; i < dataPoints.Count; i++)
            {
                var windowStart = Math.Max(0, i - windowSize + 1);
                var windowGames = dataPoints.Skip(windowStart).Take(i - windowStart + 1).ToList();

                // Calculate rolling average participation rate
                var totalTeamDragons = windowGames.Sum(g => g.TeamDragons);
                var totalParticipated = windowGames.Sum(g => g.DragonsParticipated);
                var rollingAverage = totalTeamDragons > 0 
                    ? Math.Round((double)totalParticipated / totalTeamDragons * 100, 1)
                    : 0;

                var point = dataPoints[i];
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(point.Timestamp).UtcDateTime;
                // If team got 0 dragons, show 0% participation (indicates poor objective control)
                var participationRate = point.TeamDragons > 0
                    ? Math.Round((double)point.DragonsParticipated / point.TeamDragons * 100, 1)
                    : 0;

                trendPoints.Add(new DragonParticipationTrendPoint(
                    MatchId: point.MatchId,
                    GameIndex: i + 1,
                    Timestamp: timestamp,
                    TeamDragons: point.TeamDragons,
                    DragonsParticipated: point.DragonsParticipated,
                    ParticipationRate: participationRate,
                    RollingAverage: rollingAverage,
                    ChampionName: point.ChampionName,
                    Role: point.Role
                ));
            }

            // Calculate summary statistics (include all games to show complete picture)
            var overallTeamDragons = dataPoints.Sum(d => d.TeamDragons);
            var overallParticipated = dataPoints.Sum(d => d.DragonsParticipated);
            var overallAverage = overallTeamDragons > 0 
                ? Math.Round((double)overallParticipated / overallTeamDragons * 100, 1)
                : 0;

            var recentCount = Math.Min(20, dataPoints.Count);
            var recentGames = dataPoints.TakeLast(recentCount).ToList();
            var recentTeamDragons = recentGames.Sum(d => d.TeamDragons);
            var recentParticipated = recentGames.Sum(d => d.DragonsParticipated);
            var recentAverage = recentTeamDragons > 0 
                ? Math.Round((double)recentParticipated / recentTeamDragons * 100, 1)
                : 0;

            // Determine trend: improving if recent participation is higher
            var trend = "neutral";
            if (recentAverage > overallAverage + 5)
                trend = "improving";
            else if (recentAverage < overallAverage - 5)
                trend = "worsening";

            // If limit is specified, return the most recent N games at full resolution
            DragonParticipationTrendPoint[] resultPoints;
            if (limit.HasValue && limit.Value > 0)
            {
                var limitValue = Math.Min(limit.Value, trendPoints.Count);
                resultPoints = trendPoints.TakeLast(limitValue).ToArray();
            }
            else
            {
                // Downsample if more than 100 data points (only when no limit specified)
                const int maxDataPoints = 100;
                if (trendPoints.Count > maxDataPoints)
                {
                    var step = (double)trendPoints.Count / maxDataPoints;
                    var downsampled = new List<DragonParticipationTrendPoint>();

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

                    resultPoints = downsampled.ToArray();
                }
                else
                {
                    resultPoints = trendPoints.ToArray();
                }
            }

            return (resultPoints, recentAverage, overallAverage, trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDragonParticipationTrendAsync for accountCount={AccountCount}, queueType={QueueType}, timeRange={TimeRange}", 
                puuids.Count, LogSanitizer.Sanitize(queueType), LogSanitizer.Sanitize(timeRange));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> GetVisionScoreTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        => await GetVisionScoreTrendAsync([puuid], queueType, timeRange, limit);

    /// <inheritdoc />
    public async Task<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> GetVisionScoreTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null)
    {
        try
        {
            if (puuids.Count == 0)
                return (Array.Empty<VisionScoreTrendPoint>(), 0, 0, 1.0, "neutral");

            queueType = _filterBuilder.ValidateQueueType(queueType);
            var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
            var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
            var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
            var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

            // Query to get vision score data
            // When limit is provided, fetch limit + windowSize - 1 rows (need extra rows for rolling average)
            // Use DESC order with LIMIT for efficiency, then reverse to ASC in-memory
            const int windowSize = 20;
            var useDbLimit = limit.HasValue && limit.Value > 0;
            var dbLimit = useDbLimit ? limit!.Value + windowSize - 1 : int.MaxValue;
            var orderDirection = useDbLimit ? "DESC" : "ASC";
            var limitClause = useDbLimit ? $"LIMIT {dbLimit}" : "";

            var sql = $@"
                SELECT
                    p.match_id,
                    m.game_start_time,
                    pm.vision_score,
                    m.game_duration_sec,
                    p.champion_name,
                    p.role
                FROM participants p
                INNER JOIN matches m ON m.match_id = p.match_id
                INNER JOIN participant_metrics pm ON pm.participant_id = p.id
                                WHERE {puuidPredicate} {queueFilter} {timeFilter}
                  AND m.game_duration_sec >= 600
                ORDER BY m.game_start_time {orderDirection}
                {limitClause}";

            var dataPoints = new List<(
                string MatchId,
                long Timestamp,
                int VisionScore,
                int GameDuration,
                string ChampionName,
                string? Role
            )>();

            await ExecuteWithConnectionAsync(async conn =>
            {
                await using var cmd = new MySqlCommand(sql, conn);
                foreach (var (name, value) in puuidParams)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }
                _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var matchId = reader.GetString(0);
                    var timestamp = reader.GetInt64(1);
                    var visionScore = reader.GetInt32(2);
                    var gameDuration = reader.GetInt32(3);
                    var championName = reader.GetString(4);
                    var role = reader.IsDBNull(5) ? null : reader.GetString(5);

                    dataPoints.Add((matchId, timestamp, visionScore, gameDuration, championName, role));
                }
                return 0;
            });

            // If we fetched in DESC order (for DB efficiency), reverse to ASC for chronological processing
            if (useDbLimit)
            {
                dataPoints.Reverse();
            }

            _logger.LogDebug("Vision score: Retrieved {Count} dataPoints", dataPoints.Count);

            if (dataPoints.Count == 0)
                return (Array.Empty<VisionScoreTrendPoint>(), 0, 0, 1.0, "neutral");

            // Determine role target based on most common role (Support = 2.0, others = 1.0)
            var roleCounts = dataPoints
                .Where(d => !string.IsNullOrEmpty(d.Role))
                .GroupBy(d => d.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var mostCommonRole = roleCounts.FirstOrDefault()?.Role;
            var roleTarget = (mostCommonRole?.Equals("UTILITY", StringComparison.OrdinalIgnoreCase) == true) ? 2.0 : 1.0;

            // Calculate rolling 20-game average for each game
            var trendPoints = new List<VisionScoreTrendPoint>();

            for (int i = 0; i < dataPoints.Count; i++)
            {
                var windowStart = Math.Max(0, i - windowSize + 1);
                var windowGames = dataPoints.Skip(windowStart).Take(i - windowStart + 1).ToList();

                // Calculate rolling average vision score per minute
                var totalVisionPerMin = windowGames.Sum(g => (double)g.VisionScore / (g.GameDuration / 60.0));
                var rollingAverage = Math.Round(totalVisionPerMin / windowGames.Count, 2);

                var point = dataPoints[i];
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(point.Timestamp).UtcDateTime;
                var visionScorePerMinute = Math.Round((double)point.VisionScore / (point.GameDuration / 60.0), 2);
                var gameDurationMinutes = Math.Round(point.GameDuration / 60.0, 1);

                trendPoints.Add(new VisionScoreTrendPoint(
                    MatchId: point.MatchId,
                    GameIndex: i + 1,
                    Timestamp: timestamp,
                    VisionScore: point.VisionScore,
                    VisionScorePerMinute: visionScorePerMinute,
                    RollingAverage: rollingAverage,
                    GameDurationMinutes: gameDurationMinutes,
                    ChampionName: point.ChampionName,
                    Role: point.Role
                ));
            }

            // Calculate summary statistics
            var overallTotalVisionPerMin = dataPoints.Sum(d => (double)d.VisionScore / (d.GameDuration / 60.0));
            var overallAverage = Math.Round(overallTotalVisionPerMin / dataPoints.Count, 2);

            var recentCount = Math.Min(20, dataPoints.Count);
            var recentGames = dataPoints.TakeLast(recentCount).ToList();
            var recentTotalVisionPerMin = recentGames.Sum(d => (double)d.VisionScore / (d.GameDuration / 60.0));
            var recentAverage = Math.Round(recentTotalVisionPerMin / recentGames.Count, 2);

            // Determine trend: improving if recent vision per minute is higher
            var trend = "neutral";
            if (recentAverage > overallAverage + 0.1)
                trend = "improving";
            else if (recentAverage < overallAverage - 0.1)
                trend = "worsening";

            // Apply final result limiting and downsampling
            VisionScoreTrendPoint[] resultPoints;
            if (limit.HasValue && limit.Value > 0)
            {
                // When limit was provided, we already fetched limited data from DB
                // Just take the last N points (may be less than limit if not enough history)
                var limitValue = Math.Min(limit.Value, trendPoints.Count);
                resultPoints = trendPoints.TakeLast(limitValue).ToArray();
            }
            else
            {
                // Downsample if more than 100 data points (only when no limit specified)
                const int maxDataPoints = 100;
                if (trendPoints.Count > maxDataPoints)
                {
                    var step = (double)trendPoints.Count / maxDataPoints;
                    var downsampled = new List<VisionScoreTrendPoint>();

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

                    resultPoints = downsampled.ToArray();
                }
                else
                {
                    resultPoints = trendPoints.ToArray();
                }
            }

            return (resultPoints, recentAverage, overallAverage, roleTarget, trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetVisionScoreTrendAsync for accountCount={AccountCount}, queueType={QueueType}, timeRange={TimeRange}", 
                puuids.Count, LogSanitizer.Sanitize(queueType), LogSanitizer.Sanitize(timeRange));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
        => await GetDailyMatchCountsAsync([puuid], daysBack);

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(IReadOnlyList<string> puuids, int daysBack = 91)
    {
        if (puuids.Count == 0)
            return new Dictionary<string, int>();

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);
        var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

        var sql = $@"
            SELECT
                DATE(FROM_UNIXTIME(m.game_start_time / 1000)) as game_date,
                COUNT(DISTINCT m.match_id) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_start_time >= @start_timestamp
            GROUP BY DATE(FROM_UNIXTIME(m.game_start_time / 1000))
            ORDER BY game_date ASC";

        var result = new Dictionary<string, int>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
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

