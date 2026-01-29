namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public sealed class TokenBucket : IDisposable
{
    private readonly int _capacity;
    private int _tokens;
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _timer;
    private bool _disposed;

    // Raised when a caller is about to block waiting for a token.
    // Subscribe to observe backpressure (e.g., for logging/metrics).
    public event EventHandler? WaitingStartedEvent;

    public TokenBucket(int capacity, TimeSpan refillPeriod)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (refillPeriod <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(refillPeriod));

        _capacity = capacity;
        _tokens = capacity;
        _semaphore = new SemaphoreSlim(capacity, capacity);

        // Store timer in a field to prevent GC
        _timer = new Timer(_ => Refill(), null, refillPeriod, refillPeriod);
    }

    private void Refill()
    {
        if (_disposed) return;

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

        // Now wait until a token becomes available
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        Interlocked.Decrement(ref _tokens);
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