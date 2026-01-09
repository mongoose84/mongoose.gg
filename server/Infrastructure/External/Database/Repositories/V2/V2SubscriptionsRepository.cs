using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2SubscriptionsRepository : RepositoryBase
{
    public V2SubscriptionsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public async Task<long> UpsertAsync(V2Subscription sub)
    {
        const string sql = @"INSERT INTO subscriptions
            (subscription_id, user_id, tier, status, mollie_subscription_id, mollie_plan_id, current_period_start, current_period_end, trial_start, trial_end, is_founding_member, cancel_at_period_end, canceled_at, created_at, updated_at)
            VALUES (@id, @user_id, @tier, @status, @mollie_subscription_id, @mollie_plan_id, @current_period_start, @current_period_end, @trial_start, @trial_end, @is_founding_member, @cancel_at_period_end, @canceled_at, @created_at, @updated_at) AS new
            ON DUPLICATE KEY UPDATE
                tier = new.tier,
                status = new.status,
                mollie_subscription_id = new.mollie_subscription_id,
                mollie_plan_id = new.mollie_plan_id,
                current_period_start = new.current_period_start,
                current_period_end = new.current_period_end,
                trial_start = new.trial_start,
                trial_end = new.trial_end,
                is_founding_member = new.is_founding_member,
                cancel_at_period_end = new.cancel_at_period_end,
                canceled_at = new.canceled_at,
                updated_at = new.updated_at;";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", sub.SubscriptionId == 0 ? DBNull.Value : sub.SubscriptionId);
            cmd.Parameters.AddWithValue("@user_id", sub.UserId);
            cmd.Parameters.AddWithValue("@tier", sub.Tier);
            cmd.Parameters.AddWithValue("@status", sub.Status);
            cmd.Parameters.AddWithValue("@mollie_subscription_id", sub.MollieSubscriptionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@mollie_plan_id", sub.MolliePlanId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@current_period_start", sub.CurrentPeriodStart ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@current_period_end", sub.CurrentPeriodEnd ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@trial_start", sub.TrialStart ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@trial_end", sub.TrialEnd ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@is_founding_member", sub.IsFoundingMember);
            cmd.Parameters.AddWithValue("@cancel_at_period_end", sub.CancelAtPeriodEnd);
            cmd.Parameters.AddWithValue("@canceled_at", sub.CanceledAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", sub.CreatedAt == default ? DateTime.UtcNow : sub.CreatedAt);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            return sub.SubscriptionId != 0 ? sub.SubscriptionId : cmd.LastInsertedId;
        });
    }

    public Task<V2Subscription?> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT * FROM subscriptions WHERE user_id = @user_id ORDER BY updated_at DESC LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@user_id", userId));
    }

    private static V2Subscription Map(MySqlDataReader r) => new()
    {
        SubscriptionId = r.GetInt64(0),
        UserId = r.GetInt64(1),
        Tier = r.GetString(2),
        Status = r.GetString(3),
        MollieSubscriptionId = r.IsDBNull(4) ? null : r.GetString(4),
        MolliePlanId = r.IsDBNull(5) ? null : r.GetString(5),
        CurrentPeriodStart = r.IsDBNull(6) ? null : r.GetDateTime(6),
        CurrentPeriodEnd = r.IsDBNull(7) ? null : r.GetDateTime(7),
        TrialStart = r.IsDBNull(8) ? null : r.GetDateTime(8),
        TrialEnd = r.IsDBNull(9) ? null : r.GetDateTime(9),
        IsFoundingMember = r.GetBoolean(10),
        CancelAtPeriodEnd = r.GetBoolean(11),
        CanceledAt = r.IsDBNull(12) ? null : r.GetDateTime(12),
        CreatedAt = r.GetDateTime(13),
        UpdatedAt = r.GetDateTime(14)
    };
}
