using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ISubscriptionsRepository
{
    Task<long> UpsertAsync(Subscription subscription);
    Task<Subscription?> GetByUserIdAsync(long userId);
}

