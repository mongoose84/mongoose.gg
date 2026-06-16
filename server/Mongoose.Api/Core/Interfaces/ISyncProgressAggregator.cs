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
    /// Opens (or extends) the aggregate run for a user, ensuring a slot for each given account,
    /// and immediately broadcasts an initial "syncing" state so the UI gets instant feedback.
    /// Multiple seeds (sync-all, login) and the job's per-account ensure converge on one run.
    ///
    /// Callers must seed a run for an account <em>before</em> marking that account claimable
    /// (sync_status = 'pending'); otherwise the background job can claim and complete the
    /// account before its slot exists, leaving an orphaned 'pending' slot that never settles.
    /// </summary>
    Task StartRunAsync(long userId, IReadOnlyList<string> puuids);

    /// <summary>Feeds a per-account progress update into any active run containing the PUUID.</summary>
    Task OnProgressAsync(string puuid, int progress, int total, string? matchId);

    /// <summary>Marks an account complete; broadcasts aggregate completion once all settle.</summary>
    Task OnCompleteAsync(string puuid, int totalSynced);

    /// <summary>Marks an account failed; the run resolves as failed once all accounts settle.</summary>
    Task OnErrorAsync(string puuid, string error);
}
