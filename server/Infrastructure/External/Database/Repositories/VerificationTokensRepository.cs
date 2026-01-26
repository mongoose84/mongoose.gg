using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class VerificationTokensRepository : RepositoryBase
{
    public VerificationTokensRepository(IDbConnectionFactory factory) : base(factory) { }

    /// <summary>
    /// Creates a new verification token for a user.
    /// </summary>
    public virtual async Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt)
    {
        const string sql = @"INSERT INTO verification_tokens 
            (user_id, token_type, code, expires_at, attempts, created_at)
            VALUES (@user_id, @token_type, @code, @expires_at, 0, @created_at)";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@token_type", tokenType);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@expires_at", expiresAt);
            cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    /// <summary>
    /// Gets the most recent active (non-expired, non-used) token for a user and type.
    /// </summary>
    public virtual async Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType)
    {
        const string sql = @"SELECT id, user_id, token_type, code, expires_at, used_at, attempts, created_at 
            FROM verification_tokens 
            WHERE user_id = @user_id 
              AND token_type = @token_type 
              AND used_at IS NULL 
              AND expires_at > @now
            ORDER BY created_at DESC
            LIMIT 1";

        return await ExecuteSingleAsync(sql, Map,
            ("@user_id", userId),
            ("@token_type", tokenType),
            ("@now", DateTime.UtcNow));
    }

    /// <summary>
    /// Marks a token as used.
    /// </summary>
    public virtual async Task MarkTokenAsUsedAsync(long tokenId)
    {
        const string sql = "UPDATE verification_tokens SET used_at = @used_at WHERE id = @id";
        await ExecuteNonQueryAsync(sql, ("@used_at", DateTime.UtcNow), ("@id", tokenId));
    }

    /// <summary>
    /// Increments the attempt counter for a token.
    /// </summary>
    public virtual async Task IncrementAttemptsAsync(long tokenId)
    {
        const string sql = "UPDATE verification_tokens SET attempts = attempts + 1 WHERE id = @id";
        await ExecuteNonQueryAsync(sql, ("@id", tokenId));
    }

    /// <summary>
    /// Counts tokens created for a user within the specified time window.
    /// Used for rate limiting token creation.
    /// </summary>
    public virtual async Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds)
    {
        const string sql = @"SELECT COUNT(*) FROM verification_tokens 
            WHERE user_id = @user_id 
              AND token_type = @token_type 
              AND created_at > @since";

        var since = DateTime.UtcNow.AddSeconds(-seconds);
        var result = await ExecuteScalarAsync<long>(sql,
            ("@user_id", userId),
            ("@token_type", tokenType),
            ("@since", since));

        return (int)result;
    }

    /// <summary>
    /// Invalidates all active tokens for a user and type (marks as used).
    /// Call this before creating a new token to ensure only one active token exists.
    /// </summary>
    public virtual async Task InvalidateActiveTokensAsync(long userId, string tokenType)
    {
        const string sql = @"UPDATE verification_tokens 
            SET used_at = @used_at 
            WHERE user_id = @user_id 
              AND token_type = @token_type 
              AND used_at IS NULL";

        await ExecuteNonQueryAsync(sql,
            ("@used_at", DateTime.UtcNow),
            ("@user_id", userId),
            ("@token_type", tokenType));
    }

    private static VerificationToken Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        UserId = r.GetInt64(1),
        TokenType = r.GetString(2),
        Code = r.GetString(3),
        ExpiresAt = r.GetDateTimeUtc(4),
        UsedAt = r.GetDateTimeUtcOrNull(5),
        Attempts = r.GetInt32(6),
        CreatedAt = r.GetDateTimeUtc(7)
    };
}

