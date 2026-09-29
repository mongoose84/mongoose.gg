namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// Which background jobs are using Riot right now (singleton), so the lowest-priority job (the
/// death detail backfill) can stay out of their way.
/// </summary>
public sealed class RiotBackgroundActivity
{
    private int _rankSnapshotsRunning;

    public bool RankSnapshotsRunning => Volatile.Read(ref _rankSnapshotsRunning) > 0;

    /// <summary>Marks a rank-snapshot tick as running until the returned scope is disposed.</summary>
    public IDisposable RankSnapshotsTick()
    {
        Interlocked.Increment(ref _rankSnapshotsRunning);
        return new Scope(() => Interlocked.Decrement(ref _rankSnapshotsRunning));
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) onDispose();
        }
    }
}
