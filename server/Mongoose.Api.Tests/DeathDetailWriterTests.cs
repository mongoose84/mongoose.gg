using System.Text.Json;
using FluentAssertions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class DeathDetailWriterTests
{
    // Participant 1 dies to 7 (with 8 and 9) at 5:01 next to ally 2; participant 6 is executed at 10:30.
    // Team 100 takes an Ocean dragon, team 200 destroys a bot tower of team 100.
    private const string Timeline = """
        {
          "info": {
            "frames": [
              {
                "timestamp": 300000,
                "participantFrames": { "1": { "position": { "x": 5000, "y": 5000 } }, "2": { "position": { "x": 5500, "y": 5000 } } },
                "events": [
                  { "type": "CHAMPION_KILL", "timestamp": 301000, "victimId": 1, "killerId": 7,
                    "assistingParticipantIds": [8, 9], "position": { "x": 5000, "y": 5000 } },
                  { "type": "CHAMPION_KILL", "timestamp": 630000, "victimId": 6, "killerId": 0, "position": { "x": 9000, "y": 9000 } },
                  { "type": "ELITE_MONSTER_KILL", "timestamp": 320000, "killerId": 2, "killerTeamId": 100,
                    "monsterType": "DRAGON", "monsterSubType": "WATER_DRAGON" },
                  { "type": "BUILDING_KILL", "timestamp": 640000, "killerId": 7, "teamId": 100,
                    "buildingType": "TOWER_BUILDING", "laneType": "BOT_LANE" }
                ]
              }
            ]
          }
        }
        """;

    private sealed class RecordingDeathEvents : IParticipantDeathEventsRepository
    {
        public List<(string MatchId, IReadOnlyList<ParticipantDeathEvent> Events)> Replaced { get; } = new();
        public Task InsertAsync(ParticipantDeathEvent deathEvent) => Task.CompletedTask;
        public Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents) => Task.CompletedTask;
        public Task ReplaceForMatchAsync(string matchId, IReadOnlyList<ParticipantDeathEvent> deathEvents)
        {
            Replaced.Add((matchId, deathEvents));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingObjectiveEvents : IMatchObjectiveEventsRepository
    {
        public List<(string MatchId, IReadOnlyList<MatchObjectiveEvent> Events)> Replaced { get; } = new();
        public Task ReplaceForMatchAsync(string matchId, IReadOnlyList<MatchObjectiveEvent> events)
        {
            Replaced.Add((matchId, events));
            return Task.CompletedTask;
        }
    }

    private static readonly Dictionary<int, TimelineParticipant> Participants = Enumerable.Range(1, 10)
        .ToDictionary(id => id, id => new TimelineParticipant(ParticipantId: 1000 + id, ChampionId: 100 + id));

    private static async Task<(RecordingDeathEvents Deaths, RecordingObjectiveEvents Objectives)> WriteAsync(
        IReadOnlyDictionary<int, TimelineParticipant>? participants = null)
    {
        var deaths = new RecordingDeathEvents();
        var objectives = new RecordingObjectiveEvents();
        var writer = new DeathDetailWriter(deaths, objectives);

        await writer.WriteAsync("EUW1_1", JsonDocument.Parse(Timeline).RootElement, participants ?? Participants);
        return (deaths, objectives);
    }

    [Fact]
    public async Task WriteAsync_ReplacesTheMatchsDeaths_WithTheirDetail()
    {
        var (deaths, _) = await WriteAsync();

        var (matchId, events) = deaths.Replaced.Single();
        matchId.Should().Be("EUW1_1");
        var gank = events.Single(e => e.ParticipantId == 1001);
        gank.TimestampSec.Should().Be(301);
        gank.KillerParticipantId.Should().Be(7);
        gank.KillerChampionId.Should().Be(107);
        gank.AssistingParticipantIds.Should().Be("8,9");
        gank.AssistCount.Should().Be(2);
        gank.AlliesNearby.Should().Be(1);
    }

    [Fact]
    public async Task WriteAsync_StoresAnExecute_WithoutKiller()
    {
        var (deaths, _) = await WriteAsync();

        var execute = deaths.Replaced.Single().Events.Single(e => e.ParticipantId == 1006);
        execute.KillerParticipantId.Should().BeNull();
        execute.KillerChampionId.Should().BeNull();
        execute.AssistingParticipantIds.Should().BeEmpty();
    }

    [Fact]
    public async Task WriteAsync_SkipsDeathsOfParticipantsItDoesNotKnow()
    {
        var (deaths, _) = await WriteAsync(Participants.Where(p => p.Key != 6).ToDictionary());

        deaths.Replaced.Single().Events.Should().ContainSingle().Which.ParticipantId.Should().Be(1001);
    }

    [Fact]
    public async Task WriteAsync_ReplacesTheMatchsObjectives()
    {
        var (_, objectives) = await WriteAsync();

        var (matchId, events) = objectives.Replaced.Single();
        matchId.Should().Be("EUW1_1");
        events.Select(e => (e.Type, e.TeamId, e.Subtype, e.TimestampSec)).Should().Equal(
            ("dragon", 100, "WATER_DRAGON", 320),
            ("tower", 200, "BOT_LANE", 640));
    }
}
