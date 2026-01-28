using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class LpSnapshotsRepository : RepositoryBase, ILpSnapshotsRepository
{
    public LpSnapshotsRepository(IDbConnectionFactory factory) : base(factory) { }

    /// <summary>
    /// Inserts a new LP snapshot. Always inserts (no upsert) since each snapshot is a unique point in time.
    /// </summary>
    public virtual Task<long> InsertAsync(LpSnapshot snapshot)
    {
        const string sql = @"INSERT INTO lp_snapshots
            (puuid, queue_type, tier, division, lp, recorded_at, created_at)
            VALUES (@puuid, @queue_type, @tier, @division, @lp, @recorded_at, @created_at);";

        return ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", snapshot.Puuid);
            cmd.Parameters.AddWithValue("@queue_type", snapshot.QueueType);
            cmd.Parameters.AddWithValue("@tier", snapshot.Tier);
            cmd.Parameters.AddWithValue("@division", snapshot.Division);
            cmd.Parameters.AddWithValue("@lp", snapshot.Lp);
            cmd.Parameters.AddWithValue("@recorded_at", snapshot.RecordedAt);
            cmd.Parameters.AddWithValue("@created_at", snapshot.CreatedAt == default ? DateTime.UtcNow : snapshot.CreatedAt);

            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    /// <summary>
    /// Gets LP snapshots for a player in a specific queue, ordered by recorded_at descending.
    /// </summary>
    public virtual async Task<IList<LpSnapshot>> GetByPuuidAndQueueAsync(string puuid, string queueType, int limit = 100)
    {
        const string sql = @"SELECT id, puuid, queue_type, tier, division, lp, recorded_at, created_at
            FROM lp_snapshots
            WHERE puuid = @puuid AND queue_type = @queue_type
            ORDER BY recorded_at DESC
            LIMIT @limit";

        var snapshots = new List<LpSnapshot>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_type", queueType);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                snapshots.Add(Map(reader));
            }
            return 0;
        });
        return snapshots;
    }

    /// <summary>
    /// Gets the most recent LP snapshot for a player in a specific queue.
    /// Returns null if no snapshot exists.
    /// </summary>
    public virtual async Task<LpSnapshot?> GetLatestByPuuidAndQueueAsync(string puuid, string queueType)
    {
        const string sql = @"SELECT id, puuid, queue_type, tier, division, lp, recorded_at, created_at
            FROM lp_snapshots
            WHERE puuid = @puuid AND queue_type = @queue_type
            ORDER BY recorded_at DESC
            LIMIT 1";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_type", queueType);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        });
    }

    /// <summary>
    /// Gets all LP snapshots for a player (both queues), ordered by recorded_at descending.
    /// </summary>
    public virtual async Task<IList<LpSnapshot>> GetByPuuidAsync(string puuid, int limit = 100)
    {
        const string sql = @"SELECT id, puuid, queue_type, tier, division, lp, recorded_at, created_at
            FROM lp_snapshots
            WHERE puuid = @puuid
            ORDER BY recorded_at DESC
            LIMIT @limit";

        var snapshots = new List<LpSnapshot>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                snapshots.Add(Map(reader));
            }
            return 0;
        });
        return snapshots;
    }

    /// <summary>
    /// Gets the LP snapshot closest to (but not after) a specific timestamp for a player and queue.
    /// Used to find "what was the LP at the time of match X".
    /// Returns null if no snapshot exists before the given timestamp.
    /// </summary>
    public virtual async Task<LpSnapshot?> GetSnapshotAtOrBeforeAsync(string puuid, string queueType, DateTime timestamp)
    {
        const string sql = @"SELECT id, puuid, queue_type, tier, division, lp, recorded_at, created_at
            FROM lp_snapshots
            WHERE puuid = @puuid AND queue_type = @queue_type AND recorded_at <= @timestamp
            ORDER BY recorded_at DESC
            LIMIT 1";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_type", queueType);
            cmd.Parameters.AddWithValue("@timestamp", timestamp);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        });
    }

    /// <summary>
    /// Gets the oldest LP snapshot for a player in a specific queue.
    /// Used as a fallback when no snapshot exists before a specific timestamp.
    /// Returns null if no snapshots exist.
    /// </summary>
    public virtual async Task<LpSnapshot?> GetOldestByPuuidAndQueueAsync(string puuid, string queueType)
    {
        const string sql = @"SELECT id, puuid, queue_type, tier, division, lp, recorded_at, created_at
            FROM lp_snapshots
            WHERE puuid = @puuid AND queue_type = @queue_type
            ORDER BY recorded_at ASC
            LIMIT 1";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_type", queueType);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        });
    }

    private static LpSnapshot Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Puuid = r.GetString(1),
        QueueType = r.GetString(2),
        Tier = r.GetString(3),
        Division = r.GetString(4),
        Lp = r.GetInt32(5),
        RecordedAt = r.GetDateTime(6),
        CreatedAt = r.GetDateTime(7)
    };
}

