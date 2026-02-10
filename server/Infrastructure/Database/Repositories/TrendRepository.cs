using MySqlConnector;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for trend-related statistics.
/// Provides winrate trends, LP trends, and match activity data.
/// </summary>
public class TrendRepository : RepositoryBase, ITrendRepository
{
    private readonly ILogger<TrendRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;
    private readonly ILpCalculationService _lpCalc;

    public TrendRepository(
        IDbConnectionFactory factory,
        ILogger<TrendRepository> logger,
        IQueryFilterBuilder filterBuilder,
        ILpCalculationService lpCalc) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
        _lpCalc = lpCalc;
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
    /// <remarks>
    /// Uses lp_snapshots table which records LP at each sync time.
    /// This provides accurate LP progression data independent of specific matches.
    /// Note: Win field is always false since snapshots are not tied to specific games.
    /// The frontend should not rely on win/loss coloring for LP chart points.
    /// </remarks>
    public async Task<IList<LpTrendPoint>> GetLpTrendAsync(string puuid, string? queueType = null, int limit = 100)
    {
        // Build queue filter for ranked modes
        var queueTypeFilter = queueType?.ToLowerInvariant() switch
        {
            "ranked_solo" => "AND queue_type = 'RANKED_SOLO_5x5'",
            "ranked_flex" => "AND queue_type = 'RANKED_FLEX_SR'",
            _ => "" // All ranked queues
        };

        // Query lp_snapshots table: get most recent N rows, then order ascending for chart display
        // Uses subquery to select most recent rows (DESC), then outer query re-orders ASC
        var sql = $@"
            SELECT lp, tier, division, recorded_at
            FROM (
                SELECT
                    lp,
                    tier,
                    division,
                    recorded_at
                FROM lp_snapshots
                WHERE puuid = @puuid
                  {queueTypeFilter}
                ORDER BY recorded_at DESC
                LIMIT @limit
            ) AS recent
            ORDER BY recorded_at ASC";

        var points = new List<LpTrendPoint>();

        await ExecuteWithConnectionAsync<int>(async (conn, cmd) =>
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();

            int snapshotIndex = 1;
            int? previousLp = null;
            string? previousTier = null;
            string? previousDivision = null;

            while (await reader.ReadAsync())
            {
                var lp = reader.GetInt32(0);
                var tier = reader.GetString(1);
                var division = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var recordedAt = reader.GetDateTimeUtc(3);

                // Skip duplicate snapshots (same LP, tier, and division as previous)
                // This filters out snapshots created by multiple logins or syncs without rank changes
                if (previousLp.HasValue &&
                    previousLp.Value == lp &&
                    previousTier == tier &&
                    previousDivision == division)
                {
                    continue; // Skip this duplicate snapshot
                }

                var rankString = _lpCalc.FormatRank(tier, division);
                var isPromotion = _lpCalc.IsPromotion(previousTier, previousDivision, tier, division);
                var isDemotion = _lpCalc.IsDemotion(previousTier, previousDivision, tier, division);

                // Calculate absolute LP for accurate chart positioning (handles promotions/demotions correctly)
                var absoluteLp = _lpCalc.CalculateAbsoluteLp(tier, division, lp);

                int? lpGain = null;
                if (previousLp.HasValue && !isPromotion && !isDemotion)
                {
                    lpGain = lp - previousLp.Value;
                }

                points.Add(new LpTrendPoint(
                    GameIndex: snapshotIndex,
                    LpGain: lpGain,
                    CurrentLp: lp,
                    AbsoluteLp: absoluteLp,
                    Rank: rankString,
                    Timestamp: recordedAt,
                    IsPromotion: isPromotion,
                    IsDemotion: isDemotion,
                    Win: false // Snapshots are not tied to specific games
                ));

                previousLp = lp;
                previousTier = tier;
                previousDivision = division;
                snapshotIndex++;
            }

            return 0;
        });

        return points;
    }

    /// <inheritdoc />
    public async Task<(LpTrendPoint[] RankedSolo, LpTrendPoint[] RankedFlex)> GetLpTrendBothQueuesAsync(string puuid, int limit = 100)
    {
        // Fetch both queues in parallel
        var soloTask = GetLpTrendAsync(puuid, "ranked_solo", limit);
        var flexTask = GetLpTrendAsync(puuid, "ranked_flex", limit);

        await Task.WhenAll(soloTask, flexTask);

        return (soloTask.Result.ToArray(), flexTask.Result.ToArray());
    }
}

