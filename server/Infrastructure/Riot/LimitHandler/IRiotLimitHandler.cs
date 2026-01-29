namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public interface IRiotLimitHandler : IDisposable
{
    Task WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// TEMPORARY: Wait for rate limit tokens with PUUID context.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    /// <param name="puuid">The PUUID of the account making the request, or null if unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WaitAsync(string? puuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// TEMPORARY: Event raised when rate limiting causes a wait.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;
}