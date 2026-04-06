using System.Text.Json;
using FluentAssertions;
using Mongoose.Api.Infrastructure.Riot.Mappers;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for RiotTimelineMapper — validates all timeline extraction methods
/// used to derive solo dashboard analytics from Riot match-v5 timeline JSON.
/// </summary>
public class RiotTimelineMapperTests
{
    #region MapCheckpoints

    [Fact]
    public void MapCheckpoints_ReturnsCheckpointsAtTargetMinutes()
    {
        // Arrange — timeline with frames at 0, 5, 10, and 15 minutes
        var timeline = BuildTimeline(new[]
        {
            BuildFrame(0,    new[] { (1, 500,  0,   0,   0)  }),
            BuildFrame(5,    new[] { (1, 1500, 30,  0,   2000) }),
            BuildFrame(10,   new[] { (1, 2800, 65,  0,   4500) }),
            BuildFrame(15,   new[] { (1, 4200, 108, 0,   7000) }),
            BuildFrame(16,   new[] { (1, 4300, 112, 0,   7100) }), // should be skipped
        });
        var participantIdMap = new Dictionary<int, long> { [1] = 101 };
        var participantTeams = new Dictionary<int, int> { [1] = 100 };
        var participantRoles = new Dictionary<int, string?> { [1] = null };

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert — only 0, 5, 10, 15 frames should be captured
        checkpoints.Should().HaveCount(4);
        checkpoints.Select(c => c.MinuteMark).Should().BeEquivalentTo(new[] { 0, 5, 10, 15 });
    }

    [Fact]
    public void MapCheckpoints_MapsCorrectGoldAndCsValues()
    {
        // Arrange
        var timeline = BuildTimeline(new[]
        {
            BuildFrame(15, new[] { (1, 4200, 90, 15, 7000) }) // gold=4200, minions=90, jungle=15
        });
        var participantIdMap = new Dictionary<int, long> { [1] = 101 };
        var participantTeams = new Dictionary<int, int> { [1] = 100 };
        var participantRoles = new Dictionary<int, string?> { [1] = null };

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert
        var checkpoint = checkpoints.Single();
        checkpoint.ParticipantId.Should().Be(101);
        checkpoint.MinuteMark.Should().Be(15);
        checkpoint.Gold.Should().Be(4200);
        checkpoint.Cs.Should().Be(105); // minions + jungle
        checkpoint.Xp.Should().Be(7000);
    }

    [Fact]
    public void MapCheckpoints_CalculatesLaneGoldDiff_WhenOpponentPresent()
    {
        // Arrange — two mid laners on opposite teams
        var timeline = BuildTimeline(new[]
        {
            BuildFrame(15, new[]
            {
                (1, 4500, 100, 0, 7000), // participant 1 - team 100 - MID
                (6, 4000, 90,  0, 6500)  // participant 6 - team 200 - MID (opponent)
            })
        });
        var participantIdMap = new Dictionary<int, long> { [1] = 101, [6] = 106 };
        var participantTeams = new Dictionary<int, int> { [1] = 100, [6] = 200 };
        var participantRoles = new Dictionary<int, string?> { [1] = "MIDDLE", [6] = "MIDDLE" };

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert — participant 1 should be 500g ahead
        var p1 = checkpoints.Single(c => c.ParticipantId == 101);
        p1.GoldDiffVsLane.Should().Be(500);
        p1.CsDiffVsLane.Should().Be(10);
        p1.IsAhead.Should().BeTrue();

        // Participant 6 should be 500g behind
        var p6 = checkpoints.Single(c => c.ParticipantId == 106);
        p6.GoldDiffVsLane.Should().Be(-500);
        p6.IsAhead.Should().BeFalse();
    }

    [Fact]
    public void MapCheckpoints_HasNullDiffs_WhenNoLaneOpponent()
    {
        // Arrange — solo player with no role set
        var timeline = BuildTimeline(new[]
        {
            BuildFrame(15, new[] { (1, 4000, 80, 0, 6000) })
        });
        var participantIdMap = new Dictionary<int, long> { [1] = 101 };
        var participantTeams = new Dictionary<int, int> { [1] = 100 };
        var participantRoles = new Dictionary<int, string?> { [1] = null };

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert
        var checkpoint = checkpoints.Single();
        checkpoint.GoldDiffVsLane.Should().BeNull();
        checkpoint.CsDiffVsLane.Should().BeNull();
        checkpoint.IsAhead.Should().BeNull();
    }

