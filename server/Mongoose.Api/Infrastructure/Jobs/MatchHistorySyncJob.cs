using Microsoft.Extensions.Logging;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Services;
using Mongoose.Api.Infrastructure.Riot.LimitHandler;
using Mongoose.Api.Infrastructure.WebSocket;
using System.Text.Json;

namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// Background job that syncs match history for linked Riot accounts.
/// Polls for accounts with sync_status='pending' and processes them.
/// Uses per-account locking via sync_status to allow concurrent syncs for different accounts.
/// </summary>
public class MatchHistorySyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchHistorySyncJob> _logger;
    private readonly ISyncQueueSignal _queueSignal;
    private readonly ISyncProgressAggregator _aggregator;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _stuckJobThreshold = TimeSpan.FromMinutes(10);

    // Backfill configuration
    private const int MaxBackfillMatches = 300;
    private const int MaxIncrementalMatches = 100;
    private const int DeepAnalysisMatchCount = 100;
    private static readonly TimeSpan BackfillLookbackPeriod = TimeSpan.FromDays(180); // 6 months

    public MatchHistorySyncJob(
        IServiceProvider serviceProvider,
        ILogger<MatchHistorySyncJob> logger,
        ISyncQueueSignal queueSignal,
        ISyncProgressAggregator aggregator)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueSignal = queueSignal;
        _aggregator = aggregator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchHistorySyncJob starting...");

        // On startup: recover any stuck 'syncing' jobs (crash recovery)
        await RecoverStuckJobsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Try to claim and process ONE pending account per iteration
                var processed = await TryProcessNextPendingAccountAsync(stoppingToken);

                if (!processed)
                {
                    // No work to do. Wait for either the next poll tick (fallback for
                    // crash recovery / missed signals) or an explicit wake-up from the
                    // sync endpoint — whichever comes first. The signal makes a queued
                    // sync start within milliseconds instead of up to a full poll interval.
                    //
                    // Cancel the loser via a linked token so the pending task doesn't
                    // linger across iterations (which would otherwise leak waiters and
                    // could let a stale waiter swallow a wake-up permit).
                    using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    var pollTick = Task.Delay(_pollInterval, idleCts.Token);
                    var wakeUp = _queueSignal.WaitAsync(idleCts.Token);
                    await Task.WhenAny(pollTick, wakeUp);
                    idleCts.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MatchHistorySyncJob loop");
                await Task.Delay(_pollInterval, stoppingToken); // Back off on error
            }
        }

        _logger.LogInformation("MatchHistorySyncJob stopped.");
    }

    private async Task RecoverStuckJobsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRiotAccountsRepository>();

            await repo.ResetStuckSyncingAccountsAsync(_stuckJobThreshold);
            _logger.LogInformation("Recovered stuck syncing jobs older than {Threshold}", _stuckJobThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering stuck sync jobs");
        }
    }

    private async Task<bool> TryProcessNextPendingAccountAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var riotAccountsRepo = scope.ServiceProvider.GetRequiredService<IRiotAccountsRepository>();

        // Atomically claim a pending account (returns null if none available or race lost)
        var account = await riotAccountsRepo.ClaimNextPendingForSyncAsync();
        if (account == null)
            return false; // No work

        _logger.LogInformation("Starting sync for account {Puuid} ({GameName}#{TagLine})",
            LogSanitizer.HashForLog(account.Puuid), LogSanitizer.Sanitize(account.GameName), LogSanitizer.Sanitize(account.TagLine));

        // Ensure the Overview aggregate reflects this sync regardless of how it was queued
        // (login auto-sync, Settings, sync-all, or crash recovery). This is the single point
        // every sync flows through, so opening a run here guarantees progress surfaces.
        await EnsureAggregateRunAsync(scope.ServiceProvider, account.Puuid);

        try
        {
            var syncedCount = await SyncAccountMatchesAsync(scope.ServiceProvider, account, ct);

            // Mark completed
            await riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "completed", DateTime.UtcNow);
            _logger.LogInformation("Sync completed for account {Puuid}", LogSanitizer.HashForLog(account.Puuid));

            // Broadcast completion via WebSocket
            var broadcaster = scope.ServiceProvider.GetService<ISyncProgressBroadcaster>();
            if (broadcaster != null)
            {
                await broadcaster.BroadcastCompleteAsync(account.Puuid, syncedCount);
            }
        }
        catch (OperationCanceledException)
        {
            // Don't mark as failed on cancellation - leave as syncing for recovery
            _logger.LogWarning("Sync cancelled for account {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {Puuid}", LogSanitizer.HashForLog(account.Puuid));
            await riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "failed");

            // Broadcast error via WebSocket
            var broadcaster = scope.ServiceProvider.GetService<ISyncProgressBroadcaster>();
            if (broadcaster != null)
            {
                await broadcaster.BroadcastErrorAsync(account.Puuid, ex.Message);
            }
        }

        return true;
    }

    /// <summary>
    /// Opens (or extends) the per-user aggregate run for every user who owns this account,
    /// so the combined Overview progress stream reflects the sync. Non-fatal on failure.
    /// </summary>
    private async Task EnsureAggregateRunAsync(IServiceProvider services, string puuid)
    {
        try
        {
            var userRiotAccountsRepo = services.GetRequiredService<IUserRiotAccountsRepository>();
            var ownerIds = await userRiotAccountsRepo.GetUserIdsByPuuidAsync(puuid);
            foreach (var userId in ownerIds)
            {
                await _aggregator.StartRunAsync(userId, new[] { puuid });
            }
        }
        catch (Exception ex)
        {
            // Don't fail the sync if we can't open the aggregate run — progress just won't
            // surface on the combined Overview stream for this account.
            _logger.LogWarning(ex, "Failed to open aggregate run for {Puuid}", LogSanitizer.HashForLog(puuid));
        }
    }

    /// <summary>
    /// Syncs matches for a Riot account. Returns the number of matches processed.
    /// </summary>
    private async Task<int> SyncAccountMatchesAsync(
        IServiceProvider services,
        RiotAccount account,
        CancellationToken ct)
    {
        var riotApiClient = services.GetRequiredService<IRiotApiClient>();
        var riotAccountsRepo = services.GetRequiredService<IRiotAccountsRepository>();
        var matchesRepo = services.GetRequiredService<IMatchesRepository>();
        var participantsRepo = services.GetRequiredService<IParticipantsRepository>();
        var duoMetricsRepo = services.GetRequiredService<IDuoMetricsRepository>();
        var persistenceService = services.GetRequiredService<IMatchDataPersistenceService>();
        var broadcaster = services.GetService<ISyncProgressBroadcaster>();

        // TEMPORARY: Subscribe to rate limit events to notify UI when waiting on Riot API
        // When any rate limit is hit, all syncing accounts are affected since they share the rate limiter.
        // We broadcast to this account's UI regardless of which PUUID triggered the rate limit.
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        EventHandler<RateLimitWaitEventArgs>? rateLimitHandler = null;
        if (broadcaster != null)
        {
            rateLimitHandler = (sender, e) =>
            {
                // Fire-and-forget broadcast (event handler is synchronous)
                // All syncing accounts should see the rate limit message since they share the limiter
                _ = broadcaster.BroadcastRateLimitedAsync(account.Puuid);
            };
            riotApiClient.RateLimitWaitStarted += rateLimitHandler;
        }

        try
        {
            var processed = await SyncAccountMatchesInternalAsync(
                riotApiClient, riotAccountsRepo, matchesRepo, participantsRepo, duoMetricsRepo,
                persistenceService, broadcaster, account, ct);

            // Read the rank now and attribute any waiting reading to the match it belongs to
            await CaptureRankAsync(services, riotApiClient, account, ct);

            return processed;
        }
        finally
        {
            // TEMPORARY: Unsubscribe from rate limit events
            // TODO: Remove this once we have a more sophisticated rate limiting UX.
            if (rateLimitHandler != null)
            {
                riotApiClient.RateLimitWaitStarted -= rateLimitHandler;
            }
        }
    }

    /// <summary>
    /// Internal implementation of match sync logic.
    /// Extracted to allow proper event handler cleanup in the calling method.
    /// TEMPORARY: This extraction is for rate limit event handling.
    /// TODO: Merge back into SyncAccountMatchesAsync once rate limiting UX is removed.
    /// </summary>
    private async Task<int> SyncAccountMatchesInternalAsync(
        IRiotApiClient riotApiClient,
        IRiotAccountsRepository riotAccountsRepo,
        IMatchesRepository matchesRepo,
        IParticipantsRepository participantsRepo,
        IDuoMetricsRepository duoMetricsRepo,
        IMatchDataPersistenceService persistenceService,
        ISyncProgressBroadcaster? broadcaster,
        RiotAccount account,
        CancellationToken ct)
    {
        // Determine if this is an initial backfill or incremental sync
        bool isInitialSync = !account.LastSyncAt.HasValue;

        // 1. Fetch existing match IDs to avoid re-processing
        var existingMatchIds = await participantsRepo.GetMatchIdsForPuuidAsync(account.Puuid);
        var existingSet = new HashSet<string>(existingMatchIds, StringComparer.OrdinalIgnoreCase);

        // Compute startTime based on sync type:
        // - Initial sync: look back 6 months
        // - Incremental sync: use LastSyncAt
        long startTime;
        int maxMatches;

        if (isInitialSync)
        {
            var backfillStart = DateTime.UtcNow - BackfillLookbackPeriod;
            startTime = new DateTimeOffset(backfillStart).ToUnixTimeSeconds();
            maxMatches = MaxBackfillMatches;
            _logger.LogInformation("Starting initial backfill for {Puuid} (last 6 months, max {MaxMatches} matches)",
                LogSanitizer.HashForLog(account.Puuid), maxMatches);
        }
        else
        {
            startTime = new DateTimeOffset(account.LastSyncAt!.Value).ToUnixTimeSeconds();
            maxMatches = MaxIncrementalMatches;
            _logger.LogInformation("Starting incremental sync for {Puuid} (since {LastSync})",
                LogSanitizer.HashForLog(account.Puuid), account.LastSyncAt);
        }

        // 2. Fetch new match IDs from Riot
        // For initial sync: don't stop early on existing matches (they may have been created by another account's sync)
        // For incremental sync: stop early when we hit an existing match (we trust previous syncs completed)
        var matchIds = await FetchNewMatchIdsAsync(riotApiClient, account.Puuid, existingSet, startTime, maxMatches, isInitialSync, ct);

        _logger.LogInformation("Found {Count} new matches for {Puuid}", matchIds.Count, LogSanitizer.HashForLog(account.Puuid));

        // 3. Process each match
        int processed = 0;
        int total = matchIds.Count;

        // Update total for progress tracking
        await riotAccountsRepo.UpdateSyncProgressAsync(account.Puuid, 0, total);

        // Broadcast initial progress (0/total)
        if (broadcaster != null)
        {
            await broadcaster.BroadcastProgressAsync(account.Puuid, 0, total);
        }

        for (int i = 0; i < matchIds.Count; i++)
        {
            var matchId = matchIds[i];
            ct.ThrowIfCancellationRequested();

            try
            {
                // Fetch match info (always needed)
                using var matchInfo = await riotApiClient.GetMatchInfoAsync(matchId, ct);

                // Fetch timeline only for:
                // - All matches in incremental sync (they're recent)
                // - First DeepAnalysisMatchCount matches in initial backfill
                JsonDocument? timeline = null;
                bool needsTimeline = !isInitialSync || i < DeepAnalysisMatchCount;

                if (needsTimeline)
                {
                    timeline = await riotApiClient.GetMatchTimelineAsync(matchId, ct);
                }

                // Persist match and participant data
                await persistenceService.PersistMatchDataAsync(matchInfo.RootElement, timeline?.RootElement);

                timeline?.Dispose();

                _logger.LogDebug("Processed match {MatchId} ({Processed}/{Total}) for {Puuid}{TimelineInfo}",
                    LogSanitizer.Sanitize(matchId), processed + 1, total, LogSanitizer.HashForLog(account.Puuid),
                    LogSanitizer.Sanitize(needsTimeline ? " (with timeline)" : " (info only)"));
            }
            catch (TaskCanceledException)
            {
                // Timeout - common for timeline requests, don't log full stack trace
                _logger.LogDebug("Timeout fetching match {MatchId} for {Puuid} - skipping", LogSanitizer.Sanitize(matchId), LogSanitizer.HashForLog(account.Puuid));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Match not found - skip silently
                _logger.LogDebug("Match {MatchId} not found (404) - skipping", LogSanitizer.Sanitize(matchId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process match {MatchId} for {Puuid} - skipping", LogSanitizer.Sanitize(matchId), LogSanitizer.HashForLog(account.Puuid));
            }
            finally
            {
                // Always increment progress counter, even for failed matches
                // This ensures progress bar moves forward and doesn't appear stuck
                processed++;

                // Update progress after each match (success or failure)
                await riotAccountsRepo.UpdateSyncProgressAsync(account.Puuid, processed, total);

                // Broadcast progress via WebSocket
                if (broadcaster != null)
                {
                    await broadcaster.BroadcastProgressAsync(account.Puuid, processed, total, matchId);
                }
            }
        }

        _logger.LogInformation("Synced {Processed}/{Total} matches for {Puuid}", processed, total, LogSanitizer.HashForLog(account.Puuid));

        return processed;
    }

    /// <summary>
    /// Stores the rank after a sync as a snapshot and attributes pending snapshots of this account to
    /// the matches the sync just stored (see RankSnapshotService). Never fails the sync.
    /// </summary>
    private async Task CaptureRankAsync(
        IServiceProvider services,
        IRiotApiClient riotApiClient,
        RiotAccount account,
        CancellationToken ct)
    {
        try
        {
            var rankSnapshots = services.GetRequiredService<RankSnapshotService>();
            await rankSnapshots.CaptureAsync(account, RankSnapshotSource.Sync, riotApiClient, ct);
            await rankSnapshots.AttributePendingAsync(account.Puuid, DateTime.UtcNow, queueSyncWhenMissing: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture rank for {Puuid}", LogSanitizer.HashForLog(account.Puuid));
        }
    }

    /// <summary>
    /// Fetches new match IDs from the Riot API.
    /// For incremental syncs: stops when it hits an existing match (caught up) or reaches the maxMatches limit.
    /// For initial syncs: fetches all matches up to the limit, filtering out existing ones
    ///   (because existing matches may have been created by another account's sync, not this account's previous sync).
    /// </summary>
    private async Task<IList<string>> FetchNewMatchIdsAsync(
        IRiotApiClient riotApiClient,
        string puuid,
        HashSet<string> existingMatchIds,
        long startTime,
        int maxMatches,
        bool isInitialSync,
        CancellationToken ct)
    {
        var newMatchIds = new List<string>();
        const int pageSize = 100;
        int start = 0;
        bool keepFetching = true;
        int totalFetched = 0; // Track total matches fetched from API (for initial sync limit)

        while (keepFetching)
        {
            using var matchesDoc = await riotApiClient.GetMatchHistoryAsync(puuid, start, pageSize, startTime, ct);
            var root = matchesDoc.RootElement;

            // Riot returns an array of match ID strings
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                break;

            int pageCount = root.GetArrayLength();

            for (int i = 0; i < pageCount; i++)
            {
                var matchId = root[i].GetString();
                if (string.IsNullOrEmpty(matchId))
                    continue;

                totalFetched++;

                if (!existingMatchIds.Contains(matchId))
                {
                    newMatchIds.Add(matchId);
                }
                else if (!isInitialSync)
                {
                    // Incremental sync: found an existing match - we've caught up, stop fetching
                    // For initial sync: continue fetching (this match may have been synced by another account)
                    keepFetching = false;
                    break;
                }
                // For initial sync: skip this match but keep fetching older ones

                // Check if we've hit the cap on new matches to process
                if (newMatchIds.Count >= maxMatches)
                {
                    keepFetching = false;
                    break;
                }

                // For initial sync: use a generous safety cap to prevent infinite loops
                const int initialSyncSafetyCap = 1500;
                if (isInitialSync && totalFetched >= initialSyncSafetyCap)
                {
                    _logger.LogWarning("Initial sync hit safety cap ({Cap}) for puuid, stopping fetch", initialSyncSafetyCap);
                    keepFetching = false;
                    break;
                }
            }

            // If we got less than page size, we've reached the end
            if (pageCount < pageSize)
                break;

            start += pageSize;
        }

        return newMatchIds;
    }
}
