using Microsoft.Extensions.Logging;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Infrastructure.External.Riot.Mappers;
using RiotProxy.Infrastructure.WebSocket;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External;

/// <summary>
/// Background job that syncs match history for linked Riot accounts.
/// Polls for accounts with sync_status='pending' and processes them.
/// Uses per-account locking via sync_status to allow concurrent syncs for different accounts.
/// </summary>
public class MatchHistorySyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchHistorySyncJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _stuckJobThreshold = TimeSpan.FromMinutes(10);

    // Backfill configuration
    private const int MaxBackfillMatches = 300;
    private const int MaxIncrementalMatches = 100;
    private const int DeepAnalysisMatchCount = 100;
    private static readonly TimeSpan BackfillLookbackPeriod = TimeSpan.FromDays(180); // 6 months

    public MatchHistorySyncJob(IServiceProvider serviceProvider, ILogger<MatchHistorySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            var repo = scope.ServiceProvider.GetRequiredService<RiotAccountsRepository>();

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
        var riotAccountsRepo = scope.ServiceProvider.GetRequiredService<RiotAccountsRepository>();

        // Atomically claim a pending account (returns null if none available or race lost)
        var account = await riotAccountsRepo.ClaimNextPendingForSyncAsync();
        if (account == null)
            return false; // No work

        _logger.LogInformation("Starting sync for account {Puuid} ({GameName}#{TagLine})",
            account.Puuid, account.GameName, account.TagLine);

        try
        {
            var syncedCount = await SyncAccountMatchesAsync(scope.ServiceProvider, account, ct);

            // Mark completed
            await riotAccountsRepo.UpdateSyncStatusAsync(account.Puuid, "completed", DateTime.UtcNow);
            _logger.LogInformation("Sync completed for account {Puuid}", account.Puuid);

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
            _logger.LogWarning("Sync cancelled for account {Puuid}", account.Puuid);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {Puuid}", account.Puuid);
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
    /// Syncs matches for a Riot account. Returns the number of matches processed.
    /// </summary>
    private async Task<int> SyncAccountMatchesAsync(
        IServiceProvider services,
        RiotAccount account,
        CancellationToken ct)
    {
        var riotApiClient = services.GetRequiredService<IRiotApiClient>();
        var riotAccountsRepo = services.GetRequiredService<RiotAccountsRepository>();
        var matchesRepo = services.GetRequiredService<MatchesRepository>();
        var participantsRepo = services.GetRequiredService<ParticipantsRepository>();
        var checkpointsRepo = services.GetRequiredService<ParticipantCheckpointsRepository>();
        var partMetricsRepo = services.GetRequiredService<ParticipantMetricsRepository>();
        var teamObjectivesRepo = services.GetRequiredService<TeamObjectivesRepository>();
        var partObjectivesRepo = services.GetRequiredService<ParticipantObjectivesRepository>();
        var teamMetricsRepo = services.GetRequiredService<TeamMatchMetricsRepository>();
        var duoMetricsRepo = services.GetRequiredService<DuoMetricsRepository>();
        var teamRoleRepo = services.GetRequiredService<TeamRoleResponsibilitiesRepository>();
        var seasonsRepo = services.GetRequiredService<SeasonsRepository>();
        var lpSnapshotsRepo = services.GetRequiredService<ILpSnapshotsRepository>();
        var broadcaster = services.GetService<ISyncProgressBroadcaster>();

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
                account.Puuid, maxMatches);
        }
        else
        {
            startTime = new DateTimeOffset(account.LastSyncAt!.Value).ToUnixTimeSeconds();
            maxMatches = MaxIncrementalMatches;
            _logger.LogInformation("Starting incremental sync for {Puuid} (since {LastSync})",
                account.Puuid, account.LastSyncAt);
        }

        // 2. Fetch new match IDs from Riot
        // For initial sync: don't stop early on existing matches (they may have been created by another account's sync)
        // For incremental sync: stop early when we hit an existing match (we trust previous syncs completed)
        var matchIds = await FetchNewMatchIdsAsync(riotApiClient, account.Puuid, existingSet, startTime, maxMatches, isInitialSync, ct);

        _logger.LogInformation("Found {Count} new matches for {Puuid}", matchIds.Count, account.Puuid);

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
                await PersistMatchDataAsync(
                    matchInfo.RootElement,
                    timeline?.RootElement,
                    matchesRepo,
                    participantsRepo,
                    teamObjectivesRepo,
                    partMetricsRepo,
                    checkpointsRepo,
                    partObjectivesRepo,
                    teamMetricsRepo,
                    teamRoleRepo,
                    seasonsRepo);

                timeline?.Dispose();

                _logger.LogDebug("Processed match {MatchId} ({Processed}/{Total}) for {Puuid}{TimelineInfo}",
                    matchId, processed + 1, total, account.Puuid,
                    needsTimeline ? " (with timeline)" : " (info only)");
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

                // Broadcast progress via WebSocket
                if (broadcaster != null)
                {
                    await broadcaster.BroadcastProgressAsync(account.Puuid, processed, total, matchId);
                }
            }
        }

        _logger.LogInformation("Synced {Processed}/{Total} matches for {Puuid}", processed, total, account.Puuid);

        // Record LP snapshots for time-series tracking (always, regardless of matches synced)
        // This provides honest LP progression data independent of specific matches
        await RecordLpSnapshotsAsync(riotApiClient, lpSnapshotsRepo, account, ct);

        // Legacy: Also update LP on most recent ranked match for backward compatibility
        // TODO: Consider removing this once UI fully transitions to LP snapshots
        if (processed > 0)
        {
            await UpdateLpForMostRecentRankedMatchAsync(
                riotApiClient,
                participantsRepo,
                matchesRepo,
                account,
                ct);
        }

        return processed;
    }

    /// <summary>
    /// Fetches current LP from League API and updates the most recent ranked match's participant record.
    /// Only works accurately for the most recent match; historical LP cannot be determined.
    /// </summary>
    private async Task UpdateLpForMostRecentRankedMatchAsync(
        IRiotApiClient riotApiClient,
        ParticipantsRepository participantsRepo,
        MatchesRepository matchesRepo,
        RiotAccount account,
        CancellationToken ct)
    {
        try
        {
            // Find the most recent ranked match for this player
            // Queue IDs: 420 = Ranked Solo/Duo, 440 = Ranked Flex
            var recentRankedMatches = await matchesRepo.GetRecentMatchHeadersAsync(account.Puuid, null, 1);

            // Filter to ranked matches - we need to check if any recent matches are ranked
            var rankedSoloMatches = await matchesRepo.GetRecentMatchHeadersAsync(account.Puuid, 420, 1);
            var rankedFlexMatches = await matchesRepo.GetRecentMatchHeadersAsync(account.Puuid, 440, 1);

            // Determine which ranked match is more recent
            var mostRecentRankedMatch = rankedSoloMatches.Count > 0 && rankedFlexMatches.Count > 0
                ? (rankedSoloMatches[0].GameStartTime > rankedFlexMatches[0].GameStartTime
                    ? rankedSoloMatches[0]
                    : rankedFlexMatches[0])
                : rankedSoloMatches.Count > 0
                    ? rankedSoloMatches[0]
                    : rankedFlexMatches.FirstOrDefault();

            if (mostRecentRankedMatch == null)
            {
                _logger.LogDebug("No ranked matches found for {Puuid}, skipping LP update", account.Puuid);
                return;
            }

            // Fetch current LP from League API
            using var leagueDoc = await riotApiClient.GetLeagueEntriesByPuuidAsync(account.Region, account.Puuid, ct);

            string? tier = null, rank = null;
            int? lp = null;
            string queueType = mostRecentRankedMatch.QueueId == 420 ? "RANKED_SOLO_5x5" : "RANKED_FLEX_SR";

            foreach (var entry in leagueDoc.RootElement.EnumerateArray())
            {
                var entryQueueType = entry.GetProperty("queueType").GetString();
                if (entryQueueType == queueType)
                {
                    tier = entry.GetProperty("tier").GetString();
                    rank = entry.GetProperty("rank").GetString();
                    lp = entry.GetProperty("leaguePoints").GetInt32();
                    break;
                }
            }

            if (tier != null && lp.HasValue)
            {
                await participantsRepo.UpdateLpDataAsync(
                    mostRecentRankedMatch.MatchId,
                    account.Puuid,
                    lp.Value,
                    tier,
                    rank);

                _logger.LogDebug("Updated LP for {Puuid} on match {MatchId}: {Tier} {Rank} {LP} LP",
                    account.Puuid, mostRecentRankedMatch.MatchId, tier, rank, lp);
            }
            else
            {
                _logger.LogDebug("No ranked data found for {Puuid} in queue {QueueType}", account.Puuid, queueType);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the sync if LP update fails
            _logger.LogWarning(ex, "Failed to update LP data for {Puuid}", account.Puuid);
        }
    }

    /// <summary>
    /// Records LP snapshots for all ranked queues the player participates in.
    /// This provides honest time-series LP data independent of specific matches.
    /// Called on every sync to build LP progression history.
    /// </summary>
    private async Task RecordLpSnapshotsAsync(
        IRiotApiClient riotApiClient,
        ILpSnapshotsRepository lpSnapshotsRepo,
        RiotAccount account,
        CancellationToken ct)
    {
        try
        {
            using var leagueDoc = await riotApiClient.GetLeagueEntriesByPuuidAsync(account.Region, account.Puuid, ct);
            var now = DateTime.UtcNow;

            foreach (var entry in leagueDoc.RootElement.EnumerateArray())
            {
                var queueType = entry.GetProperty("queueType").GetString();

                // Only record Solo/Duo and Flex queues
                if (queueType != "RANKED_SOLO_5x5" && queueType != "RANKED_FLEX_SR")
                    continue;

                var tier = entry.GetProperty("tier").GetString();
                var division = entry.GetProperty("rank").GetString();
                var lp = entry.GetProperty("leaguePoints").GetInt32();

                if (string.IsNullOrEmpty(tier) || string.IsNullOrEmpty(division))
                    continue;

                var snapshot = new LpSnapshot
                {
                    Puuid = account.Puuid,
                    QueueType = queueType,
                    Tier = tier,
                    Division = division,
                    Lp = lp,
                    RecordedAt = now,
                    CreatedAt = now
                };

                await lpSnapshotsRepo.InsertAsync(snapshot);
                _logger.LogDebug("Recorded LP snapshot for {Puuid} in {Queue}: {Tier} {Division} {LP} LP",
                    account.Puuid, queueType, tier, division, lp);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the sync if LP snapshot recording fails
            _logger.LogWarning(ex, "Failed to record LP snapshots for {Puuid}", account.Puuid);
        }
    }

    /// <summary>
    /// Fetches new match IDs from the Riot API.
    /// For incremental syncs: stops when it hits an existing match (caught up) or reaches the maxMatches limit.
    /// For initial syncs: fetches all matches up to the limit, filtering out existing ones
    ///   (because existing matches may have been created by another account's sync, not this account's previous sync).
    /// </summary>
    /// <param name="riotApiClient">The Riot API client</param>
    /// <param name="puuid">The player's PUUID</param>
    /// <param name="existingMatchIds">Set of match IDs where this PUUID already has a participant record</param>
    /// <param name="startTime">Unix timestamp (seconds) - only fetch matches after this time</param>
    /// <param name="maxMatches">Maximum number of matches to fetch (300 for backfill, 100 for incremental)</param>
    /// <param name="isInitialSync">True for initial backfill (don't stop early), false for incremental (stop on existing)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of new match IDs, ordered from newest to oldest</returns>
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
                // while still allowing completeness within the 6-month window.
                // With 6 months and ~10 games/day max, theoretical max is ~1800 matches.
                // Use 1500 as a reasonable safety cap that allows completeness for most players.
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

    /// <summary>
    /// Persists match data to the database using the mappers.
    /// </summary>
    private async Task PersistMatchDataAsync(
        JsonElement matchRoot,
        JsonElement? timelineRoot,
        MatchesRepository matchesRepo,
        ParticipantsRepository participantsRepo,
        TeamObjectivesRepository teamObjectivesRepo,
        ParticipantMetricsRepository partMetricsRepo,
        ParticipantCheckpointsRepository checkpointsRepo,
        ParticipantObjectivesRepository partObjectivesRepo,
        TeamMatchMetricsRepository teamMetricsRepo,
        TeamRoleResponsibilitiesRepository teamRoleRepo,
        SeasonsRepository seasonsRepo)
    {
        // 1. Map and persist match
        var match = RiotMatchMapper.MapMatch(matchRoot);

        // Calculate and ensure season exists, then set on match
        match.SeasonCode = await Riot.SeasonHelper.EnsureSeasonExistsAsync(
            seasonsRepo,
            match.PatchVersion,
            match.GameStartTime);

        await matchesRepo.UpsertAsync(match);

        // 2. Map and persist participants
        var participants = RiotMatchMapper.MapParticipants(matchRoot);
        var participantIdMap = new Dictionary<int, long>(); // Riot participantId (1-10) -> DB id
        var participantTeams = new Dictionary<int, int>();
        var participantRoles = new Dictionary<int, string?>();

        var info = matchRoot.GetProperty("info");
        var gameDurationSec = info.GetProperty("gameDuration").GetInt32();

        // Calculate team totals for metrics
        var teamKills = new Dictionary<int, int> { { 100, 0 }, { 200, 0 } };
        var teamDamage = new Dictionary<int, int> { { 100, 0 }, { 200, 0 } };

        foreach (var p in info.GetProperty("participants").EnumerateArray())
        {
            var teamId = p.GetProperty("teamId").GetInt32();
            teamKills[teamId] += p.GetProperty("kills").GetInt32();
            teamDamage[teamId] += p.GetProperty("totalDamageDealtToChampions").GetInt32();
        }

        // Insert participants and build lookup maps
        _logger.LogDebug("Inserting {Count} participants for match {MatchId}", participants.Count, match.MatchId);
        int riotParticipantId = 1;
        foreach (var p in info.GetProperty("participants").EnumerateArray())
        {
            var participant = participants.First(x => x.Puuid == p.GetProperty("puuid").GetString());
            _logger.LogTrace("Inserting participant {Puuid} for match {MatchId}", participant.Puuid, match.MatchId);
            var dbId = await participantsRepo.InsertAsync(participant);

            participantIdMap[riotParticipantId] = dbId;
            participantTeams[riotParticipantId] = participant.TeamId;
            participantRoles[riotParticipantId] = participant.Role;

            // Map and persist participant metrics (info-derived only)
            var teamTotalKills = teamKills[participant.TeamId];
            var teamTotalDamage = teamDamage[participant.TeamId];
            var metric = RiotMatchMapper.MapParticipantMetricFromInfo(p, gameDurationSec, teamTotalKills, teamTotalDamage);
            metric.ParticipantId = dbId;

            // If we have timeline, enrich with death timings
            if (timelineRoot.HasValue)
            {
                var deathTimings = RiotTimelineMapper.ExtractDeathTimings(timelineRoot.Value);
                if (deathTimings.TryGetValue(riotParticipantId, out var deathData))
                {
                    metric.DeathsPre10 = deathData.DeathsPre10;
                    metric.Deaths10To20 = deathData.Deaths10To20;
                    metric.Deaths20To30 = deathData.Deaths20To30;
                    metric.Deaths30Plus = deathData.Deaths30Plus;
                    metric.FirstDeathMinute = deathData.FirstDeathMinute;
                }
            }

            await partMetricsRepo.UpsertAsync(metric);
            riotParticipantId++;
        }

        // 3. Map and persist team objectives
        var teamObjectives = RiotMatchMapper.MapTeamObjectives(matchRoot);
        foreach (var obj in teamObjectives)
        {
            await teamObjectivesRepo.UpsertAsync(obj);
        }

        // 4. Map and persist team role responsibilities (derived from match info)
        var roleResponsibilities = RiotMatchMapper.MapTeamRoleResponsibilities(matchRoot);
        foreach (var rr in roleResponsibilities)
        {
            await teamRoleRepo.UpsertAsync(rr);
        }

        // 5. If timeline available, map and persist timeline-derived data
        if (timelineRoot.HasValue)
        {
            // Checkpoints
            var checkpoints = RiotTimelineMapper.MapCheckpoints(
                timelineRoot.Value,
                participantIdMap,
                participantTeams,
                participantRoles);
            await checkpointsRepo.UpsertBatchAsync(checkpoints);

            // Participant objective participation
            var objParticipation = RiotTimelineMapper.ExtractObjectiveParticipation(timelineRoot.Value);
            foreach (var (riotPid, data) in objParticipation)
            {
                if (!participantIdMap.TryGetValue(riotPid, out var dbPid)) continue;
                await partObjectivesRepo.UpsertAsync(new ParticipantObjective
                {
                    ParticipantId = dbPid,
                    DragonsParticipated = data.Dragons,
                    HeraldsParticipated = data.Heralds,
                    BaronsParticipated = data.Barons,
                    TowersParticipated = data.Towers,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Team match metrics (gold leads)
            var matchId = matchRoot.GetProperty("metadata").GetProperty("matchId").GetString()!;
            var teamWins = new Dictionary<int, bool>();
            foreach (var team in info.GetProperty("teams").EnumerateArray())
            {
                var teamId = team.GetProperty("teamId").GetInt32();
                teamWins[teamId] = team.GetProperty("win").GetBoolean();
            }

            var teamGoldMetrics = RiotTimelineMapper.ExtractTeamGoldMetrics(
                timelineRoot.Value,
                participantTeams,
                teamWins);

            foreach (var (teamId, metrics) in teamGoldMetrics)
            {
                await teamMetricsRepo.UpsertAsync(new TeamMatchMetric
                {
                    MatchId = matchId,
                    TeamId = teamId,
                    GoldLeadAt15 = metrics.GoldLeadAt15,
                    LargestGoldLead = metrics.LargestGoldLead,
                    GoldSwingPost20 = metrics.GoldSwingPost20,
                    WinWhenAheadAt20 = metrics.WinWhenAheadAt20,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}

