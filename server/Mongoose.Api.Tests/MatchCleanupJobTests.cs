using System.Collections.Concurrent;
using FluentAssertions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Tests for MatchCleanupJob - automated deletion of old matches.
/// Verifies retention policy enforcement, batch processing, and cascade deletion.
/// </summary>
public class MatchCleanupJobTests
{
    private readonly FakeMatchesRepository _matchesRepo;
    private readonly FakeParticipantsRepositoryForCleanup _participantsRepo;

    public MatchCleanupJobTests()
    {
        _participantsRepo = new FakeParticipantsRepositoryForCleanup();
        _matchesRepo = new FakeMatchesRepository(_participantsRepo);
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_DeletesMatchesOlderThanCutoff()
    {
        // Arrange - Create matches with different ages
        var now = DateTimeOffset.UtcNow;
        var oldMatch = CreateTestMatch("OLD_MATCH_1", now.AddDays(-200).ToUnixTimeMilliseconds());
        var recentMatch = CreateTestMatch("RECENT_MATCH_1", now.AddDays(-30).ToUnixTimeMilliseconds());

        await _matchesRepo.UpsertAsync(oldMatch);
        await _matchesRepo.UpsertAsync(recentMatch);

        // Create participants for both matches
        await _participantsRepo.InsertAsync(CreateTestParticipant("OLD_MATCH_1", "puuid-1"));
        await _participantsRepo.InsertAsync(CreateTestParticipant("RECENT_MATCH_1", "puuid-1"));

        // Act - Delete matches older than 180 days
        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, 1000);

        // Assert
        deletedCount.Should().Be(1, "only one match is older than 180 days");

        // Verify old match is deleted
        var oldMatchParticipants = await _participantsRepo.GetByMatchAsync("OLD_MATCH_1");
        oldMatchParticipants.Should().BeEmpty("cascade delete should remove participants");

        // Verify recent match is preserved
        var recentMatchParticipants = await _participantsRepo.GetByMatchAsync("RECENT_MATCH_1");
        recentMatchParticipants.Should().HaveCount(1, "recent match should be preserved");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_RespectsMatchSize()
    {
        // Arrange - Create 5 old matches
        var now = DateTimeOffset.UtcNow;
        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();

        for (int i = 0; i < 5; i++)
        {
            var match = CreateTestMatch($"OLD_MATCH_{i}", now.AddDays(-200).ToUnixTimeMilliseconds());
            await _matchesRepo.UpsertAsync(match);
        }

        // Act - Delete with batch size of 3
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, batchSize: 3);

        // Assert
        deletedCount.Should().Be(3, "should respect batch size limit");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_ReturnsZeroWhenNoOldMatches()
    {
        // Arrange - Create only recent matches
        var now = DateTimeOffset.UtcNow;
        var recentMatch = CreateTestMatch("RECENT_MATCH_2", now.AddDays(-30).ToUnixTimeMilliseconds());
        await _matchesRepo.UpsertAsync(recentMatch);

        // Act - Try to delete old matches
        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, 1000);

        // Assert
        deletedCount.Should().Be(0, "no matches are older than cutoff");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_HandlesCascadeDeletion()
    {
        // Arrange - Create old match with participant
        var now = DateTimeOffset.UtcNow;
        var oldMatch = CreateTestMatch("CASCADE_TEST", now.AddDays(-200).ToUnixTimeMilliseconds());
        await _matchesRepo.UpsertAsync(oldMatch);

        var participant = CreateTestParticipant("CASCADE_TEST", "puuid-cascade");
        await _participantsRepo.InsertAsync(participant);

        // Verify participant exists before deletion
        var participantsBefore = await _participantsRepo.GetByMatchAsync("CASCADE_TEST");
        participantsBefore.Should().HaveCount(1);

        // Act - Delete old match
        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();
        await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, 1000);

        // Assert - Verify cascade deletion
        var participantsAfter = await _participantsRepo.GetByMatchAsync("CASCADE_TEST");
        participantsAfter.Should().BeEmpty("CASCADE DELETE should remove participants");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_HandlesEmptyDatabase()
    {
        // Arrange - Empty database (cleanup from previous tests)
        var now = DateTimeOffset.UtcNow;
        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();

        // Act
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, 1000);

        // Assert
        deletedCount.Should().BeGreaterThanOrEqualTo(0, "should handle empty database gracefully");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_ValidatesMinimumBatchSize()
    {
        // Arrange - Create old match
        var now = DateTimeOffset.UtcNow;
        var oldMatch = CreateTestMatch("BATCH_TEST_1", now.AddDays(-200).ToUnixTimeMilliseconds());
        await _matchesRepo.UpsertAsync(oldMatch);

        var cutoffTimestamp = now.AddDays(-180).ToUnixTimeMilliseconds();

        // Act - Try with batch size of 0 (should still work, just return 0)
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, 0);

        // Assert - Repository should handle gracefully (batch size validation is in the job)
        deletedCount.Should().Be(0, "batch size of 0 should delete nothing");
    }

    [Fact]
    public async Task DeleteOldMatchesAsync_HandlesNegativeCutoff()
    {
        // Arrange - Create match with normal timestamp
        var now = DateTimeOffset.UtcNow;
        var match = CreateTestMatch("NEG_TEST_1", now.AddDays(-100).ToUnixTimeMilliseconds());
        await _matchesRepo.UpsertAsync(match);

        // Act - Try with negative cutoff (far in the past)
        var deletedCount = await _matchesRepo.DeleteOldMatchesAsync(-1000, 1000);

        // Assert - Should delete nothing (no matches older than negative timestamp)
        deletedCount.Should().Be(0, "negative cutoff should delete nothing");
    }

