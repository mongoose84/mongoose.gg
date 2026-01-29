using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ISubscriptionEventsRepository
{
    Task<long> InsertAsync(SubscriptionEvent ev);
    Task<IList<SubscriptionEvent>> GetBySubscriptionIdAsync(long subscriptionId);
}

