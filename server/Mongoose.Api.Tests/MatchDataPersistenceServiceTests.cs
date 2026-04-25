using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Services;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for MatchDataPersistenceService remake/abandoned-game guard.
/// Verifies that short-duration and early-surrender matches are rejected before
/// any data is written, and that normal games are persisted as expected.
/// </summary>
public class MatchDataPersistenceServiceTests
{
    private readonly FakeMatchesRepoForPersistence _matchesRepo = new();
    private readonly FakeSeasonsRepoForPersistence _seasonsRepo = new();

    private MatchDataPersistenceService BuildService() => new(
        _matchesRepo,
        new StubParticipantsRepository(),
        new StubTeamObjectivesRepository(),
        new StubParticipantMetricsRepository(),
        new StubParticipantCheckpointsRepository(),
        new StubParticipantObjectivesRepository(),
        new StubParticipantDeathEventsRepository(),
        new StubTeamMatchMetricsRepository(),
        new StubTeamRoleResponsibilitiesRepository(),
        _seasonsRepo,
        NullLogger<MatchDataPersistenceService>.Instance);

    // -------------------------------------------------------------------------
    // Short duration
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PersistMatchDataAsync_ShortDuration_ReturnsEarly_WithoutPersisting()
    {
        // Arrange — game under 5 minutes
        var matchRoot = BuildMinimalMatchJson("NA1_REMAKE01", gameDuration: 240);

        // Act
        var sut = BuildService();
        await sut.PersistMatchDataAsync(matchRoot, null);

        // Assert — nothing written to the matches table
        _matchesRepo.UpsertCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(299)]
    public async Task PersistMatchDataAsync_VariousShortDurations_AllRejected(int duration)
    {
        var matchRoot = BuildMinimalMatchJson("NA1_SHORT", gameDuration: duration);
        var sut = BuildService();

        await sut.PersistMatchDataAsync(matchRoot, null);

        _matchesRepo.UpsertCallCount.Should().Be(0, $"duration {duration}s should be rejected");
    }

