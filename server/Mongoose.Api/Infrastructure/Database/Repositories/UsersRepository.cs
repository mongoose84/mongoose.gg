using MySqlConnector;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Security;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class UsersRepository : RepositoryBase, IUsersRepository
{
    private readonly IEncryptor _encryptor;

    public UsersRepository(IDbConnectionFactory factory, IEncryptor encryptor) : base(factory)
    {
        _encryptor = encryptor;
    }

    public virtual async Task<long> UpsertAsync(User user)
    {
        const string sql = @"INSERT INTO users
            (user_id, email, username, password_hash, security_stamp, email_verified, is_active, tier, user_icon_id, mollie_customer_id, riot_puuid, created_at, updated_at, last_login_at)
            VALUES (@user_id, @email, @username, @password_hash, @security_stamp, @email_verified, @is_active, @tier, @user_icon_id, @mollie_customer_id, @riot_puuid, @created_at, @updated_at, @last_login_at) AS new
            ON DUPLICATE KEY UPDATE
                email = new.email,
                username = new.username,
                password_hash = new.password_hash,
                security_stamp = new.security_stamp,
                email_verified = new.email_verified,
                is_active = new.is_active,
                tier = new.tier,
                user_icon_id = new.user_icon_id,
                mollie_customer_id = new.mollie_customer_id,
                riot_puuid = new.riot_puuid,
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
            cmd.Parameters.AddWithValue("@security_stamp", user.SecurityStamp);
            cmd.Parameters.AddWithValue("@email_verified", user.EmailVerified);
            cmd.Parameters.AddWithValue("@is_active", user.IsActive);
            cmd.Parameters.AddWithValue("@tier", user.Tier);
            cmd.Parameters.AddWithValue("@user_icon_id", (object?)user.UserIconId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@mollie_customer_id", user.MollieCustomerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@riot_puuid", user.RiotPuuid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@last_login_at", user.LastLoginAt ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            return user.UserId != 0 ? user.UserId : cmd.LastInsertedId;
        });
    }

    public virtual async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"SELECT
                user_id,
                email,
                username,
                password_hash,
                security_stamp,
                email_verified,
                is_active,
                tier,
                user_icon_id,
                mollie_customer_id,
                riot_puuid,
                created_at,
                updated_at,
                last_login_at
            FROM users
            WHERE email = @email
            LIMIT 1";
        // Encrypt the search email to match stored encrypted value
        var encryptedEmail = _encryptor.Encrypt(email);
        return await ExecuteSingleAsync(sql, MapWithDecryption, ("@email", encryptedEmail));
    }

    public virtual Task<User?> GetByIdAsync(long userId)
    {
        const string sql = @"SELECT
                user_id,
                email,
                username,
                password_hash,
                security_stamp,
                email_verified,
                is_active,
                tier,
                user_icon_id,
                mollie_customer_id,
                riot_puuid,
                created_at,
                updated_at,
                last_login_at
            FROM users
            WHERE user_id = @user_id
            LIMIT 1";
        return ExecuteSingleAsync(sql, MapWithDecryption, ("@user_id", userId));
    }

    public virtual Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"SELECT
                user_id,
                email,
                username,
                password_hash,
                security_stamp,
                email_verified,
                is_active,
                tier,
                user_icon_id,
                mollie_customer_id,
                riot_puuid,
                created_at,
                updated_at,
                last_login_at
            FROM users
            WHERE username = @username
            LIMIT 1";
        // Use case-preserving encryption for lookup (IV derived from normalized value)
        var encryptedUsername = _encryptor.EncryptPreserveCase(username);
        return ExecuteSingleAsync(sql, MapWithDecryption, ("@username", encryptedUsername));
    }

    public virtual async Task<bool> UsernameExistsAsync(string username)
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

    public virtual Task<User?> GetByRiotPuuidAsync(string riotPuuid)
    {
        const string sql = @"SELECT
                user_id,
                email,
                username,
                password_hash,
                security_stamp,
                email_verified,
                is_active,
                tier,
                user_icon_id,
                mollie_customer_id,
                riot_puuid,
                created_at,
                updated_at,
                last_login_at
            FROM users
            WHERE riot_puuid = @riot_puuid
            LIMIT 1";
        return ExecuteSingleAsync(sql, MapWithDecryption, ("@riot_puuid", riotPuuid));
    }

    public virtual async Task<bool> EmailExistsAsync(string email)
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
	    public virtual async Task<long> GetActiveUserCountAsync()
	    {
	        const string sql = "SELECT COUNT(*) FROM users WHERE is_active = TRUE";
	        var result = await ExecuteScalarAsync<long>(sql);
	        return result;
	    }

    public virtual async Task UpdatePasswordHashAsync(long userId, string passwordHash)
    {
        const string sql = "UPDATE users SET password_hash = @password_hash, security_stamp = @security_stamp, updated_at = @updated_at WHERE user_id = @user_id";
        await ExecuteWithConnectionAsync<object?>(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
            cmd.Parameters.AddWithValue("@security_stamp", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@user_id", userId);
            await cmd.ExecuteNonQueryAsync();
            return null;
        });
    }

    /// <inheritdoc />
    public virtual async Task<string?> GetSecurityStampAsync(long userId)
    {
        const string sql = "SELECT security_stamp FROM users WHERE user_id = @user_id LIMIT 1";
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        });
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

    public virtual async Task UpdateUserIconIdAsync(long userId, int? userIconId)
    {
        const string sql = "UPDATE users SET user_icon_id = @user_icon_id, updated_at = @updated_at WHERE user_id = @user_id";
        await ExecuteWithConnectionAsync<object?>(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_icon_id", (object?)userIconId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@user_id", userId);
            await cmd.ExecuteNonQueryAsync();
            return null;
        });
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteUserAsync(long userId)
    {
        // Delete in order: child tables first, then parent
        // 1. Delete user_riot_accounts (links)
        // 2. Delete subscriptions
        // 3. Delete verification tokens (email verification, password reset, etc.)
        // 4. Delete the user record

        return await ExecuteWithConnectionAsync(async conn =>
        {
            // Start a transaction for atomicity
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Delete user_riot_accounts links
                var deleteLinks = "DELETE FROM user_riot_accounts WHERE user_id = @user_id";
                await using (var cmd = new MySqlCommand(deleteLinks, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete subscriptions
                var deleteSubscriptions = "DELETE FROM subscriptions WHERE user_id = @user_id";
                await using (var cmd = new MySqlCommand(deleteSubscriptions, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete verification tokens (email verification, password reset, etc.)
                var deleteVerificationTokens = "DELETE FROM verification_tokens WHERE user_id = @user_id";
                await using (var cmd = new MySqlCommand(deleteVerificationTokens, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete the user record
                var deleteUser = "DELETE FROM users WHERE user_id = @user_id";
                int rowsAffected;
                await using (var cmd = new MySqlCommand(deleteUser, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return rowsAffected > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    /// <summary>
    /// Maps a database row to User, decrypting the email and username.
    /// </summary>
    private User MapWithDecryption(MySqlDataReader r)
    {
        var userIdOrdinal = r.GetOrdinal("user_id");
        var emailOrdinal = r.GetOrdinal("email");
        var usernameOrdinal = r.GetOrdinal("username");
        var passwordHashOrdinal = r.GetOrdinal("password_hash");
        var securityStampOrdinal = r.GetOrdinal("security_stamp");
        var emailVerifiedOrdinal = r.GetOrdinal("email_verified");
        var isActiveOrdinal = r.GetOrdinal("is_active");
        var tierOrdinal = r.GetOrdinal("tier");
        var userIconIdOrdinal = r.GetOrdinal("user_icon_id");
        var mollieCustomerIdOrdinal = r.GetOrdinal("mollie_customer_id");
        var riotPuuidOrdinal = r.GetOrdinal("riot_puuid");
        var createdAtOrdinal = r.GetOrdinal("created_at");
        var updatedAtOrdinal = r.GetOrdinal("updated_at");
        var lastLoginAtOrdinal = r.GetOrdinal("last_login_at");

        var userId = r.GetInt64(userIdOrdinal);
        var encryptedEmail = r.GetString(emailOrdinal);
        var encryptedUsername = r.GetString(usernameOrdinal);

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
            PasswordHash = r.GetString(passwordHashOrdinal),
            SecurityStamp = r.GetString(securityStampOrdinal),
            EmailVerified = r.GetFieldValue<bool>(emailVerifiedOrdinal),
            IsActive = r.GetFieldValue<bool>(isActiveOrdinal),
            Tier = r.GetString(tierOrdinal),
            UserIconId = r.IsDBNull(userIconIdOrdinal) ? null : r.GetInt32(userIconIdOrdinal),
            MollieCustomerId = r.IsDBNull(mollieCustomerIdOrdinal) ? null : r.GetString(mollieCustomerIdOrdinal),
            RiotPuuid = r.IsDBNull(riotPuuidOrdinal) ? null : r.GetString(riotPuuidOrdinal),
            CreatedAt = r.GetDateTimeUtc(createdAtOrdinal),
            UpdatedAt = r.GetDateTimeUtc(updatedAtOrdinal),
            LastLoginAt = r.GetDateTimeUtcOrNull(lastLoginAtOrdinal)
        };
    }
}
