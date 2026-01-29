namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public interface IRiotLimitHandler : IDisposable
{
    Task WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// TEMPORARY: Event raised when rate limiting causes a wait.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    event EventHandler? RateLimitWaitStarted;
}