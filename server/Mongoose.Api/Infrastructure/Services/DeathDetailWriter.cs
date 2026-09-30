using System.Text.Json;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot.Mappers;

namespace Mongoose.Api.Infrastructure.Services;

/// <summary>A match's participant row as the timeline sees it: Riot participantId (1-10) to row ID and champion.</summary>
public sealed record TimelineParticipant(long ParticipantId, int ChampionId);

public interface IDeathDetailWriter
{
    /// <summary>
    /// Writes the match's deaths (with time, killer, assisters and allies nearby) and the objectives
    /// each team took, replacing what the match had, so sync and the backfill can both call it.
    /// </summary>
    Task WriteAsync(string matchId, JsonElement timelineRoot, IReadOnlyDictionary<int, TimelineParticipant> participants);
}

/// <summary>
/// Turns a match-v5 timeline into participant_death_events and match_objective_events rows
/// (features/solo-trends.spec.md, 5e).
/// </summary>
public sealed class DeathDetailWriter : IDeathDetailWriter
{
    private readonly IParticipantDeathEventsRepository _deathEventsRepo;
    private readonly IMatchObjectiveEventsRepository _objectiveEventsRepo;

    public DeathDetailWriter(IParticipantDeathEventsRepository deathEventsRepo, IMatchObjectiveEventsRepository objectiveEventsRepo)
    {
        _deathEventsRepo = deathEventsRepo;
        _objectiveEventsRepo = objectiveEventsRepo;
    }

    public async Task WriteAsync(string matchId, JsonElement timelineRoot, IReadOnlyDictionary<int, TimelineParticipant> participants)
    {
        var now = DateTime.UtcNow;

        var deaths = new List<ParticipantDeathEvent>();
        foreach (var (victimId, positions) in RiotTimelineMapper.ExtractDeathPositions(timelineRoot))
        {
            if (!participants.TryGetValue(victimId, out var victim)) continue;

            foreach (var death in positions)
            {
                int? killerChampionId = death.KillerParticipantId is { } killerId && participants.TryGetValue(killerId, out var killer)
                    ? killer.ChampionId
                    : null;

                deaths.Add(new ParticipantDeathEvent
                {
                    ParticipantId = victim.ParticipantId,
                    MinuteMark = death.MinuteMark,
                    TimestampSec = death.TimestampSec,
                    PositionX = death.PositionX,
                    PositionY = death.PositionY,
                    KillerChampionId = killerChampionId,
                    KillerParticipantId = death.KillerParticipantId,
                    AssistingParticipantIds = string.Join(",", death.AssistingParticipantIds),
                    AlliesNearby = death.AlliesNearby,
                    AssistCount = death.AssistCount,
                    CreatedAt = now
                });
            }
        }

        var objectives = RiotTimelineMapper.ExtractObjectiveEvents(timelineRoot)
            .Select(o => new MatchObjectiveEvent
            {
                MatchId = matchId,
                TeamId = o.TeamId,
                Type = o.Type,
                Subtype = o.Subtype,
                TimestampSec = o.TimestampSec,
                KillerParticipantId = o.KillerParticipantId,
                CreatedAt = now
            })
            .ToList();

        await _deathEventsRepo.ReplaceForMatchAsync(matchId, deaths);
        await _objectiveEventsRepo.ReplaceForMatchAsync(matchId, objectives);
    }
}
