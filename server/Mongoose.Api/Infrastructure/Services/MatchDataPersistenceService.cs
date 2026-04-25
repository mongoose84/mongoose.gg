using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot;
using Mongoose.Api.Infrastructure.Riot.Mappers;

namespace Mongoose.Api.Infrastructure.Services;

/// <summary>
/// Persists Riot match data (match, participants, metrics, timeline-derived data) to the database.
/// Extracted from MatchHistorySyncJob to allow independent testing and reuse.
/// </summary>
public class MatchDataPersistenceService : IMatchDataPersistenceService
{
    private readonly IMatchesRepository _matchesRepo;
    private readonly IParticipantsRepository _participantsRepo;
    private readonly ITeamObjectivesRepository _teamObjectivesRepo;
    private readonly IParticipantMetricsRepository _partMetricsRepo;
    private readonly IParticipantCheckpointsRepository _checkpointsRepo;
    private readonly IParticipantObjectivesRepository _partObjectivesRepo;
    private readonly IParticipantDeathEventsRepository _deathEventsRepo;
    private readonly ITeamMatchMetricsRepository _teamMetricsRepo;
    private readonly ITeamRoleResponsibilitiesRepository _teamRoleRepo;
    private readonly ISeasonsRepository _seasonsRepo;
    private readonly ILogger<MatchDataPersistenceService> _logger;

    public MatchDataPersistenceService(
        IMatchesRepository matchesRepo,
        IParticipantsRepository participantsRepo,
        ITeamObjectivesRepository teamObjectivesRepo,
        IParticipantMetricsRepository partMetricsRepo,
        IParticipantCheckpointsRepository checkpointsRepo,
        IParticipantObjectivesRepository partObjectivesRepo,
        IParticipantDeathEventsRepository deathEventsRepo,
        ITeamMatchMetricsRepository teamMetricsRepo,
        ITeamRoleResponsibilitiesRepository teamRoleRepo,
        ISeasonsRepository seasonsRepo,
        ILogger<MatchDataPersistenceService> logger)
    {
        _matchesRepo = matchesRepo;
        _participantsRepo = participantsRepo;
        _teamObjectivesRepo = teamObjectivesRepo;
        _partMetricsRepo = partMetricsRepo;
        _checkpointsRepo = checkpointsRepo;
        _partObjectivesRepo = partObjectivesRepo;
        _deathEventsRepo = deathEventsRepo;
        _teamMetricsRepo = teamMetricsRepo;
        _teamRoleRepo = teamRoleRepo;
        _seasonsRepo = seasonsRepo;
        _logger = logger;
    }