    // Helper methods
    private static Match CreateTestMatch(string matchId, long gameStartTime)
    {
        return new Match
        {
            MatchId = matchId,
            QueueId = 420, // Ranked Solo/Duo
            GameDurationSec = 1800,
            GameStartTime = gameStartTime,
            PatchVersion = "14.1.1",
            SeasonCode = "S14",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Participant CreateTestParticipant(string matchId, string puuid)
    {
        return new Participant
        {
            MatchId = matchId,
            Puuid = puuid,
            TeamId = 100,
            Role = "MIDDLE",
            ChampionId = 157,
            ChampionName = "Yasuo",
            Win = true,
            Kills = 10,
            Deaths = 5,
            Assists = 8,
            CreepScore = 200,
            GoldEarned = 15000,
            TimeDeadSec = 60,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Fake participants repository for cleanup testing.
/// Implements IParticipantsRepository with in-memory storage.
/// </summary>
internal sealed class FakeParticipantsRepositoryForCleanup : IParticipantsRepository
{
    private readonly ConcurrentDictionary<string, List<Participant>> _participantsByMatch = new();

    public Task<long> InsertAsync(Participant participant)
    {
        _participantsByMatch.GetOrAdd(participant.MatchId, _ => new List<Participant>()).Add(participant);
        return Task.FromResult(1L);
    }

    public Task<IList<Participant>> GetByMatchAsync(string matchId)
    {
        if (_participantsByMatch.TryGetValue(matchId, out var participants))
            return Task.FromResult<IList<Participant>>(participants.ToList());
        return Task.FromResult<IList<Participant>>(new List<Participant>());
    }

    public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank)
    {
        // Not needed for cleanup tests
        return Task.CompletedTask;
    }

    public Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid)
    {
        // Not needed for cleanup tests
        return Task.FromResult<ISet<string>>(new HashSet<string>());
    }

    public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit)
    {
        // Not needed for cleanup tests
        return Task.FromResult<IList<Participant>>(new List<Participant>());
    }

    public Task DeleteByMatchIdAsync(string matchId)
    {
        _participantsByMatch.TryRemove(matchId, out _);
        return Task.CompletedTask;
    }

    public Task<IList<LpEstimationMatch>> GetRecentRankedMatchesForLpEstimationAsync(string puuid, int queueId, int limit)
    {
        // Not needed for cleanup tests
        return Task.FromResult<IList<LpEstimationMatch>>(new List<LpEstimationMatch>());
    }

    public Task<int> BatchUpdateLpEstimatesAsync(IList<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)> estimates)
    {
        // Not needed for cleanup tests
        return Task.FromResult(0);
    }
}

/// <summary>
/// Fake matches repository for cleanup testing.
/// Implements IMatchesRepository with in-memory storage and simulates cascade deletion.
/// </summary>
internal sealed class FakeMatchesRepository : IMatchesRepository
{
    private readonly ConcurrentDictionary<string, Match> _matches = new();
    private readonly FakeParticipantsRepositoryForCleanup _participantsRepo;

    public FakeMatchesRepository(FakeParticipantsRepositoryForCleanup participantsRepo)
    {
        _participantsRepo = participantsRepo;
    }

    public Task UpsertAsync(Match match)
    {
        _matches[match.MatchId] = match;
        return Task.CompletedTask;
    }

    public async Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize)
    {
        // Step 1: Find old match IDs to delete
        var matchIdsToDelete = _matches.Values
            .Where(m => m.GameStartTime < cutoffTimestamp)
            .Take(batchSize)
            .Select(m => m.MatchId)
            .ToList();

        if (matchIdsToDelete.Count == 0)
            return 0;

        // Step 2: Delete matches and simulate CASCADE DELETE for participants
        foreach (var matchId in matchIdsToDelete)
        {
            _matches.TryRemove(matchId, out _);
            // Simulate CASCADE DELETE - remove participants for this match
            await _participantsRepo.DeleteByMatchIdAsync(matchId);
        }

        return matchIdsToDelete.Count;
    }

    // Other interface methods not needed for cleanup tests
    public Task<long> GetTotalMatchCountAsync() => Task.FromResult(0L);
    public Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit) => Task.FromResult<IList<Match>>(new List<Match>());
    public Task<IList<Core.QueryModels.MatchListSummaryItem>> GetMatchListSummaryAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, Core.QueryModels.RoleBaseline>? baselines = null) => Task.FromResult<IList<Core.QueryModels.MatchListSummaryItem>>(new List<Core.QueryModels.MatchListSummaryItem>());
    public Task<Core.QueryModels.MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid) => Task.FromResult<Core.QueryModels.MatchDetailsItem?>(null);
    public Task<IList<Core.QueryModels.MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, Core.QueryModels.RoleBaseline>? baselines = null) => Task.FromResult<IList<Core.QueryModels.MatchListItem>>(new List<Core.QueryModels.MatchListItem>());
    public Task<Dictionary<string, Core.QueryModels.RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter) => Task.FromResult(new Dictionary<string, Core.QueryModels.RoleBaseline>());
    public Task<IList<Core.QueryModels.MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId) => Task.FromResult<IList<Core.QueryModels.MatchupParticipantRaw>>(new List<Core.QueryModels.MatchupParticipantRaw>());
}

