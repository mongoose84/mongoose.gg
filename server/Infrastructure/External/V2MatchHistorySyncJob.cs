using Microsoft.Extensions.Logging;
using RiotProxy.External.Domain.Entities.V2;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using RiotProxy.Infrastructure.External.Riot;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External;

/// <summary>
/// Background job that syncs match history for linked Riot accounts.
/// Polls for accounts with sync_status='pending' and processes them.
/// Uses per-account locking via sync_status to allow concurrent syncs for different accounts.
/// </summary>
public class V2MatchHistorySyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<V2MatchHistorySyncJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _stuckJobThreshold = TimeSpan.FromMinutes(10);

    public V2MatchHistorySyncJob(IServiceProvider serviceProvider, ILogger<V2MatchHistorySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("V2MatchHistorySyncJob starting...");

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
                    // No work to do, sleep before next poll
                    await Task.Delay(_pollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in V2MatchHistorySyncJob loop");
                await Task.Delay(_pollInterval, stoppingToken); // Back off on error
            }
        }

        _logger.LogInformation("V2MatchHistorySyncJob stopped.");
    }

    private async Task RecoverStuckJobsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<V2RiotAccountsRepository>();

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
        var riotAccountsRepo = scope.ServiceProvider.GetRequiredService<V2RiotAccountsRepository>();

        // Atomically claim a pending account (returns null if none available or race lost)
        var account = await riotAccountsRepo.ClaimNextPendingForSyncAsync();
        if (account == null)
            return false; // No work

        _logger.LogInformation("Starting sync for account {Puuid} ({GameName}#{TagLine})",
            account.Puuid, account.GameName, account.TagLine);

        try
        {
            await SyncAccountMatchesAsync(scope.ServiceProvider, account, ct);

            // Mark completed
            await riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "completed", DateTime.UtcNow);
            _logger.LogInformation("Sync completed for account {Puuid}", account.Puuid);
        }
        catch (OperationCanceledException)
        {
            // Don't mark as failed on cancellation - leave as syncing for recovery
            _logger.LogWarning("Sync cancelled for account {Puuid}", account.Puuid);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {Puuid}", account.Puuid);
            await riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "failed");
        }

        return true;
    }

    private async Task SyncAccountMatchesAsync(
        IServiceProvider services,
        V2RiotAccount account,
        CancellationToken ct)
    {
        var riotApiClient = services.GetRequiredService<IRiotApiClient>();
        var riotAccountsRepo = services.GetRequiredService<V2RiotAccountsRepository>();
        var v2Matches = services.GetRequiredService<V2MatchesRepository>();
        var v2Participants = services.GetRequiredService<V2ParticipantsRepository>();
        var v2Checkpoints = services.GetRequiredService<V2ParticipantCheckpointsRepository>();
        var v2PartMetrics = services.GetRequiredService<V2ParticipantMetricsRepository>();
        var v2TeamObjectives = services.GetRequiredService<V2TeamObjectivesRepository>();
        var v2PartObjectives = services.GetRequiredService<V2ParticipantObjectivesRepository>();
        var v2TeamMetrics = services.GetRequiredService<V2TeamMatchMetricsRepository>();
        var v2DuoMetrics = services.GetRequiredService<V2DuoMetricsRepository>();

        // 1. Fetch existing match IDs to avoid re-processing
        var existingMatchIds = await v2Participants.GetMatchIdsForPuuidAsync(account.Puuid);
        var existingSet = new HashSet<string>(existingMatchIds, StringComparer.OrdinalIgnoreCase);

        // Use startTime filter if we have LastSyncAt
        long? startTime = account.LastSyncAt.HasValue
            ? new DateTimeOffset(account.LastSyncAt.Value).ToUnixTimeSeconds()
            : null;

        // 2. Fetch new match IDs from Riot
        var matchIds = await FetchNewMatchIdsAsync(riotApiClient, account.Puuid, existingSet, startTime, ct);

        _logger.LogInformation("Found {Count} new matches for {Puuid}", matchIds.Count, account.Puuid);

        // 3. Process each match
        int processed = 0;
        int total = matchIds.Count;

        // Update total for progress tracking
        await riotAccountsRepo.UpdateSyncProgressAsync(account.Puuid, 0, total);

        foreach (var matchId in matchIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Fetch match info and timeline from Riot API (rate limited)
                var matchInfo = await riotApiClient.GetMatchInfoAsync(matchId, ct);
                var timeline = await riotApiClient.GetMatchTimelineAsync(matchId, ct);

                // Persist to V2 tables - reuse the mapping logic from MatchHistorySyncJob
                await PersistMatchDataAsync(
                    matchId, matchInfo, timeline,
                    v2Matches, v2Participants, v2Checkpoints,
                    v2PartMetrics, v2TeamObjectives, v2PartObjectives,
                    v2TeamMetrics, v2DuoMetrics, ct);

                _logger.LogDebug("Processed match {MatchId} ({Processed}/{Total}) for {Puuid}",
                    matchId, processed + 1, total, account.Puuid);
            }
            catch (TaskCanceledException)
            {
                // Timeout - common for timeline requests, don't log full stack trace
                _logger.LogDebug("Timeout fetching match {MatchId} for {Puuid} - skipping", matchId, account.Puuid);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Match not found - skip silently
                _logger.LogDebug("Match {MatchId} not found (404) - skipping", matchId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process match {MatchId} for {Puuid} - skipping", matchId, account.Puuid);
            }
            finally
            {
                // Always increment progress counter, even for failed matches
                // This ensures progress bar moves forward and doesn't appear stuck
                processed++;

                // Update progress after each match (success or failure)
                await riotAccountsRepo.UpdateSyncProgressAsync(account.Puuid, processed, total);

                // TODO: When IWebSocketBroadcaster is implemented (F13), broadcast progress here
                // await broadcaster.SendSyncProgressAsync(account.UserId, account.Id, processed, total, matchId);
            }
        }

        _logger.LogInformation("Synced {Processed}/{Total} matches for {Puuid}", processed, total, account.Puuid);
    }

    private async Task<IList<string>> FetchNewMatchIdsAsync(
        IRiotApiClient riotApiClient,
        string puuid,
        HashSet<string> existingMatchIds,
        long? startTime,
        CancellationToken ct)
    {
        var newMatchIds = new List<string>();
        const int pageSize = 100;
        int start = 0;
        bool keepFetching = true;

        while (keepFetching)
        {
            var matches = await riotApiClient.GetMatchHistoryAsync(puuid, start, pageSize, startTime, ct);

            if (matches.Count == 0)
                break;

            foreach (var match in matches)
            {
                if (!existingMatchIds.Contains(match.MatchId))
                {
                    newMatchIds.Add(match.MatchId);
                }
                else
                {
                    // Found an existing match - we've caught up, stop fetching
                    keepFetching = false;
                    break;
                }
            }

            // If we got less than page size, we've reached the end
            if (matches.Count < pageSize)
                break;

            start += pageSize;

            // Safety limit: max 500 matches per sync
            if (newMatchIds.Count >= 500)
                break;
        }

        return newMatchIds;
    }

    private async Task PersistMatchDataAsync(
        string matchId,
        JsonDocument matchInfo,
        JsonDocument timeline,
        V2MatchesRepository v2Matches,
        V2ParticipantsRepository v2Participants,
        V2ParticipantCheckpointsRepository v2Checkpoints,
        V2ParticipantMetricsRepository v2PartMetrics,
        V2TeamObjectivesRepository v2TeamObjectives,
        V2ParticipantObjectivesRepository v2PartObjectives,
        V2TeamMatchMetricsRepository v2TeamMetrics,
        V2DuoMetricsRepository v2DuoMetrics,
        CancellationToken ct)
    {
        // Persist match
        var v2Match = MatchHistorySyncJob.MapToV2Match(matchInfo, matchId);
        await v2Matches.UpsertAsync(v2Match);

        // Persist participants (InsertAsync is actually an upsert - INSERT ON DUPLICATE KEY UPDATE)
        var participants = MatchHistorySyncJob.MapToV2Participants(matchInfo, matchId);
        foreach (var p in participants)
        {
            await v2Participants.InsertAsync(p);
        }

        // Persist timeline-derived data (checkpoints, metrics, objectives)
        await MatchHistorySyncJob.PersistV2TimelineDerivedAsync(
            matchId, matchInfo, timeline,
            v2Participants, v2Checkpoints, v2PartMetrics,
            v2TeamObjectives, v2PartObjectives, v2TeamMetrics, v2DuoMetrics,
            ct);
    }
}

