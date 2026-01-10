using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2UsersRepository : RepositoryBase
{
    public V2UsersRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public async Task<long> UpsertAsync(V2User user)
    {
        const string sql = @"INSERT INTO users
            (user_id, email, username, password_hash, email_verified, is_active, tier, mollie_customer_id, created_at, updated_at, last_login_at)
            VALUES (@user_id, @email, @username, @password_hash, @email_verified, @is_active, @tier, @mollie_customer_id, @created_at, @updated_at, @last_login_at) AS new
            ON DUPLICATE KEY UPDATE
                email = new.email,
                username = new.username,
                password_hash = new.password_hash,
                email_verified = new.email_verified,
                is_active = new.is_active,
                tier = new.tier,
                mollie_customer_id = new.mollie_customer_id,
                updated_at = new.updated_at,
                last_login_at = new.last_login_at;";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", user.UserId == 0 ? DBNull.Value : user.UserId);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@email_verified", user.EmailVerified);
            cmd.Parameters.AddWithValue("@is_active", user.IsActive);
            cmd.Parameters.AddWithValue("@tier", user.Tier);
            cmd.Parameters.AddWithValue("@mollie_customer_id", user.MollieCustomerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@last_login_at", user.LastLoginAt ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            return user.UserId != 0 ? user.UserId : cmd.LastInsertedId;
        });
    }

    public Task<V2User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM users WHERE email = @email LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@email", email));
    }

    public Task<V2User?> GetByIdAsync(long userId)
    {
        const string sql = "SELECT * FROM users WHERE user_id = @user_id LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@user_id", userId));
    }

    public Task<V2User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM users WHERE username = @username LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@username", username));
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE username = @username";
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE email = @email";
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        });
    }

    public async Task UpdateEmailVerifiedAsync(long userId, bool verified)
    {
        const string sql = "UPDATE users SET email_verified = @verified, updated_at = @updated_at WHERE user_id = @user_id";
        await ExecuteWithConnectionAsync<object?>(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@verified", verified);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@user_id", userId);
            await cmd.ExecuteNonQueryAsync();
            return null;
        });
    }

    private static V2User Map(MySqlDataReader r) => new()
    {
        UserId = r.GetInt64(0),
        Email = r.GetString(1),
        Username = r.GetString(2),
        PasswordHash = r.GetString(3),
        EmailVerified = r.GetBoolean(4),
        IsActive = r.GetBoolean(5),
        Tier = r.GetString(6),
        MollieCustomerId = r.IsDBNull(7) ? null : r.GetString(7),
        CreatedAt = r.GetDateTime(8),
        UpdatedAt = r.GetDateTime(9),
        LastLoginAt = r.IsDBNull(10) ? null : r.GetDateTime(10)
    };
}
