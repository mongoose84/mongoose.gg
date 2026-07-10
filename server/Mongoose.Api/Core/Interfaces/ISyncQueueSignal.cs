namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Coordination signal that lets a request handler wake the background match-sync job
/// the instant work is queued, instead of waiting for the job's next poll.
///
/// The sync endpoint calls <see cref="Notify"/> after marking accounts 'pending';
/// <see cref="MatchHistorySyncJob"/> awaits <see cref="WaitAsync"/> while idle so it
/// starts draining immediately. The periodic poll remains as a fallback.
/// </summary>
public interface ISyncQueueSignal
{
    /// <summary>
    /// Signals that new work has been queued. Coalesced — multiple notifications
    /// before the job wakes are collapsed into a single wake-up.
    /// </summary>
    void Notify();

    /// <summary>
    /// Completes when a notification is (or has already been) raised since the last wake.
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken);
}
