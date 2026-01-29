namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public class RiotLimitHandler : IRiotLimitHandler
{
    private bool _disposed = false;
    private readonly TokenBucket _perSecondBucket = new(10, TimeSpan.FromSeconds(1));
    private readonly TokenBucket _perTwoMinuteBucket = new(50, TimeSpan.FromMinutes(2));

    /// <summary>
    /// TEMPORARY: AsyncLocal to track the current PUUID context during rate limit waits.
    /// This allows us to identify which account triggered the rate limit when the bucket fires its event.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    private static readonly AsyncLocal<string?> _currentPuuid = new();

    /// <summary>
    /// TEMPORARY: Event raised when rate limiting causes a wait.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;

    public RiotLimitHandler()
    {
        _perSecondBucket.WaitingStartedEvent += OnWaitingStarted;
        _perTwoMinuteBucket.WaitingStartedEvent += OnWaitingStarted;
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        // TEMPORARY: Delegate to the PUUID-aware overload with null PUUID
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        await WaitAsync(null, cancellationToken);
    }

    /// <summary>
    /// TEMPORARY: Wait for rate limit tokens with PUUID context.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    public async Task WaitAsync(string? puuid, CancellationToken cancellationToken = default)
    {
        // TEMPORARY: Store the PUUID in AsyncLocal so the event handler can access it
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        _currentPuuid.Value = puuid;
        try
        {
            await _perSecondBucket.WaitAsync(cancellationToken);
            await _perTwoMinuteBucket.WaitAsync(cancellationToken);
        }
        finally
        {
            _currentPuuid.Value = null;
        }
    }

    private void OnWaitingStarted(object? sender, EventArgs e)
    {
        var bucketName = ReferenceEquals(sender, _perSecondBucket)
            ? "per-second"
            : ReferenceEquals(sender, _perTwoMinuteBucket)
                ? "per-two-minute"
                : "unknown";

        // TEMPORARY: Get the current PUUID from AsyncLocal context
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        var puuid = _currentPuuid.Value;

        Console.WriteLine($"Rate limiting: waiting for token ({bucketName}), puuid={puuid ?? "unknown"}");

        // TEMPORARY: Raise event for sync job to broadcast rate limit status
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        RateLimitWaitStarted?.Invoke(this, new RateLimitWaitEventArgs(puuid));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _perSecondBucket.Dispose();
        _perTwoMinuteBucket.Dispose();
    }
}