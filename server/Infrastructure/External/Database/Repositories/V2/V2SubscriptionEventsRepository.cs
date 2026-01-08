using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2SubscriptionEventsRepository : RepositoryBase
{
    public V2SubscriptionEventsRepository(IDbConnectionFactory factory) : base(factory) {}

    public async Task<long> InsertAsync(V2SubscriptionEvent ev)
    {
        const string sql = @"INSERT INTO subscription_events
            (subscription_id, event_type, old_tier, new_tier, old_status, new_status, mollie_event_id, metadata, created_at)
            VALUES (@subscription_id, @event_type, @old_tier, @new_tier, @old_status, @new_status, @mollie_event_id, @metadata, @created_at);";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@subscription_id", ev.SubscriptionId);
            cmd.Parameters.AddWithValue("@event_type", ev.EventType);
            cmd.Parameters.AddWithValue("@old_tier", ev.OldTier ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@new_tier", ev.NewTier ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@old_status", ev.OldStatus ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@new_status", ev.NewStatus ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@mollie_event_id", ev.MollieEventId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@metadata", ev.MetadataJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", ev.CreatedAt == default ? DateTime.UtcNow : ev.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    public Task<IList<V2SubscriptionEvent>> GetBySubscriptionIdAsync(long subscriptionId)
    {
        const string sql = "SELECT * FROM subscription_events WHERE subscription_id = @subscription_id ORDER BY created_at DESC";
        return ExecuteListAsync(sql, Map, ("@subscription_id", subscriptionId));
    }

    private static V2SubscriptionEvent Map(MySqlDataReader r) => new()
    {
        EventId = r.GetInt64(0),
        SubscriptionId = r.GetInt64(1),
        EventType = r.GetString(2),
        OldTier = r.IsDBNull(3) ? null : r.GetString(3),
        NewTier = r.IsDBNull(4) ? null : r.GetString(4),
        OldStatus = r.IsDBNull(5) ? null : r.GetString(5),
        NewStatus = r.IsDBNull(6) ? null : r.GetString(6),
        MollieEventId = r.IsDBNull(7) ? null : r.GetString(7),
        MetadataJson = r.IsDBNull(8) ? null : r.GetString(8),
        CreatedAt = r.GetDateTime(9)
    };
}