    // -------------------------------------------------------------------------
    // Early surrender / remake flag
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PersistMatchDataAsync_EarlySurrender_ReturnsEarly_WithoutPersisting()
    {
        // Arrange — game long enough but flagged as early surrender on one participant
        var matchRoot = BuildMinimalMatchJson("NA1_SURRENDER01", gameDuration: 350, earlySurrender: true);

        // Act
        var sut = BuildService();
        await sut.PersistMatchDataAsync(matchRoot, null);

        // Assert
        _matchesRepo.UpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task PersistMatchDataAsync_EarlySurrender_OnAnyParticipant_IsRejected()
    {
        // Arrange — only the second participant has the flag set
        var matchRoot = BuildMinimalMatchJson("NA1_SURRENDER02", gameDuration: 400, earlySurrenderOnSecondParticipant: true);

        var sut = BuildService();
        await sut.PersistMatchDataAsync(matchRoot, null);

        _matchesRepo.UpsertCallCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Normal game — must be persisted
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PersistMatchDataAsync_NormalGame_PersistsMatch()
    {
        // Arrange — valid game: >= 300 s, no early surrender
        var matchRoot = BuildFullMatchJson("NA1_NORMAL01", gameDuration: 1800);

        var sut = BuildService();
        await sut.PersistMatchDataAsync(matchRoot, null);

        // Assert — match was upserted
        _matchesRepo.UpsertCallCount.Should().Be(1);
    }

    [Fact]
    public async Task PersistMatchDataAsync_ExactlyAtBoundary_300s_PersistsMatch()
    {
        var matchRoot = BuildFullMatchJson("NA1_BOUNDARY01", gameDuration: 300);

        var sut = BuildService();
        await sut.PersistMatchDataAsync(matchRoot, null);

        _matchesRepo.UpsertCallCount.Should().Be(1);
    }

    // -------------------------------------------------------------------------
    // JSON helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the minimal JSON the guard needs (metadata.matchId + info.gameDuration +
    /// info.participants[].gameEndedInEarlySurrender). Does NOT include the full
    /// participant fields needed by downstream mappers — only for early-return paths.
    /// </summary>
    private static JsonElement BuildMinimalMatchJson(
        string matchId,
        int gameDuration,
        bool earlySurrender = false,
        bool earlySurrenderOnSecondParticipant = false)
    {
        var json = $$"""
        {
            "metadata": { "matchId": "{{matchId}}" },
            "info": {
                "gameDuration": {{gameDuration}},
                "participants": [
                    { "gameEndedInEarlySurrender": {{(earlySurrender ? "true" : "false")}} },
                    { "gameEndedInEarlySurrender": {{(earlySurrenderOnSecondParticipant ? "true" : "false")}} }
                ]
            }
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Builds a complete minimal match JSON that passes the guard AND all downstream
    /// mappers (2 participants, 2 teams, objectives). Used for "normal game" tests.
    /// </summary>
    private static JsonElement BuildFullMatchJson(string matchId, int gameDuration)
    {
        var participant = (string puuid, int teamId, string role, bool win) => $$"""
        {
            "puuid": "{{puuid}}",
            "teamId": {{teamId}},
            "teamPosition": "{{role}}",
            "lane": "{{role}}",
            "championId": 1,
            "championName": "Annie",
            "win": {{(win ? "true" : "false")}},
            "kills": 5,
            "deaths": 3,
            "assists": 2,
            "totalMinionsKilled": 150,
            "neutralMinionsKilled": 20,
            "goldEarned": 12000,
            "totalTimeSpentDead": 90,
            "totalDamageDealtToChampions": 20000,
            "totalDamageTaken": 15000,
            "damageSelfMitigated": 3000,
            "visionScore": 30,
            "gameEndedInEarlySurrender": false
        }
        """;

        var objectives = (int dragons, int heralds, int barons, int towers) => $$"""
        {
            "dragon":     { "first": false, "kills": {{dragons}} },
            "riftHerald": { "first": false, "kills": {{heralds}} },
            "baron":      { "first": false, "kills": {{barons}} },
            "tower":      { "first": false, "kills": {{towers}} }
        }
        """;

        var json = $$"""
        {
            "metadata": { "matchId": "{{matchId}}" },
            "info": {
                "gameDuration": {{gameDuration}},
                "queueId": 420,
                "gameStartTimestamp": 1700000000000,
                "gameVersion": "15.3.123456",
                "participants": [
                    {{participant("puuid-t100-a", 100, "MIDDLE", true)}},
                    {{participant("puuid-t200-a", 200, "MIDDLE", false)}}
                ],
                "teams": [
                    { "teamId": 100, "win": true,  "objectives": {{objectives(2, 1, 0, 5)}} },
                    { "teamId": 200, "win": false, "objectives": {{objectives(0, 0, 0, 2)}} }
                ]
            }
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }
}

// ---------------------------------------------------------------------------
// Minimal fake / stub repositories
// ---------------------------------------------------------------------------

internal sealed class FakeMatchesRepoForPersistence : IMatchesRepository
{
    public int UpsertCallCount { get; private set; }

    public Task UpsertAsync(Match match) { UpsertCallCount++; return Task.CompletedTask; }
    public Task<long> GetTotalMatchCountAsync() => Task.FromResult(0L);
    public Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit) => Task.FromResult<IList<Match>>(new List<Match>());
    public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null) => Task.FromResult<IList<MatchListSummaryItem>>(new List<MatchListSummaryItem>());
    public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null) => Task.FromResult<IList<MatchListSummaryItem>>(new List<MatchListSummaryItem>());
    public Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid) => Task.FromResult<MatchDetailsItem?>(null);
    public Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null) => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
    public Task<IList<MatchListItem>> GetMatchListAsync(IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null) => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
    public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter) => Task.FromResult(new Dictionary<string, RoleBaseline>());
    public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(IReadOnlyList<string> puuids, string queueFilter) => Task.FromResult(new Dictionary<string, RoleBaseline>());
    public Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId) => Task.FromResult<IList<MatchupParticipantRaw>>(new List<MatchupParticipantRaw>());
    public Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize) => Task.FromResult(0);
}

