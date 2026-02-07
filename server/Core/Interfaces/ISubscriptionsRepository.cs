using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ISubscriptionsRepository
{
    Task<long> UpsertAsync(Subscription subscription);
    Task<Subscription?> GetByUserIdAsync(long userId);
}

