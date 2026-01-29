namespace RiotProxy.Infrastructure.Riot.LimitHandler;

public interface IRiotLimitHandler : IDisposable
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}