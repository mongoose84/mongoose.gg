using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// Auto-reset-event style signal backed by a <see cref="SemaphoreSlim"/>.
///
/// <see cref="Notify"/> releases at most one permit (extra notifications while a permit is
/// already pending are coalesced), so a burst of queued accounts produces a single wake-up.
/// <see cref="WaitAsync"/> consumes a permit, blocking until one is available.
/// Registered as a singleton so the request-scoped sync endpoint and the singleton
/// background job share the same instance.
/// </summary>
public sealed class SyncQueueSignal : ISyncQueueSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Notify()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake-up is already pending — coalesce. The job will drain all
            // 'pending' accounts in one pass, so a single permit is sufficient.
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _semaphore.WaitAsync(cancellationToken);
}
