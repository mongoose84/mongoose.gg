using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for V2 analytics events with strict validation
/// </summary>
public interface IAnalyticsEventsV2Repository
{
    /// <summary>
    /// Insert a single v2 analytics event (validated or rejected)
    /// </summary>
    Task<long> InsertAsync(AnalyticsEventV2 evt);

    /// <summary>
    /// Insert multiple events in batch
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<AnalyticsEventV2> events);

    /// <summary>
    /// Get event count by name within time range
    /// </summary>
    Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to, bool includeRejected = false);

    /// <summary>
    /// Get unique user count by event name
    /// </summary>
    Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to);

    /// <summary>
    /// Get accepted event count (excluding rejections)
    /// </summary>
    Task<long> GetAcceptedEventCountAsync(DateTime from, DateTime to);

    /// <summary>
    /// Get rejection count by reason
    /// </summary>
    Task<Dictionary<string, long>> GetRejectionsByReasonAsync(DateTime from, DateTime to);

    /// <summary>
    /// Get event acceptance rate (0.0 to 1.0)
    /// </summary>
    Task<double> GetAcceptanceRateAsync(DateTime from, DateTime to);

    /// <summary>
    /// Delete events older than specified date (for retention policy)
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Get event distribution by category (for observability)
    /// </summary>
    Task<Dictionary<string, long>> GetEventDistributionByCategoryAsync(DateTime from, DateTime to);
}
