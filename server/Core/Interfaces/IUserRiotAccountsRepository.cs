using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Repository for managing the M:M relationship between users and Riot accounts.
/// </summary>
public interface IUserRiotAccountsRepository
{
    /// <summary>
    /// Link a Riot account to a user.
    /// </summary>
    Task LinkAsync(long userId, string puuid, bool isPrimary);
    
    /// <summary>
    /// Unlink a Riot account from a user.
    /// </summary>
    Task UnlinkAsync(long userId, string puuid);
    
    /// <summary>
    /// Check if a user has linked a specific Riot account.
    /// </summary>
    Task<bool> IsLinkedAsync(long userId, string puuid);
    
    /// <summary>
    /// Get all links for a user (with Riot account data).
    /// </summary>
    Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId);
    
    /// <summary>
    /// Get all user IDs linked to a specific Riot account.
    /// </summary>
    Task<IList<long>> GetUserIdsByPuuidAsync(string puuid);
    
    /// <summary>
    /// Set a Riot account as primary for a user (and unset others).
    /// </summary>
    Task SetPrimaryAsync(long userId, string puuid);
    
    /// <summary>
    /// Get the primary Riot account link for a user.
    /// </summary>
    Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId);
    
    /// <summary>
    /// Check if any user has linked a specific Riot account.
    /// </summary>
    Task<bool> HasAnyLinksAsync(string puuid);
    
    /// <summary>
    /// Get count of users linked to a Riot account.
    /// </summary>
    Task<int> GetLinkCountAsync(string puuid);
}

