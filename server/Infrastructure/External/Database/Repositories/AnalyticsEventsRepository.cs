using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for analytics events. Optimized for high-volume inserts.
/// </summary>
public class AnalyticsEventsRepository : RepositoryBase
{
    public AnalyticsEventsRepository(IDbConnectionFactory factory) : base(factory) { }

    /// <summary>
    /// Insert a single analytics event.
    /// </summary>
    public virtual Task<int> InsertAsync(AnalyticsEvent evt)
    {
        const string sql = @"
            INSERT INTO analytics_events (user_id, tier, event_name, payload_json, session_id, created_at)
            VALUES (@user_id, @tier, @event_name, @payload_json, @session_id, @created_at)";

        return ExecuteNonQueryAsync(sql,
            ("@user_id", evt.UserId),
            ("@tier", evt.Tier),
            ("@event_name", evt.EventName),
            ("@payload_json", evt.PayloadJson),
            ("@session_id", evt.SessionId),
            ("@created_at", evt.CreatedAt));
    }

    /// <summary>
    /// Insert multiple analytics events in a batch (for future buffered writes).
    /// </summary>
    public virtual async Task<int> InsertBatchAsync(IEnumerable<AnalyticsEvent> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0) return 0;

        // For small batches, use individual inserts
        // For large batches, we could use bulk insert syntax
        var count = 0;
        foreach (var evt in eventList)
        {
            count += await InsertAsync(evt);
        }
        return count;
    }

    /// <summary>
    /// Get event count by event name within a time range (for basic analytics).
    /// </summary>
    public virtual Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT COUNT(*) FROM analytics_events
            WHERE event_name = @event_name
              AND created_at >= @from
              AND created_at <= @to";

        return ExecuteScalarAsync<long>(sql,
            ("@event_name", eventName),
            ("@from", from),
            ("@to", to))!;
    }

    /// <summary>
    /// Get distinct user count for an event within a time range.
    /// </summary>
    public virtual Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT user_id) FROM analytics_events
            WHERE event_name = @event_name
              AND created_at >= @from
              AND created_at <= @to
              AND user_id IS NOT NULL";

        return ExecuteScalarAsync<long>(sql,
            ("@event_name", eventName),
            ("@from", from),
            ("@to", to))!;
    }
}

