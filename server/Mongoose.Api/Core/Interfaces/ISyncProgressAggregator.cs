namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Aggregates per-account match-sync progress into a single, user-scoped view for the
/// "Analyze all" flow. The sync endpoint opens a run; the background job's per-account
/// broadcasts feed <see cref="OnProgressAsync"/>/<see cref="OnCompleteAsync"/>/<see cref="OnErrorAsync"/>;
/// the aggregator recomputes the combined totals and pushes one message per affected user.
///
/// Interface lives in Core (methods take primitives only) so the Application-layer endpoint
/// can depend on it without referencing Infrastructure.
/// </summary>
public interface ISyncProgressAggregator
{
    /// <summary>
    /// Opens (or replaces) the aggregate run for a user covering the given accounts and
    /// immediately broadcasts an initial "syncing" state so the UI gets instant feedback.
    /// </summary>
    Task StartRunAsync(long userId, IReadOnlyList<string> puuids);

    /// <summary>Feeds a per-account progress update into any active run containing the PUUID.</summary>
    Task OnProgressAsync(string puuid, int progress, int total, string? matchId);

    /// <summary>Marks an account complete; broadcasts aggregate completion once all settle.</summary>
    Task OnCompleteAsync(string puuid, int totalSynced);

    /// <summary>Marks an account failed; the run resolves as failed once all accounts settle.</summary>
    Task OnErrorAsync(string puuid, string error);
}
