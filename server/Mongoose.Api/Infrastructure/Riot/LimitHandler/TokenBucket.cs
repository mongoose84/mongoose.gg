using System.Collections.Concurrent;

namespace Mongoose.Api.Infrastructure.Riot.LimitHandler;

public sealed class TokenBucket : IDisposable
{
    private readonly int _capacity;
    private readonly TimeSpan _refillPeriod;
    private readonly TimeProvider _timeProvider;
    private int _tokens;
    private readonly SemaphoreSlim _semaphore;
    private readonly ITimer _timer;
    private bool _disposed;

    // Callers blocked on a token, with when they started waiting (FR 40)
    private readonly ConcurrentDictionary<long, DateTimeOffset> _waiters = new();
    private long _nextWaiterId;
    private long _nextRefillTicks;

    // Raised when a caller is about to block waiting for a token.
    // Subscribe to observe backpressure (e.g., for logging/metrics).
    public event EventHandler? WaitingStartedEvent;

    public TokenBucket(int capacity, TimeSpan refillPeriod, TimeProvider? timeProvider = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (refillPeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(refillPeriod));

        _capacity = capacity;
        _refillPeriod = refillPeriod;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _tokens = capacity;
        _semaphore = new SemaphoreSlim(capacity, capacity);
        _nextRefillTicks = (_timeProvider.GetUtcNow() + refillPeriod).UtcTicks;

        // Store timer in a field to prevent GC
        _timer = _timeProvider.CreateTimer(_ => Refill(), null, refillPeriod, refillPeriod);
    }

    /// <summary>When the next refill is due.</summary>
    public DateTimeOffset NextRefillAt => new(Interlocked.Read(ref _nextRefillTicks), TimeSpan.Zero);

    /// <summary>When the longest-waiting caller started waiting; null when nobody waits.</summary>
    public DateTimeOffset? OldestWaitStartedAt
    {
        get
        {
            DateTimeOffset? oldest = null;
            foreach (var started in _waiters.Values)
            {
                if (oldest is null || started < oldest) oldest = started;
            }
            return oldest;
        }
    }

    private void Refill()
    {
        if (_disposed) return;

        Interlocked.Exchange(ref _nextRefillTicks, (_timeProvider.GetUtcNow() + _refillPeriod).UtcTicks);

        // Use a spin loop with atomic compare-exchange to safely add tokens
        int current, newValue, toAdd;
        do
        {
            current = Volatile.Read(ref _tokens);
            toAdd = _capacity - current;

            if (toAdd <= 0) return;

            newValue = current + toAdd;
        }
        while (Interlocked.CompareExchange(ref _tokens, newValue, current) != current);

        // Only release after successfully updating _tokens
        _semaphore.Release(toAdd);
    }

    public async Task WaitAsync(CancellationToken ct)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TokenBucket));

        // Fast path: try to acquire immediately without waiting
        if (await _semaphore.WaitAsync(0, ct).ConfigureAwait(false))
        {
            Interlocked.Decrement(ref _tokens);
            return;
        }

        // We could not acquire immediately; notify observers that we're about to wait
        RaiseWaitingStarted();

        var waiterId = Interlocked.Increment(ref _nextWaiterId);
        _waiters[waiterId] = _timeProvider.GetUtcNow();
        try
        {
            // Now wait until a token becomes available
            await _semaphore.WaitAsync(ct).ConfigureAwait(false);
            Interlocked.Decrement(ref _tokens);
        }
        finally
        {
            _waiters.TryRemove(waiterId, out _);
        }
    }

    private void RaiseWaitingStarted()
    {
        var handler = WaitingStartedEvent;
        if (handler == null) return;
        try
        {
            handler(this, EventArgs.Empty);
        }
        catch
        {
            // Swallow to avoid impacting rate limiter behavior
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer?.Dispose();
        _semaphore?.Dispose();
    }
}
