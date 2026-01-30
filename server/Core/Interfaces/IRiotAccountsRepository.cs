using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Repository for Riot account data (shared across users).
/// User-specific linking is handled by IUserRiotAccountsRepository.
/// </summary>
public interface IRiotAccountsRepository
{
    Task UpsertAsync(RiotAccount account);
    Task<RiotAccount?> GetByPuuidAsync(string puuid);
    Task<bool> ExistsByPuuidAsync(string puuid);
    Task DeleteAsync(string puuid);
    Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null);
    Task<RiotAccount?> ClaimNextPendingForSyncAsync();
    Task ResetStuckSyncingAccountsAsync(TimeSpan threshold);
    Task UpdateSyncProgressAsync(string puuid, int progress, int total);
    Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel);
    Task UpdateRankDataAsync(string puuid, string? summonerId, string? soloTier, string? soloRank, int? soloLp, string? flexTier, string? flexRank, int? flexLp);
}