    [Fact]
    public void MapCheckpoints_ReturnsEmpty_WhenNoFrames()
    {
        // Arrange
        var timeline = BuildTimeline(Array.Empty<JsonObject>());
        var participantIdMap = new Dictionary<int, long>();
        var participantTeams = new Dictionary<int, int>();
        var participantRoles = new Dictionary<int, string?>();

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert
        checkpoints.Should().BeEmpty();
    }

    [Fact]
    public void MapCheckpoints_SkipsParticipant_WhenNotInIdMap()
    {
        // Arrange — participant 2 has no db mapping
        var timeline = BuildTimeline(new[]
        {
            BuildFrame(15, new[]
            {
                (1, 4000, 80, 0, 6000),
                (2, 3500, 70, 0, 5500)  // not in participantIdMap
            })
        });
        var participantIdMap = new Dictionary<int, long> { [1] = 101 }; // only participant 1 mapped
        var participantTeams = new Dictionary<int, int> { [1] = 100, [2] = 200 };
        var participantRoles = new Dictionary<int, string?> { [1] = null, [2] = null };

        // Act
        var checkpoints = RiotTimelineMapper.MapCheckpoints(timeline, participantIdMap, participantTeams, participantRoles);

        // Assert — only participant 1's checkpoint created
        checkpoints.Should().HaveCount(1);
        checkpoints[0].ParticipantId.Should().Be(101);
    }

    #endregion

    #region ExtractDeathTimings

