using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IUsersRepository
{
    Task<long> UpsertAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<long> GetActiveUserCountAsync();
    Task UpdateEmailVerifiedAsync(long userId, bool verified);

    /// <summary>
    /// Updates a user's password hash and rotates the security stamp
    /// so that all existing sessions are invalidated.
    /// </summary>
    /// <param name="userId">The user ID to update</param>
    /// <param name="passwordHash">The new BCrypt password hash</param>
    Task UpdatePasswordHashAsync(long userId, string passwordHash);

    /// <summary>
    /// Returns the current security stamp for a user.
    /// Used by cookie validation to detect invalidated sessions.
    /// </summary>
    Task<string?> GetSecurityStampAsync(long userId);

    /// <summary>
    /// Permanently deletes a user and all associated data.
    /// This includes: user record, riot account links, LP snapshots, subscriptions, and verification tokens.
    /// Match/participant data is NOT deleted as it's tied to puuid, not user_id.
    /// </summary>
    /// <param name="userId">The user ID to delete</param>
    /// <returns>True if user was deleted, false if user not found</returns>
    Task<bool> DeleteUserAsync(long userId);
}

