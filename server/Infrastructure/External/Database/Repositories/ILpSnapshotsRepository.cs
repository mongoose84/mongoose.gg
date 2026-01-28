using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Interface for LP snapshots repository operations.
/// Provides time-series LP tracking for rank progression analysis.
/// </summary>
public interface ILpSnapshotsRepository
{
    /// <summary>
    /// Inserts a new LP snapshot. Always inserts (no upsert) since each snapshot is a unique point in time.
    /// </summary>
    Task<long> InsertAsync(LpSnapshot snapshot);

    /// <summary>
    /// Gets LP snapshots for a player in a specific queue, ordered by recorded_at descending.
    /// </summary>
    Task<IList<LpSnapshot>> GetByPuuidAndQueueAsync(string puuid, string queueType, int limit = 100);

    /// <summary>
    /// Gets the most recent LP snapshot for a player in a specific queue.
    /// Returns null if no snapshot exists.
    /// </summary>
    Task<LpSnapshot?> GetLatestByPuuidAndQueueAsync(string puuid, string queueType);

    /// <summary>
    /// Gets all LP snapshots for a player (both queues), ordered by recorded_at descending.
    /// </summary>
    Task<IList<LpSnapshot>> GetByPuuidAsync(string puuid, int limit = 100);

    /// <summary>
    /// Gets the LP snapshot closest to (but not after) a specific timestamp for a player and queue.
    /// Used to find "what was the LP at the time of match X".
    /// Returns null if no snapshot exists before the given timestamp.
    /// </summary>
    Task<LpSnapshot?> GetSnapshotAtOrBeforeAsync(string puuid, string queueType, DateTime timestamp);

    /// <summary>
    /// Gets the oldest LP snapshot for a player in a specific queue.
    /// Used as a fallback when no snapshot exists before a specific timestamp.
    /// Returns null if no snapshots exist.
    /// </summary>
    Task<LpSnapshot?> GetOldestByPuuidAndQueueAsync(string puuid, string queueType);
}