    [Fact]
    public void ExtractDeathTimings_ReturnsEmpty_WhenNoKillEvents()
    {
        // Arrange — frames exist but have no CHAMPION_KILL events
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(Array.Empty<KillEvent>())
        });

        // Act
        var timings = RiotTimelineMapper.ExtractDeathTimings(timeline);

        // Assert
        timings.Should().BeEmpty();
    }

    [Fact]
    public void ExtractDeathTimings_BucketsDeathsByTimeRange()
    {
        // Arrange — participant 1 dies at 5min, 15min, 25min, 35min
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(new[]
            {
                new KillEvent(victimId: 1, timestampMs: 5 * 60_000),   // pre-10
                new KillEvent(victimId: 1, timestampMs: 15 * 60_000),  // 10-20
                new KillEvent(victimId: 1, timestampMs: 25 * 60_000),  // 20-30
                new KillEvent(victimId: 1, timestampMs: 35 * 60_000),  // 30+
            })
        });

        // Act
        var timings = RiotTimelineMapper.ExtractDeathTimings(timeline);

        // Assert
        timings.Should().ContainKey(1);
        var d = timings[1];
        d.DeathsPre10.Should().Be(1);
        d.Deaths10To20.Should().Be(1);
        d.Deaths20To30.Should().Be(1);
        d.Deaths30Plus.Should().Be(1);
    }

    [Fact]
    public void ExtractDeathTimings_RecordsFirstDeathMinute()
    {
        // Arrange — participant dies at 3, then 12 minutes
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(new[]
            {
                new KillEvent(victimId: 2, timestampMs: 3 * 60_000),
                new KillEvent(victimId: 2, timestampMs: 12 * 60_000),
            })
        });

        // Act
        var timings = RiotTimelineMapper.ExtractDeathTimings(timeline);

        // Assert
        timings[2].FirstDeathMinute.Should().Be(3);
    }

    [Fact]
    public void ExtractDeathTimings_TracksMultipleParticipantsSeparately()
    {
        // Arrange
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(new[]
            {
                new KillEvent(victimId: 1, timestampMs: 5 * 60_000),
                new KillEvent(victimId: 3, timestampMs: 8 * 60_000),
            })
        });

        // Act
        var timings = RiotTimelineMapper.ExtractDeathTimings(timeline);

        // Assert
        timings.Should().ContainKey(1);
        timings.Should().ContainKey(3);
        timings[1].DeathsPre10.Should().Be(1);
        timings[3].DeathsPre10.Should().Be(1);
    }

    #endregion

    #region ExtractObjectiveParticipation

    [Fact]
    public void ExtractObjectiveParticipation_ReturnsEmpty_WhenNoObjectiveEvents()
    {
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(Array.Empty<KillEvent>())
        });

        var result = RiotTimelineMapper.ExtractObjectiveParticipation(timeline);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractObjectiveParticipation_CountsDragonKillerAndAssists()
    {
        // Arrange — participant 1 kills dragon, participant 2 and 3 assist
        var timeline = BuildTimelineWithEliteMonsterEvent("DRAGON", killerId: 1, assistIds: new[] { 2, 3 });

        // Act
        var result = RiotTimelineMapper.ExtractObjectiveParticipation(timeline);

        // Assert
        result[1].Dragons.Should().Be(1);
        result[2].Dragons.Should().Be(1);
        result[3].Dragons.Should().Be(1);
    }

    [Fact]
    public void ExtractObjectiveParticipation_CountsBaronAndHerald()
    {
        // Arrange
        var timeline = BuildTimelineWithMultipleObjectiveEvents(new[]
        {
            ("BARON_NASHOR", 1, Array.Empty<int>()),
            ("RIFTHERALD",   2, Array.Empty<int>()),
        });

        // Act
        var result = RiotTimelineMapper.ExtractObjectiveParticipation(timeline);

        // Assert
        result[1].Barons.Should().Be(1);
        result[2].Heralds.Should().Be(1);
    }

    [Fact]
    public void ExtractObjectiveParticipation_CountsTowerKills()
    {
        // Arrange
        var timeline = BuildTimelineWithTowerKillEvent(killerId: 1, assistIds: new[] { 2 });

        // Act
        var result = RiotTimelineMapper.ExtractObjectiveParticipation(timeline);

        // Assert
        result[1].Towers.Should().Be(1);
        result[2].Towers.Should().Be(1);
    }

    #endregion

    #region ExtractDeathPositions

    [Fact]
    public void ExtractDeathPositions_ReturnsEmpty_WhenNoChampionKills()
    {
        var timeline = BuildTimelineWithEvents(new[]
        {
            BuildEventFrame(Array.Empty<KillEvent>())
        });

        var result = RiotTimelineMapper.ExtractDeathPositions(timeline);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractDeathPositions_ExtractsPositionAndMinute()
    {
        // Arrange
        var timeline = BuildTimelineWithPositionedKill(victimId: 1, timestampMs: 7 * 60_000, x: 5000, y: 8000, killerId: 3, assistCount: 1);

        // Act
        var result = RiotTimelineMapper.ExtractDeathPositions(timeline);

        // Assert
        result.Should().ContainKey(1);
        var death = result[1].Single();
        death.MinuteMark.Should().Be(7);
        death.PositionX.Should().Be(5000);
        death.PositionY.Should().Be(8000);
        death.KillerParticipantId.Should().Be(3);
        death.AssistCount.Should().Be(1);
    }

    [Fact]
    public void ExtractDeathPositions_SkipsEvent_WhenPositionMissing()
    {
        // Arrange — kill event without a position field
        var json = """
        {
            "info": {
                "frames": [{
                    "timestamp": 300000,
                    "events": [{
                        "type": "CHAMPION_KILL",
                        "victimId": 1,
                        "timestamp": 300000,
                        "killerId": 2
                    }]
                }]
            }
        }
        """;
        var timeline = JsonDocument.Parse(json).RootElement;

        // Act
        var result = RiotTimelineMapper.ExtractDeathPositions(timeline);

        // Assert — no death positions extracted (position was missing)
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractDeathPositions_AccumulatesMultipleDeathsPerParticipant()
    {
        // Arrange — participant 1 dies twice
        var event1 = BuildKillEventJson(1, 300000, 1000, 2000, 2, 0);
        var event2 = BuildKillEventJson(1, 900000, 3000, 4000, 3, 0);
        var json = $$"""
        {
            "info": {
                "frames": [{
                    "timestamp": 0,
                    "events": [
                        {{event1}},
                        {{event2}}
                    ]
                }]
            }
        }
        """;
        var timeline = JsonDocument.Parse(json).RootElement;

        // Act
        var result = RiotTimelineMapper.ExtractDeathPositions(timeline);

        // Assert
        result[1].Should().HaveCount(2);
    }

    #endregion

    #region ExtractTeamGoldMetrics

    [Fact]
    public void ExtractTeamGoldMetrics_CalculatesGoldLeadAt15ForBothTeams()
    {
        // Arrange — team 100 has 1000g more at minute 15
        var timeline = BuildTeamGoldTimeline(new[]
        {
            (15, 100, 20000),  // team 100 has 20k gold
            (15, 200, 19000),  // team 200 has 19k gold
        });
        var participantTeams = new Dictionary<int, int> { [1] = 100, [6] = 200 };
        var teamWins = new Dictionary<int, bool> { [100] = true, [200] = false };

        // Act
        var result = RiotTimelineMapper.ExtractTeamGoldMetrics(timeline, participantTeams, teamWins);

        // Assert
        result[100].GoldLeadAt15.Should().Be(1000);
        result[200].GoldLeadAt15.Should().Be(-1000);
    }

    [Fact]
    public void ExtractTeamGoldMetrics_AlwaysReturnsBothTeamEntries()
    {
        // Arrange — empty timeline (no frames)
        var timeline = BuildTimeline(Array.Empty<JsonObject>());
        var participantTeams = new Dictionary<int, int>();
        var teamWins = new Dictionary<int, bool>();

        // Act
        var result = RiotTimelineMapper.ExtractTeamGoldMetrics(timeline, participantTeams, teamWins);

        // Assert — both teams always present even with no data
        result.Should().ContainKey(100);
        result.Should().ContainKey(200);
    }

    [Fact]
    public void ExtractTeamGoldMetrics_ReturnsNullGoldLead_WhenNoFrameAt15()
    {
        // Arrange — only frames at 10 and 20 (no minute 15)
        var timeline = BuildTeamGoldTimeline(new[]
        {
            (10, 100, 15000),
            (10, 200, 14000),
            (20, 100, 28000),
            (20, 200, 27000),
        });
        var participantTeams = new Dictionary<int, int> { [1] = 100, [6] = 200 };
        var teamWins = new Dictionary<int, bool> { [100] = true, [200] = false };

        // Act
        var result = RiotTimelineMapper.ExtractTeamGoldMetrics(timeline, participantTeams, teamWins);

        // Assert
        result[100].GoldLeadAt15.Should().BeNull();
        result[200].GoldLeadAt15.Should().BeNull();
    }

    #endregion

    #region Edge cases — malformed / partial payloads

    [Fact]
    public void ExtractDeathTimings_HandlesNullEventsArray_Gracefully()
    {
        // Arrange — events field is explicitly null
        var json = """
        {
            "info": {
                "frames": [{
                    "timestamp": 0,
                    "events": null
                }]
            }
        }
        """;
        var timeline = JsonDocument.Parse(json).RootElement;

        // Act
        var act = () => RiotTimelineMapper.ExtractDeathTimings(timeline);

        // Assert — no exception
        act.Should().NotThrow();
    }

    [Fact]
    public void MapCheckpoints_HandlesNullParticipantFrames_Gracefully()
    {
        // Arrange — participantFrames is null (can happen in early frames)
        var json = """
        {
            "info": {
                "frames": [{
                    "timestamp": 0,
                    "participantFrames": null
                }]
            }
        }
        """;
        var timeline = JsonDocument.Parse(json).RootElement;

        // Act
        var act = () => RiotTimelineMapper.MapCheckpoints(
            timeline,
            new Dictionary<int, long>(),
            new Dictionary<int, int>(),
            new Dictionary<int, string?>());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ExtractObjectiveParticipation_HandlesNullEventsArray_Gracefully()
    {
        var json = """
        {
            "info": {
                "frames": [{
                    "timestamp": 0,
                    "events": null
                }]
            }
        }
        """;
        var timeline = JsonDocument.Parse(json).RootElement;

        var act = () => RiotTimelineMapper.ExtractObjectiveParticipation(timeline);

        act.Should().NotThrow();
    }

    #endregion

    // ---- JSON builder helpers ----

    private record JsonObject(string Json);
    private record KillEvent(int victimId, long timestampMs);

    private static JsonElement BuildTimeline(IEnumerable<JsonObject> frameObjects)
    {
        var framesJson = string.Join(",", frameObjects.Select(f => f.Json));
        var json = $"{{\"info\":{{\"frames\":[{framesJson}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonObject BuildFrame(int minuteMark, IEnumerable<(int id, int gold, int minions, int jungle, int xp)> participants)
    {
        var timestamp = (long)minuteMark * 60_000;
        var frames = string.Join(",", participants.Select(p =>
            $"\"{p.id}\":{{\"totalGold\":{p.gold},\"minionsKilled\":{p.minions},\"jungleMinionsKilled\":{p.jungle},\"xp\":{p.xp}}}"));
        return new JsonObject($"{{\"timestamp\":{timestamp},\"participantFrames\":{{{frames}}}}}");
    }

    private static JsonElement BuildTimelineWithEvents(IEnumerable<JsonObject> frames)
        => BuildTimeline(frames);

    private static JsonObject BuildEventFrame(IEnumerable<KillEvent> kills)
    {
        var events = string.Join(",", kills.Select(k =>
            $"{{\"type\":\"CHAMPION_KILL\",\"victimId\":{k.victimId},\"timestamp\":{k.timestampMs},\"killerId\":99}}"));
        return new JsonObject($"{{\"timestamp\":0,\"events\":[{events}]}}");
    }

    private static JsonElement BuildTimelineWithEliteMonsterEvent(string monsterType, int killerId, int[] assistIds)
    {
        var assistsJson = $"[{string.Join(",", assistIds)}]";
        var eventJson = $"{{\"type\":\"ELITE_MONSTER_KILL\",\"monsterType\":\"{monsterType}\",\"killerId\":{killerId},\"assistingParticipantIds\":{assistsJson}}}";
        var frameJson = $"{{\"timestamp\":0,\"events\":[{eventJson}]}}";
        var json = $"{{\"info\":{{\"frames\":[{frameJson}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement BuildTimelineWithMultipleObjectiveEvents(IEnumerable<(string monsterType, int killerId, int[] assistIds)> events)
    {
        var eventsJson = string.Join(",", events.Select(e =>
        {
            var assists = $"[{string.Join(",", e.assistIds)}]";
            return $"{{\"type\":\"ELITE_MONSTER_KILL\",\"monsterType\":\"{e.monsterType}\",\"killerId\":{e.killerId},\"assistingParticipantIds\":{assists}}}";
        }));
        var frameJson = $"{{\"timestamp\":0,\"events\":[{eventsJson}]}}";
        var json = $"{{\"info\":{{\"frames\":[{frameJson}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement BuildTimelineWithTowerKillEvent(int killerId, int[] assistIds)
    {
        var assistsJson = $"[{string.Join(",", assistIds)}]";
        var eventJson = $"{{\"type\":\"BUILDING_KILL\",\"buildingType\":\"TOWER_BUILDING\",\"killerId\":{killerId},\"assistingParticipantIds\":{assistsJson}}}";
        var frameJson = $"{{\"timestamp\":0,\"events\":[{eventJson}]}}";
        var json = $"{{\"info\":{{\"frames\":[{frameJson}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement BuildTimelineWithPositionedKill(int victimId, long timestampMs, int x, int y, int killerId, int assistCount)
    {
        var assists = string.Join(",", Enumerable.Range(10, assistCount));
        var eventJson = $"{{\"type\":\"CHAMPION_KILL\",\"victimId\":{victimId},\"timestamp\":{timestampMs},\"killerId\":{killerId},\"position\":{{\"x\":{x},\"y\":{y}}},\"assistingParticipantIds\":[{assists}]}}";
        var frameJson = $"{{\"timestamp\":0,\"events\":[{eventJson}]}}";
        var json = $"{{\"info\":{{\"frames\":[{frameJson}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }

    private static string BuildKillEventJson(int victimId, long timestampMs, int x, int y, int killerId, int assistCount)
    {
        var assists = string.Join(",", Enumerable.Range(10, assistCount));
        return $"{{\"type\":\"CHAMPION_KILL\",\"victimId\":{victimId},\"timestamp\":{timestampMs},\"killerId\":{killerId},\"position\":{{\"x\":{x},\"y\":{y}}},\"assistingParticipantIds\":[{assists}]}}";
    }

    /// <summary>
    /// Builds a timeline where each entry is (minuteMark, teamId, goldForThatTeam).
    /// Creates one participant per team in each frame.
    /// </summary>
    private static JsonElement BuildTeamGoldTimeline(IEnumerable<(int minute, int team, int gold)> entries)
    {
        // Group by minute to create frames
        var byMinute = entries.GroupBy(e => e.minute);
        var frames = byMinute.Select(g =>
        {
            var timestamp = (long)g.Key * 60_000;
            var participantList = g.Select((e, i) =>
            {
                var pid = e.team == 100 ? 1 : 6;
                return $"\"{pid}\":{{\"totalGold\":{e.gold},\"minionsKilled\":0,\"jungleMinionsKilled\":0,\"xp\":0}}";
            });
            var participantFrames = string.Join(",", participantList);
            return $"{{\"timestamp\":{timestamp},\"participantFrames\":{{{participantFrames}}}}}";
        });

        var json = $"{{\"info\":{{\"frames\":[{string.Join(",", frames)}]}}}}";
        return JsonDocument.Parse(json).RootElement;
    }
}
