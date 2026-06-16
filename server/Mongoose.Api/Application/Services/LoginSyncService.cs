using System.Text.Json;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot;
using Mongoose.Api.Infrastructure.WebSocket;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Handles sync checks when a user logs in.
/// Checks for new matches and updates profile data for linked Riot accounts.
/// </summary>
public class LoginSyncService
{
    private readonly IRiotAccountsRepository _riotAccountsRepo;
    private readonly IUserRiotAccountsRepository _userRiotAccountsRepo;
    private readonly IRiotApiClient _riotApiClient;
    private readonly ISyncProgressBroadcaster _syncBroadcaster;
    private readonly ISyncProgressAggregator _syncAggregator;
    private readonly ISyncQueueSignal _queueSignal;
    private readonly ILogger<LoginSyncService> _logger;

    /// <summary>
    /// Cooldown period - don't re-check if last sync was within this timeframe.
    /// </summary>
    private static readonly TimeSpan SyncCooldown = TimeSpan.FromMinutes(5);

    public LoginSyncService(
        IRiotAccountsRepository riotAccountsRepo,
        IUserRiotAccountsRepository userRiotAccountsRepo,
        IRiotApiClient riotApiClient,
        ISyncProgressBroadcaster syncBroadcaster,
        ISyncProgressAggregator syncAggregator,
        ISyncQueueSignal queueSignal,
        ILogger<LoginSyncService> logger)
    {
        _riotAccountsRepo = riotAccountsRepo;
        _userRiotAccountsRepo = userRiotAccountsRepo;
        _riotApiClient = riotApiClient;
        _syncBroadcaster = syncBroadcaster;
        _syncAggregator = syncAggregator;
        _queueSignal = queueSignal;
        _logger = logger;
    }

    /// <summary>
    /// Check all linked Riot accounts for a user on login.
    /// Updates profile data and triggers sync if new matches are found.
    /// </summary>
    public async Task CheckAccountsOnLoginAsync(long userId)
    {
        _logger.LogInformation("Starting login sync check for user {UserId}", LogSanitizer.Sanitize(userId.ToString()));
        try
        {
            // Get linked accounts via junction table
            var linkedAccounts = await _userRiotAccountsRepo.GetByUserIdAsync(userId);
            if (linkedAccounts == null || linkedAccounts.Count == 0)
            {
                _logger.LogInformation("No linked Riot accounts for user {UserId}", LogSanitizer.Sanitize(userId.ToString()));
                return;
            }

            _logger.LogInformation("Found {Count} linked Riot accounts for user {UserId}", linkedAccounts.Count, LogSanitizer.Sanitize(userId.ToString()));
            var queuedPuuids = new List<string>();
            foreach (var (_, account) in linkedAccounts)
            {
                if (await CheckAccountAsync(account))
                {
                    queuedPuuids.Add(account.Puuid);
                }
            }

            // If the login check queued any account, open a combined aggregate run so the
            // Overview card reflects the login-triggered sync (the per-account broadcasts alone
            // are ignored by the aggregator without a run), and wake the job to start now.
            if (queuedPuuids.Count > 0)
            {
                await _syncAggregator.StartRunAsync(userId, queuedPuuids);
                _queueSignal.Notify();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking accounts on login for user {UserId}", LogSanitizer.Sanitize(userId.ToString()));
            // Don't throw - login should succeed even if sync check fails
        }
    }

    /// <summary>
    /// Returns true if this account was queued for a match sync (so the caller can include it
    /// in the aggregate run).
    /// </summary>
    private async Task<bool> CheckAccountAsync(RiotAccount account)
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
                    LogSanitizer.HashForLog(account.Puuid), account.LastSyncAt);
                return false;
            }

            // Skip match sync if already syncing or pending
            if (account.SyncStatus == "syncing" || account.SyncStatus == "pending")
            {
                _logger.LogDebug("Skipping match sync check for {Puuid} - already {Status}",
                    LogSanitizer.HashForLog(account.Puuid), LogSanitizer.Sanitize(account.SyncStatus));
                return false;
            }

            // Check for new matches and trigger sync if needed
            return await CheckForNewMatchesAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking account {Puuid} on login", LogSanitizer.HashForLog(account.Puuid));
            // Don't throw - continue with other accounts
            return false;
        }
    }

    private async Task UpdateProfileDataAsync(RiotAccount account)
    {
        _logger.LogInformation("Fetching profile data for {GameName}#{TagLine} ({Region})",
            LogSanitizer.Sanitize(account.GameName), LogSanitizer.Sanitize(account.TagLine), LogSanitizer.Sanitize(account.Region));
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
                        LogSanitizer.HashForLog(account.Puuid), profileIconId, summonerLevel);
                }
            }
            else
            {
                _logger.LogWarning("Invalid summoner response for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            }

            // Update rank data using PUUID (new Riot API endpoint)
            await UpdateRankDataAsync(account);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update profile data for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
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
                    LogSanitizer.HashForLog(account.Puuid), LogSanitizer.Sanitize(soloTier), LogSanitizer.Sanitize(soloRank), LogSanitizer.Sanitize(flexTier), LogSanitizer.Sanitize(flexRank));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update rank data for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            // Don't throw - rank data is optional
        }
    }

    /// <summary>
    /// Returns true if new matches were found and the account was queued for sync.
    /// </summary>
    private async Task<bool> CheckForNewMatchesAsync(RiotAccount account)
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
                _logger.LogInformation("New matches found for {Puuid}, triggering sync", LogSanitizer.HashForLog(account.Puuid));
                await _riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "pending");

                // Notify the per-account channel (Settings page) that sync is starting.
                // The Overview aggregate run is opened by the caller once all accounts are checked.
                await _syncBroadcaster.BroadcastProgressAsync(account.Puuid, 0, 0);
                return true;
            }

            _logger.LogDebug("No new matches for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for new matches for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            // Don't throw - this is a best-effort check
            return false;
        }
    }
}

