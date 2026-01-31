using MySqlConnector;
using RiotProxy.Core.Interfaces;
using static RiotProxy.Application.DTOs.SoloSummaryDto;

namespace RiotProxy.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for trend-related statistics.
/// Provides winrate trends, LP trends, and match activity data.
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
    public async Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null)
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

        // Downsample if more than 100 data points
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

    /// <inheritdoc />
    public async Task<IList<LpTrendPoint>> GetLpTrendAsync(string puuid, string? queueType = null, int limit = 100)
    {
        // Build queue filter for ranked modes only (420 = Ranked Solo/Duo, 440 = Ranked Flex)
        var queueFilter = queueType?.ToLowerInvariant() switch
        {
            "ranked_solo" => "AND m.queue_id = 420",
            "ranked_flex" => "AND m.queue_id = 440",
            _ => "AND m.queue_id IN (420, 440)"
        };

        var sql = $@"
            SELECT
                p.lp_after,
                p.tier_after,
                p.rank_after,
                p.win,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND p.lp_after IS NOT NULL
              AND p.tier_after IS NOT NULL
              {queueFilter}
            ORDER BY m.game_start_time ASC
            LIMIT @limit";

        var points = new List<LpTrendPoint>();

        await ExecuteWithConnectionAsync<int>(async (conn, cmd) =>
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();

            int gameIndex = 1;
            int? previousLp = null;
            string? previousTier = null;
            string? previousRank = null;

            while (await reader.ReadAsync())
            {
                var lpAfter = reader.GetInt32(0);
                var tierAfter = reader.GetString(1);
                var rankAfter = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var win = reader.GetBoolean(3);
                var gameStartTime = reader.GetInt64(4);

                var rankString = FormatRankString(tierAfter, rankAfter);
                var isPromotion = DetectPromotion(previousTier, previousRank, tierAfter, rankAfter);
                var isDemotion = DetectDemotion(previousTier, previousRank, tierAfter, rankAfter);

                int? lpGain = null;
                if (previousLp.HasValue && !isPromotion && !isDemotion)
                {
                    lpGain = lpAfter - previousLp.Value;
                }

                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(gameStartTime).UtcDateTime;

                points.Add(new LpTrendPoint(
                    GameIndex: gameIndex,
                    LpGain: lpGain,
                    CurrentLp: lpAfter,
                    Rank: rankString,
                    Timestamp: timestamp,
                    IsPromotion: isPromotion,
                    IsDemotion: isDemotion,
                    Win: win
                ));

                previousLp = lpAfter;
                previousTier = tierAfter;
                previousRank = rankAfter;
                gameIndex++;
            }

            return 0;
        });

        return points;
    }

    private static string FormatRankString(string tier, string rank)
    {
        var formattedTier = tier.Length > 0
            ? char.ToUpper(tier[0]) + tier.Substring(1).ToLower()
            : tier;
        return string.IsNullOrEmpty(rank) ? formattedTier : $"{formattedTier} {rank}";
    }

    private static bool DetectPromotion(string? prevTier, string? prevRank, string currTier, string currRank)
    {
        if (string.IsNullOrEmpty(prevTier)) return false;

        var prevTierLevel = GetTierLevel(prevTier);
        var currTierLevel = GetTierLevel(currTier);

        if (currTierLevel > prevTierLevel) return true;

        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(prevRank);
            var currDivision = GetDivisionLevel(currRank);
            return currDivision > prevDivision;
        }

        return false;
    }

    private static bool DetectDemotion(string? prevTier, string? prevRank, string currTier, string currRank)
    {
        if (string.IsNullOrEmpty(prevTier)) return false;

        var prevTierLevel = GetTierLevel(prevTier);
        var currTierLevel = GetTierLevel(currTier);

        if (currTierLevel < prevTierLevel) return true;

        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(prevRank);
            var currDivision = GetDivisionLevel(currRank);
            return currDivision < prevDivision;
        }

        return false;
    }

    private static int GetTierLevel(string? tier)
    {
        return tier?.ToUpperInvariant() switch
        {
            "IRON" => 1,
            "BRONZE" => 2,
            "SILVER" => 3,
            "GOLD" => 4,
            "PLATINUM" => 5,
            "EMERALD" => 6,
            "DIAMOND" => 7,
            "MASTER" => 8,
            "GRANDMASTER" => 9,
            "CHALLENGER" => 10,
            _ => 0
        };
    }

    private static int GetDivisionLevel(string? rank)
    {
        return rank?.ToUpperInvariant() switch
        {
            "IV" => 1,
            "III" => 2,
            "II" => 3,
            "I" => 4,
            _ => 0
        };
    }
}

