using System.Text.Json;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Infrastructure.WebSocket;

namespace RiotProxy.Application.Services;

/// <summary>
/// Handles sync checks when a user logs in.
/// Checks for new matches and updates profile data for linked Riot accounts.
/// </summary>
public class LoginSyncService
{
    private readonly RiotAccountsRepository _riotAccountsRepo;
    private readonly LpSnapshotsRepository _lpSnapshotsRepo;
    private readonly IRiotApiClient _riotApiClient;
    private readonly ISyncProgressBroadcaster _syncBroadcaster;
    private readonly ILogger<LoginSyncService> _logger;

    /// <summary>
    /// Cooldown period - don't re-check if last sync was within this timeframe.
    /// </summary>
    private static readonly TimeSpan SyncCooldown = TimeSpan.FromMinutes(5);

    public LoginSyncService(
        RiotAccountsRepository riotAccountsRepo,
        LpSnapshotsRepository lpSnapshotsRepo,
        IRiotApiClient riotApiClient,
        ISyncProgressBroadcaster syncBroadcaster,
        ILogger<LoginSyncService> logger)
    {
        _riotAccountsRepo = riotAccountsRepo;
        _lpSnapshotsRepo = lpSnapshotsRepo;
        _riotApiClient = riotApiClient;
        _syncBroadcaster = syncBroadcaster;
        _logger = logger;
    }

