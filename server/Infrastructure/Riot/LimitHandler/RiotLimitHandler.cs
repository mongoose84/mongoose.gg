namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public class RiotLimitHandler : IRiotLimitHandler
{
    private bool _disposed = false;
    private readonly TokenBucket _perSecondBucket = new(10, TimeSpan.FromSeconds(1));
    private readonly TokenBucket _perTwoMinuteBucket = new(50, TimeSpan.FromMinutes(2));

    /// <summary>
    /// TEMPORARY: Event raised when rate limiting causes a wait.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    public event EventHandler? RateLimitWaitStarted;

    public RiotLimitHandler()
    {
        _perSecondBucket.WaitingStartedEvent += OnWaitingStarted;
        _perTwoMinuteBucket.WaitingStartedEvent += OnWaitingStarted;
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _perSecondBucket.WaitAsync(cancellationToken);
        await _perTwoMinuteBucket.WaitAsync(cancellationToken);
    }

    private void OnWaitingStarted(object? sender, EventArgs e)
    {
        var bucketName = ReferenceEquals(sender, _perSecondBucket)
            ? "per-second"
            : ReferenceEquals(sender, _perTwoMinuteBucket)
                ? "per-two-minute"
                : "unknown";

        Console.WriteLine($"Rate limiting: waiting for token ({bucketName})");

        // TEMPORARY: Raise event for sync job to broadcast rate limit status
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        RateLimitWaitStarted?.Invoke(this, EventArgs.Empty);
    }

     public void Dispose()
    {
        if (_disposed) return;
            _disposed = true;

        _perSecondBucket.Dispose();
        _perTwoMinuteBucket.Dispose();
    }
}