internal sealed class FakeSeasonsRepoForPersistence : ISeasonsRepository
{
    public Task UpsertAsync(Season season) => Task.CompletedTask;
    public Task<Season?> GetByCodeAsync(string seasonCode) => Task.FromResult<Season?>(new Season
    {
        SeasonCode = seasonCode,
        StartDate = new DateOnly(2025, 1, 8),
        CreatedAt = DateTime.UtcNow
    });
}

internal sealed class StubParticipantsRepository : IParticipantsRepository
{
    private long _nextId = 1;
    public Task<long> InsertAsync(Participant participant) => Task.FromResult(_nextId++);
    public Task<IList<Participant>> GetByMatchAsync(string matchId) => Task.FromResult<IList<Participant>>(new List<Participant>());
    public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank) => Task.CompletedTask;
    public Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid) => Task.FromResult<ISet<string>>(new HashSet<string>());
    public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit) => Task.FromResult<IList<Participant>>(new List<Participant>());
}

internal sealed class StubTeamObjectivesRepository : ITeamObjectivesRepository
{
    public Task UpsertAsync(TeamObjective t) => Task.CompletedTask;
    public Task<TeamObjective?> GetAsync(string matchId, int teamId) => Task.FromResult<TeamObjective?>(null);
}

internal sealed class StubParticipantMetricsRepository : IParticipantMetricsRepository
{
    public Task UpsertAsync(ParticipantMetric m) => Task.CompletedTask;
    public Task<ParticipantMetric?> GetByParticipantIdAsync(long participantId) => Task.FromResult<ParticipantMetric?>(null);
}

internal sealed class StubParticipantCheckpointsRepository : IParticipantCheckpointsRepository
{
    public Task UpsertAsync(ParticipantCheckpoint cp) => Task.CompletedTask;
    public Task UpsertBatchAsync(IEnumerable<ParticipantCheckpoint> checkpoints) => Task.CompletedTask;
    public Task<IList<ParticipantCheckpoint>> GetByParticipantIdsAsync(IEnumerable<long> participantIds) => Task.FromResult<IList<ParticipantCheckpoint>>(new List<ParticipantCheckpoint>());
    public Task<IList<ParticipantCheckpoint>> GetByParticipantAsync(long participantId) => Task.FromResult<IList<ParticipantCheckpoint>>(new List<ParticipantCheckpoint>());
}

internal sealed class StubParticipantObjectivesRepository : IParticipantObjectivesRepository
{
    public Task UpsertAsync(ParticipantObjective o) => Task.CompletedTask;
    public Task<ParticipantObjective?> GetByParticipantIdAsync(long participantId) => Task.FromResult<ParticipantObjective?>(null);
}

internal sealed class StubParticipantDeathEventsRepository : IParticipantDeathEventsRepository
{
    public Task InsertAsync(ParticipantDeathEvent deathEvent) => Task.CompletedTask;
    public Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents) => Task.CompletedTask;
}

internal sealed class StubTeamMatchMetricsRepository : ITeamMatchMetricsRepository
{
    public Task UpsertAsync(TeamMatchMetric t) => Task.CompletedTask;
    public Task<TeamMatchMetric?> GetAsync(string matchId, int teamId) => Task.FromResult<TeamMatchMetric?>(null);
}

internal sealed class StubTeamRoleResponsibilitiesRepository : ITeamRoleResponsibilitiesRepository
{
    public Task UpsertAsync(TeamRoleResponsibility r) => Task.CompletedTask;
}