    /// <summary>
    /// Check all linked Riot accounts for a user on login.
    /// Updates profile data and triggers sync if new matches are found.
    /// </summary>
    public async Task CheckAccountsOnLoginAsync(long userId)
    {
        _logger.LogInformation("Starting login sync check for user {UserId}", userId);
        try
        {
            var accounts = await _riotAccountsRepo.GetByUserIdAsync(userId);
            if (accounts == null || accounts.Count == 0)
            {
                _logger.LogInformation("No linked Riot accounts for user {UserId}", userId);
                return;
            }

            _logger.LogInformation("Found {Count} linked Riot accounts for user {UserId}", accounts.Count, userId);
            foreach (var account in accounts)
            {
                await CheckAccountAsync(account);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking accounts on login for user {UserId}", userId);
            // Don't throw - login should succeed even if sync check fails
        }
    }

    private async Task CheckAccountAsync(RiotAccount account)
    {
        try
        {
            // Always update profile data (icon, level, rank) from Riot API
            await UpdateProfileDataAsync(account);

            // Check cooldown - skip match sync if last sync was recent
            if (account.LastSyncAt.HasValue &&
                DateTime.UtcNow - account.LastSyncAt.Value < SyncCooldown)
            {
                _logger.LogDebug("Skipping match sync check for {Puuid} - last sync was {LastSync}",
                    account.Puuid, account.LastSyncAt);
                return;
            }

            // Skip match sync if already syncing or pending
            if (account.SyncStatus == "syncing" || account.SyncStatus == "pending")
            {
                _logger.LogDebug("Skipping match sync check for {Puuid} - already {Status}",
                    account.Puuid, account.SyncStatus);
                return;
            }

            // Check for new matches and trigger sync if needed
            await CheckForNewMatchesAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking account {Puuid} on login", account.Puuid);
            // Don't throw - continue with other accounts
        }
    }

    private async Task UpdateProfileDataAsync(RiotAccount account)
    {
        _logger.LogInformation("Fetching profile data for {GameName}#{TagLine} ({Region})",
            account.GameName, account.TagLine, account.Region);
        try
        {
            using var summonerDoc = await _riotApiClient.GetSummonerByPuuIdAsync(account.Region, account.Puuid);

            if (summonerDoc.RootElement.ValueKind == JsonValueKind.Object &&
                summonerDoc.RootElement.TryGetProperty("profileIconId", out var iconProp) &&
                summonerDoc.RootElement.TryGetProperty("summonerLevel", out var levelProp))
            {
                var profileIconId = iconProp.GetInt32();
                var summonerLevel = levelProp.GetInt32();

                // Only update if changed
                if (account.ProfileIconId != profileIconId || account.SummonerLevel != summonerLevel)
                {
                    await _riotAccountsRepo.UpdateProfileDataAsync(account.Puuid, profileIconId, summonerLevel);
                    _logger.LogInformation("Updated profile data for {Puuid}: icon={Icon}, level={Level}",
                        account.Puuid, profileIconId, summonerLevel);
                }
            }
            else
            {
                _logger.LogWarning("Invalid summoner response for {Puuid}", account.Puuid);
            }

            // Update rank data using PUUID (new Riot API endpoint)
            await UpdateRankDataAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update profile data for {Puuid}", account.Puuid);
            // Don't throw - continue with sync check
        }
    }

    private async Task UpdateRankDataAsync(RiotAccount account)
    {
        try
        {
            // Use the new PUUID-based league endpoint (added to Riot API in 2025)
            using var leagueDoc = await _riotApiClient.GetLeagueEntriesByPuuidAsync(account.Region, account.Puuid);

            string? soloTier = null, soloRank = null, flexTier = null, flexRank = null;
            string? summonerId = null;
            int? soloLp = null, flexLp = null;

            foreach (var entry in leagueDoc.RootElement.EnumerateArray())
            {
                // Try to extract summonerId from league entry (if available)
                if (summonerId == null && entry.TryGetProperty("summonerId", out var summonerIdProp))
                {
                    summonerId = summonerIdProp.GetString();
                }

                var queueType = entry.GetProperty("queueType").GetString();
                if (queueType == "RANKED_SOLO_5x5")
                {
                    soloTier = entry.GetProperty("tier").GetString();
                    soloRank = entry.GetProperty("rank").GetString();
                    soloLp = entry.GetProperty("leaguePoints").GetInt32();
                }
                else if (queueType == "RANKED_FLEX_SR")
                {
                    flexTier = entry.GetProperty("tier").GetString();
                    flexRank = entry.GetProperty("rank").GetString();
                    flexLp = entry.GetProperty("leaguePoints").GetInt32();
                }
            }

            // Check if rank data changed
            var rankChanged = account.SoloTier != soloTier || account.SoloRank != soloRank || account.SoloLp != soloLp ||
                              account.FlexTier != flexTier || account.FlexRank != flexRank || account.FlexLp != flexLp;
            var summonerIdChanged = summonerId != null && account.SummonerId != summonerId;

            if (rankChanged || summonerIdChanged)
            {
                await _riotAccountsRepo.UpdateRankDataAsync(
                    account.Puuid, summonerId ?? account.SummonerId,
                    soloTier, soloRank, soloLp,
                    flexTier, flexRank, flexLp);
                _logger.LogInformation("Updated rank data for {Puuid}: solo={SoloTier} {SoloRank}, flex={FlexTier} {FlexRank}",
                    account.Puuid, soloTier, soloRank, flexTier, flexRank);
            }

            // Record LP snapshots for time-series tracking (always, on every login)
            // This builds LP history independent of match syncs
            await RecordLpSnapshotsAsync(account.Puuid, soloTier, soloRank, soloLp, flexTier, flexRank, flexLp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update rank data for {Puuid}", account.Puuid);
            // Don't throw - rank data is optional
        }
    }

    /// <summary>
    /// Records LP snapshots for Solo and Flex queues if the player has ranked data.
    /// Called on every login to build LP progression history.
    /// </summary>
    private async Task RecordLpSnapshotsAsync(
        string puuid,
        string? soloTier, string? soloRank, int? soloLp,
        string? flexTier, string? flexRank, int? flexLp)
    {
        var now = DateTime.UtcNow;

        try
        {
            // Record Solo Queue snapshot if player has solo rank
            if (!string.IsNullOrEmpty(soloTier) && !string.IsNullOrEmpty(soloRank) && soloLp.HasValue)
            {
                var soloSnapshot = new LpSnapshot
                {
                    Puuid = puuid,
                    QueueType = "RANKED_SOLO_5x5",
                    Tier = soloTier,
                    Division = soloRank,
                    Lp = soloLp.Value,
                    RecordedAt = now,
                    CreatedAt = now
                };
                await _lpSnapshotsRepo.InsertAsync(soloSnapshot);
                _logger.LogDebug("Recorded LP snapshot for {Puuid} in Solo: {Tier} {Division} {LP} LP",
                    puuid, soloTier, soloRank, soloLp);
            }

            // Record Flex Queue snapshot if player has flex rank
            if (!string.IsNullOrEmpty(flexTier) && !string.IsNullOrEmpty(flexRank) && flexLp.HasValue)
            {
                var flexSnapshot = new LpSnapshot
                {
                    Puuid = puuid,
                    QueueType = "RANKED_FLEX_SR",
                    Tier = flexTier,
                    Division = flexRank,
                    Lp = flexLp.Value,
                    RecordedAt = now,
                    CreatedAt = now
                };
                await _lpSnapshotsRepo.InsertAsync(flexSnapshot);
                _logger.LogDebug("Recorded LP snapshot for {Puuid} in Flex: {Tier} {Division} {LP} LP",
                    puuid, flexTier, flexRank, flexLp);
            }
        }
        catch (Exception ex)
        {
            // Don't fail login if LP snapshot recording fails
            _logger.LogWarning(ex, "Failed to record LP snapshots for {Puuid}", puuid);
        }
    }

    private async Task CheckForNewMatchesAsync(RiotAccount account)
    {
        try
        {
            // Get start time for match check (use last sync time or 30 days ago)
            var startTime = account.LastSyncAt?.ToUniversalTime() ??
                           DateTime.UtcNow.AddDays(-30);
            var startTimeEpoch = new DateTimeOffset(startTime).ToUnixTimeSeconds();

            // Check if there are any new matches since last sync
            using var matchIdsDoc = await _riotApiClient.GetMatchHistoryAsync(account.Puuid, 0, 1, startTimeEpoch);
            
            if (matchIdsDoc.RootElement.ValueKind == JsonValueKind.Array &&
                matchIdsDoc.RootElement.GetArrayLength() > 0)
            {
                // New matches found - trigger sync
                _logger.LogInformation("New matches found for {Puuid}, triggering sync", account.Puuid);
                await _riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "pending");
                
                // Notify frontend that sync is starting
                await _syncBroadcaster.BroadcastProgressAsync(account.Puuid, 0, 0);
            }
            else
            {
                _logger.LogDebug("No new matches for {Puuid}", account.Puuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for new matches for {Puuid}", account.Puuid);
            // Don't throw - this is a best-effort check
        }
    }
}

