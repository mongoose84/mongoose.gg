using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Analytics Events V2 Repository - Optimized for time-range queries and retention
/// </summary>
public class AnalyticsEventsV2Repository : RepositoryBase, IAnalyticsEventsV2Repository
{
    public AnalyticsEventsV2Repository(IDbConnectionFactory factory) : base(factory) { }

    public virtual async Task<long> InsertAsync(AnalyticsEventV2 evt)
    {
        const string sql = @"
            INSERT INTO analytics_events_v2 
            (event_id, user_id, tier, session_id, event_name, event_category, event_version,
             payload_json, rejection_reason, payload_size_bytes,
             client_version, user_agent_hash, ip_anonymized,
             client_timestamp_utc, server_timestamp_utc, created_at)
            VALUES
            (@event_id, @user_id, @tier, @session_id, @event_name, @event_category, @event_version,
             @payload_json, @rejection_reason, @payload_size_bytes,
             @client_version, @user_agent_hash, @ip_anonymized,
             @client_timestamp_utc, @server_timestamp_utc, @created_at)";

        var rowsAffected = await ExecuteNonQueryAsync(sql,
            ("@event_id", evt.EventId),
            ("@user_id", evt.UserId),
            ("@tier", evt.Tier),
            ("@session_id", evt.SessionId),
            ("@event_name", evt.EventName),
            ("@event_category", evt.EventCategory),
            ("@event_version", evt.EventVersion),
            ("@payload_json", evt.PayloadJson),
            ("@rejection_reason", evt.RejectionReason),
            ("@payload_size_bytes", evt.PayloadSizeBytes),
            ("@client_version", evt.ClientVersion),
            ("@user_agent_hash", evt.UserAgentHash),
            ("@ip_anonymized", evt.IpAnonymized),
            ("@client_timestamp_utc", evt.ClientTimestampUtc),
            ("@server_timestamp_utc", evt.ServerTimestampUtc),
            ("@created_at", evt.CreatedAt));

        return rowsAffected;
    }

    public virtual async Task<int> InsertBatchAsync(IEnumerable<AnalyticsEventV2> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0) return 0;

        // For now, use individual inserts (future: bulk insert via LOAD DATA INFILE)
        var count = 0;
        foreach (var evt in eventList)
        {
            count += (int)await InsertAsync(evt);
        }
        return count;
    }

    public virtual Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to, bool includeRejected = false)
    {
        const string sql = @"
            SELECT COUNT(*) FROM analytics_events_v2
            WHERE event_name = @event_name
              AND created_at >= @from
              AND created_at <= @to
              {0}";

        var rejectionFilter = includeRejected ? "" : "AND rejection_reason IS NULL";
        var finalSql = string.Format(sql, rejectionFilter);

        return ExecuteScalarAsync<long>(finalSql,
            ("@event_name", eventName),
            ("@from", from),
            ("@to", to))!;
    }

    public virtual Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT user_id) FROM analytics_events_v2
            WHERE event_name = @event_name
              AND created_at >= @from
              AND created_at <= @to
              AND user_id IS NOT NULL
              AND rejection_reason IS NULL";

        return ExecuteScalarAsync<long>(sql,
            ("@event_name", eventName),
            ("@from", from),
            ("@to", to))!;
    }

    public virtual Task<long> GetAcceptedEventCountAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT COUNT(*) FROM analytics_events_v2
            WHERE rejection_reason IS NULL
              AND created_at >= @from
              AND created_at <= @to";

        return ExecuteScalarAsync<long>(sql,
            ("@from", from),
            ("@to", to))!;
    }

    public virtual async Task<Dictionary<string, long>> GetRejectionsByReasonAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT rejection_reason, COUNT(*) as count
            FROM analytics_events_v2
            WHERE rejection_reason IS NOT NULL
              AND created_at >= @from
              AND created_at <= @to
            GROUP BY rejection_reason";

        var results = new Dictionary<string, long>();
        using (var connection = await GetConnectionAsync())
        using (var cmd = CreateCommand(connection, sql))
        {
            cmd.AddParameter("@from", from);
            cmd.AddParameter("@to", to);

            using (var reader = await ExecuteReaderAsync(cmd))
            {
                while (await reader.ReadAsync())
                {
                    var reason = reader.GetString(0);
                    var count = reader.GetInt64(1);
                    results[reason] = count;
                }
            }
        }

        return results;
    }

    public virtual async Task<double> GetAcceptanceRateAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT
              SUM(CASE WHEN rejection_reason IS NULL THEN 1 ELSE 0 END) as accepted,
              COUNT(*) as total
            FROM analytics_events_v2
            WHERE created_at >= @from
              AND created_at <= @to";

        using (var connection = await GetConnectionAsync())
        using (var cmd = CreateCommand(connection, sql))
        {
            cmd.AddParameter("@from", from);
            cmd.AddParameter("@to", to);

            using (var reader = await ExecuteReaderAsync(cmd))
            {
                if (await reader.ReadAsync())
                {
                    var accepted = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
                    var total = reader.GetInt64(1);

                    return total > 0 ? (double)accepted / total : 0.0;
                }
            }
        }

        return 0.0;
    }

    public virtual Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        const string sql = @"
            DELETE FROM analytics_events_v2
            WHERE server_timestamp_utc < @cutoff_date";

        return ExecuteNonQueryAsync(sql,
            ("@cutoff_date", cutoffDate));
    }

    public virtual async Task<Dictionary<string, long>> GetEventDistributionByCategoryAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT event_category, COUNT(*) as count
            FROM analytics_events_v2
            WHERE created_at >= @from
              AND created_at <= @to
              AND rejection_reason IS NULL
            GROUP BY event_category";

        var results = new Dictionary<string, long>();
        using (var connection = await GetConnectionAsync())
        using (var cmd = CreateCommand(connection, sql))
        {
            cmd.AddParameter("@from", from);
            cmd.AddParameter("@to", to);

            using (var reader = await ExecuteReaderAsync(cmd))
            {
                while (await reader.ReadAsync())
                {
                    var category = reader.GetString(0);
                    var count = reader.GetInt64(1);
                    results[category] = count;
                }
            }
        }

        return results;
    }
}
