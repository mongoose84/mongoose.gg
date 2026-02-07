using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IAnalyticsEventsRepository
{
    Task<int> InsertAsync(AnalyticsEvent evt);
    Task<int> InsertBatchAsync(IEnumerable<AnalyticsEvent> events);
    Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to);
    Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to);
}

