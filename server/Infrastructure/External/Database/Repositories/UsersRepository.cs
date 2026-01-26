using MySqlConnector;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.Security;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class UsersRepository : RepositoryBase
{
    private readonly IEncryptor _encryptor;

    public UsersRepository(IDbConnectionFactory factory, IEncryptor encryptor) : base(factory)
    {
        _encryptor = encryptor;
    }

    public virtual async Task<long> UpsertAsync(User user)
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

        // Encrypt email (normalized) and username (case-preserving) before storing
        var encryptedEmail = _encryptor.Encrypt(user.Email);
        var encryptedUsername = _encryptor.EncryptPreserveCase(user.Username);

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", user.UserId == 0 ? DBNull.Value : user.UserId);
            cmd.Parameters.AddWithValue("@email", encryptedEmail);
            cmd.Parameters.AddWithValue("@username", encryptedUsername);
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

    public virtual async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM users WHERE email = @email LIMIT 1";
        // Encrypt the search email to match stored encrypted value
        var encryptedEmail = _encryptor.Encrypt(email);
        return await ExecuteSingleAsync(sql, MapWithDecryption, ("@email", encryptedEmail));
    }

    public virtual Task<User?> GetByIdAsync(long userId)
    {
        const string sql = "SELECT * FROM users WHERE user_id = @user_id LIMIT 1";
        return ExecuteSingleAsync(sql, MapWithDecryption, ("@user_id", userId));
    }

    public virtual Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM users WHERE username = @username LIMIT 1";
        // Use case-preserving encryption for lookup (IV derived from normalized value)
        var encryptedUsername = _encryptor.EncryptPreserveCase(username);
        return ExecuteSingleAsync(sql, MapWithDecryption, ("@username", encryptedUsername));
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        // Use case-preserving encryption for lookup (IV derived from normalized value)
        var encryptedUsername = _encryptor.EncryptPreserveCase(username);
        const string sql = "SELECT COUNT(*) FROM users WHERE username = @username";
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", encryptedUsername);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        // Encrypt the search email to match stored encrypted value
        var encryptedEmail = _encryptor.Encrypt(email);
        const string sql = "SELECT COUNT(*) FROM users WHERE email = @email";
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", encryptedEmail);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result) > 0;
        });
    }

	    /// <summary>
	    /// Returns the number of active users (players) in the system.
	    /// Used for public landing-page stats.
	    /// </summary>
	    public async Task<long> GetActiveUserCountAsync()
	    {
	        const string sql = "SELECT COUNT(*) FROM users WHERE is_active = TRUE";
	        var result = await ExecuteScalarAsync<long>(sql);
	        return result;
	    }

    public virtual async Task UpdateEmailVerifiedAsync(long userId, bool verified)
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

    /// <summary>
    /// Maps a database row to User, decrypting the email and username.
    /// </summary>
    private User MapWithDecryption(MySqlDataReader r)
    {
        var userId = r.GetInt64(0);
        var encryptedEmail = r.GetString(1);
        var encryptedUsername = r.GetString(2);

        string decryptedEmail;
        string decryptedUsername;
        try
        {
            decryptedEmail = _encryptor.Decrypt(encryptedEmail);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Email decryption failed for user {userId}: Invalid base64 format. " +
                "This may indicate corrupted data in the database.", ex);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new InvalidOperationException(
                $"Email decryption failed for user {userId}: Cryptographic error. " +
                "This may indicate a wrong encryption key or corrupted ciphertext.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Email decryption failed for user {userId}: {ex.Message}", ex);
        }

        try
        {
            decryptedUsername = _encryptor.Decrypt(encryptedUsername);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Username decryption failed for user {userId}: Invalid base64 format. " +
                "This may indicate corrupted data in the database.", ex);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            throw new InvalidOperationException(
                $"Username decryption failed for user {userId}: Cryptographic error. " +
                "This may indicate a wrong encryption key or corrupted ciphertext.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Username decryption failed for user {userId}: {ex.Message}", ex);
        }

        return new User
        {
            UserId = userId,
            Email = decryptedEmail,
            Username = decryptedUsername,
            PasswordHash = r.GetString(3),
            EmailVerified = r.GetBoolean(4),
            IsActive = r.GetBoolean(5),
            Tier = r.GetString(6),
            MollieCustomerId = r.IsDBNull(7) ? null : r.GetString(7),
            CreatedAt = r.GetDateTimeUtc(8),
            UpdatedAt = r.GetDateTimeUtc(9),
            LastLoginAt = r.GetDateTimeUtcOrNull(10)
        };
    }
}