    public async Task PersistMatchDataAsync(JsonElement matchRoot, JsonElement? timelineRoot)
    {
        // Guard: reject remakes and abandoned games before writing anything to the database
        var info = matchRoot.GetProperty("info");
        var matchIdForLog = matchRoot.GetProperty("metadata").GetProperty("matchId").GetString();

        var gameDuration = info.GetProperty("gameDuration").GetInt32();
        if (gameDuration < 300)
        {
            _logger.LogDebug("Skipping match {MatchId}: short_duration ({Duration}s)",
                LogSanitizer.Sanitize(matchIdForLog), gameDuration);
            return;
        }

        var hasEarlySurrender = info.GetProperty("participants").EnumerateArray()
            .Any(p => p.TryGetProperty("gameEndedInEarlySurrender", out var flag) && flag.GetBoolean());
        if (hasEarlySurrender)
        {
            _logger.LogDebug("Skipping match {MatchId}: early_surrender",
                LogSanitizer.Sanitize(matchIdForLog));
            return;
        }

        // 1. Map and persist match
        var match = RiotMatchMapper.MapMatch(matchRoot);

        // Calculate and ensure season exists, then set on match
        match.SeasonCode = await SeasonHelper.EnsureSeasonExistsAsync(
            _seasonsRepo,
            match.PatchVersion,
            match.GameStartTime);

        await _matchesRepo.UpsertAsync(match);

        // 2. Map and persist participants
        var participants = RiotMatchMapper.MapParticipants(matchRoot);
        var participantIdMap = new Dictionary<int, long>(); // Riot participantId (1-10) -> DB id
        var participantTeams = new Dictionary<int, int>();
        var participantRoles = new Dictionary<int, string?>();
        var participantChampions = new Dictionary<int, int>(); // Riot participantId (1-10) -> championId

        var gameDurationSec = info.GetProperty("gameDuration").GetInt32();
        var deathTimings = timelineRoot.HasValue
            ? RiotTimelineMapper.ExtractDeathTimings(timelineRoot.Value)
            : null;

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
        _logger.LogDebug("Inserting {Count} participants for match {MatchId}", participants.Count, LogSanitizer.Sanitize(match.MatchId));
        int riotParticipantId = 1;
        foreach (var p in info.GetProperty("participants").EnumerateArray())
        {
            var participant = participants.First(x => x.Puuid == p.GetProperty("puuid").GetString());
            _logger.LogTrace("Inserting participant {Puuid} for match {MatchId}", LogSanitizer.HashForLog(participant.Puuid), LogSanitizer.Sanitize(match.MatchId));
            var dbId = await _participantsRepo.InsertAsync(participant);

            participantIdMap[riotParticipantId] = dbId;
            participantTeams[riotParticipantId] = participant.TeamId;
            participantRoles[riotParticipantId] = participant.Role;
            participantChampions[riotParticipantId] = participant.ChampionId;

            // Map and persist participant metrics (info-derived only)
            var teamTotalKills = teamKills[participant.TeamId];
            var teamTotalDamage = teamDamage[participant.TeamId];
            var metric = RiotMatchMapper.MapParticipantMetricFromInfo(p, gameDurationSec, teamTotalKills, teamTotalDamage);
            metric.ParticipantId = dbId;

            // If we have timeline, enrich with death timings
            if (deathTimings != null)
            {
                if (deathTimings.TryGetValue(riotParticipantId, out var deathData))
                {
                    metric.DeathsPre10 = deathData.DeathsPre10;
                    metric.Deaths10To20 = deathData.Deaths10To20;
                    metric.Deaths20To30 = deathData.Deaths20To30;
                    metric.Deaths30Plus = deathData.Deaths30Plus;
                    metric.FirstDeathMinute = deathData.FirstDeathMinute;
                }
            }

            await _partMetricsRepo.UpsertAsync(metric);
            riotParticipantId++;
        }

        // 3. Map and persist team objectives
        var teamObjectives = RiotMatchMapper.MapTeamObjectives(matchRoot);
        foreach (var obj in teamObjectives)
        {
            await _teamObjectivesRepo.UpsertAsync(obj);
        }

        // 4. Map and persist team role responsibilities (derived from match info)
        var roleResponsibilities = RiotMatchMapper.MapTeamRoleResponsibilities(matchRoot);
        foreach (var rr in roleResponsibilities)
        {
            await _teamRoleRepo.UpsertAsync(rr);
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
            await _checkpointsRepo.UpsertBatchAsync(checkpoints);

            // Participant objective participation
            var objParticipation = RiotTimelineMapper.ExtractObjectiveParticipation(timelineRoot.Value);
            foreach (var (riotPid, data) in objParticipation)
            {
                if (!participantIdMap.TryGetValue(riotPid, out var dbPid)) continue;
                await _partObjectivesRepo.UpsertAsync(new ParticipantObjective
                {
                    ParticipantId = dbPid,
                    DragonsParticipated = data.Dragons,
                    HeraldsParticipated = data.Heralds,
                    BaronsParticipated = data.Barons,
                    TowersParticipated = data.Towers,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Death position events (for danger zone heatmap)
            var deathPositions = RiotTimelineMapper.ExtractDeathPositions(timelineRoot.Value);
            foreach (var (riotPid, positions) in deathPositions)
            {
                if (!participantIdMap.TryGetValue(riotPid, out var dbPid)) continue;

                var deathEvents = new List<ParticipantDeathEvent>();
                foreach (var pos in positions)
                {
                    // Resolve killer championId from killer participantId
                    int? killerChampionId = null;
                    if (pos.KillerParticipantId.HasValue &&
                        participantChampions.TryGetValue(pos.KillerParticipantId.Value, out var killerChampId))
                    {
                        killerChampionId = killerChampId;
                    }

                    deathEvents.Add(new ParticipantDeathEvent
                    {
                        ParticipantId = dbPid,
                        MinuteMark = pos.MinuteMark,
                        PositionX = pos.PositionX,
                        PositionY = pos.PositionY,
                        KillerChampionId = killerChampionId,
                        AssistCount = pos.AssistCount,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (deathEvents.Count > 0)
                {
                    await _deathEventsRepo.InsertBatchAsync(deathEvents);
                }
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
                await _teamMetricsRepo.UpsertAsync(new TeamMatchMetric
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
