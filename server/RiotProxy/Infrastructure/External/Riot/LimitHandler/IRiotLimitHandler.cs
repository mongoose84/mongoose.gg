namespace RiotProxy.Infrastructure.External.Riot.LimitHandler;

public interface IRiotLimitHandler 
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}