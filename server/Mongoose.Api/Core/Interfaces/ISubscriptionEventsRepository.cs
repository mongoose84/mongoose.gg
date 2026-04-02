using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ISubscriptionEventsRepository
{
    Task<long> InsertAsync(SubscriptionEvent ev);
    Task<IList<SubscriptionEvent>> GetBySubscriptionIdAsync(long subscriptionId);
}

