using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IRiotAccountsRepository
{
    Task UpsertAsync(RiotAccount account);
    Task<IList<RiotAccount>> GetByUserIdAsync(long userId);
    Task<RiotAccount?> GetByPuuidAsync(string puuid);
    Task<bool> ExistsByPuuidAsync(string puuid);
    Task DeleteAsync(string puuid, long userId);
    Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null);
    Task SetPrimaryAsync(string puuid, long userId);
    Task<RiotAccount?> ClaimNextPendingForSyncAsync();
    Task ResetStuckSyncingAccountsAsync(TimeSpan threshold);
    Task UpdateSyncProgressAsync(string puuid, int progress, int total);
    Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel);
    Task UpdateRankDataAsync(string puuid, string? summonerId, string? soloTier, string? soloRank, int? soloLp, string? flexTier, string? flexRank, int? flexLp);
}

