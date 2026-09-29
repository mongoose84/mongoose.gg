using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IRankSnapshotsRepository
{
    /// <summary>The newest stored snapshot for an account and ranked queue, if any.</summary>
    Task<RankSnapshotRecord?> GetLatestAsync(string puuid, int queueId);

    Task<long> InsertAsync(RankSnapshotRecord snapshot);

    /// <summary>Pending snapshots, oldest first; all accounts, or one when <paramref name="puuid"/> is given.</summary>
    Task<IList<RankSnapshotRecord>> GetPendingAsync(string? puuid, int limit);

    /// <summary>
    /// Stored matches of the account in the queue whose end (start + duration) lies in the window,
    /// in epoch milliseconds. Only matches long enough to count (no remakes).
    /// </summary>
    Task<IList<string>> GetMatchIdsEndedBetweenAsync(string puuid, int queueId, long fromMs, long toMs);

    Task UpdateStatusAsync(long id, string status, string? matchId);

    /// <summary>
    /// Riot accounts of active users (logged in since <paramref name="activeSinceUtc"/>) whose rank was
    /// last read before <paramref name="checkedBeforeUtc"/> or never, least recently read first.
    /// </summary>
    Task<IList<string>> GetDueAccountPuuidsAsync(DateTime activeSinceUtc, DateTime checkedBeforeUtc, int limit);

    /// <summary>Records when the account's rank was last read (riot_accounts.rank_checked_at).</summary>
    Task MarkRankCheckedAsync(string puuid, DateTime checkedAtUtc);

    /// <summary>Deletes snapshots older than the cutoff, keeping the newest per account and queue.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc);
}
