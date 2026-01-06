namespace RiotProxy.Infrastructure.External.Riot.LimitHandler;

public interface IRiotLimitHandler : IDisposable
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}