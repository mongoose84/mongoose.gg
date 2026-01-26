using MySqlConnector;
using RiotProxy.Application.DTOs.Overview;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for overview page statistics.
/// Provides primary queue detection, last 20 games metrics, and last match data.
/// </summary>
public class OverviewStatsRepository : RepositoryBase
{
    private readonly ILogger<OverviewStatsRepository> _logger;

    // Queue priority order for tie-breaking: Ranked Solo/Duo → Ranked Flex → Normal Draft → ARAM → other
    private static readonly Dictionary<int, int> QueuePriority = new()
    {
        { 420, 1 },  // Ranked Solo/Duo (highest priority)
        { 440, 2 },  // Ranked Flex
        { 400, 3 },  // Normal Draft
        { 430, 3 },  // Normal Blind (same priority as Draft)
        { 450, 4 },  // ARAM
    };

    public OverviewStatsRepository(IDbConnectionFactory factory, ILogger<OverviewStatsRepository> logger) : base(factory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines the primary queue based on match count in recent window (last 50 matches or 30 days).
    /// Returns the queue_id with highest match count, using tie-breaker order if counts are equal.
    /// </summary>
    public virtual async Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid)
    {
        // Get match counts per queue for last 50 matches OR last 30 days (whichever gives more games)
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();

        const string sql = @"
            SELECT 
                m.queue_id,
                COUNT(*) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND (
                  m.game_start_time >= @thirty_days_ago
                  OR p.match_id IN (
                      SELECT match_id FROM (
                          SELECT p2.match_id 
                          FROM participants p2
                          INNER JOIN matches m2 ON m2.match_id = p2.match_id
                          WHERE p2.puuid = @puuid
                          ORDER BY m2.game_start_time DESC
                          LIMIT 50
                      ) recent_matches
                  )
              )
            GROUP BY m.queue_id
            ORDER BY match_count DESC";

        var queueCounts = new List<QueueMatchCount>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@thirty_days_ago", thirtyDaysAgo);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                queueCounts.Add(new QueueMatchCount(
                    QueueId: reader.GetInt32(0),
                    MatchCount: reader.GetInt32(1)
                ));
            }
            return 0;
        });

        if (queueCounts.Count == 0)
        {
            // Default to Ranked Solo/Duo if no matches found
            return (420, "Ranked Solo/Duo", 0);
        }

        // Find queue with highest count, using priority for tie-breaking
        var primaryQueue = queueCounts
            .OrderByDescending(q => q.MatchCount)
            .ThenBy(q => QueuePriority.GetValueOrDefault(q.QueueId, 99))
            .First();

        var label = GetQueueLabel(primaryQueue.QueueId);
        return (primaryQueue.QueueId, label, primaryQueue.MatchCount);
    }

    /// <summary>
    /// Gets the last 20 matches for the specified queue with win/loss and LP data.
    /// Returns newest first (index 0 = most recent).
    /// </summary>
    public virtual async Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId)
    {
        const string sql = @"
            SELECT 
                p.match_id,
                p.win,
                p.lp_after,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND m.queue_id = @queue_id
            ORDER BY m.game_start_time DESC
            LIMIT 20";

        var matches = new List<MatchResultData>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_id", queueId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matches.Add(new MatchResultData(
                    MatchId: reader.GetString(0),
                    Win: reader.GetBoolean(1),
                    LpAfter: reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    GameStartTime: reader.GetInt64(3)
                ));
            }
            return 0;
        });

        return matches;
    }

    /// <summary>
    /// Gets the most recent match for the player across all queues.
    /// </summary>
    public virtual async Task<LastMatchData?> GetLastMatchAsync(string puuid)
    {
        const string sql = @"
            SELECT
                p.match_id,
                p.champion_id,
                p.champion_name,
                p.win,
                p.kills,
                p.deaths,
                p.assists,
                m.game_start_time,
                m.queue_id
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
            ORDER BY m.game_start_time DESC
            LIMIT 1";

        LastMatchData? result = null;

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new LastMatchData(
                    MatchId: reader.GetString(0),
                    ChampionId: reader.GetInt32(1),
                    ChampionName: reader.GetString(2),
                    Win: reader.GetBoolean(3),
                    Kills: reader.GetInt32(4),
                    Deaths: reader.GetInt32(5),
                    Assists: reader.GetInt32(6),
                    GameStartTime: reader.GetInt64(7),
                    QueueId: reader.GetInt32(8)
                );
            }
            return 0;
        });

        return result;
    }

    /// <summary>
    /// Gets the current LP for the player in the specified ranked queue.
    /// Returns null if no LP data is available.
    /// </summary>
    public virtual async Task<int?> GetCurrentLpAsync(string puuid, int queueId)
    {
        // Get the most recent LP from a ranked match
        const string sql = @"
            SELECT p.lp_after
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND m.queue_id = @queue_id
              AND p.lp_after IS NOT NULL
            ORDER BY m.game_start_time DESC
            LIMIT 1";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_id", queueId);

            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        });
    }

    /// <summary>
    /// Converts a queue ID to a human-readable label.
    /// </summary>
    public static string GetQueueLabel(int queueId) => queueId switch
    {
        420 => "Ranked Solo/Duo",
        440 => "Ranked Flex",
        400 => "Normal Draft",
        430 => "Normal Blind",
        450 => "ARAM",
        _ => $"Queue {queueId}"
    };
}

