using System.Collections.Concurrent;
using FluentAssertions;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;
using Xunit;

namespace RiotProxy.Tests;

/// <summary>
/// Unit tests for MatchHistorySyncJob repository operations.
/// Tests the core sync logic using in-memory fake repositories.
/// </summary>
public class MatchHistorySyncJobTests
{
    private readonly FakeRiotAccountsRepository _riotAccountsRepo;
    private readonly FakeParticipantsRepository _participantsRepo;

    public MatchHistorySyncJobTests()
    {
        _riotAccountsRepo = new FakeRiotAccountsRepository();
        _participantsRepo = new FakeParticipantsRepository();
    }

    [Fact]
    public async Task ClaimNextPending_ReturnsNull_WhenNoPendingAccounts()
    {
        // Arrange - no accounts added
        
        // Act
        var result = await _riotAccountsRepo.ClaimNextPendingForSyncAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextPending_ReturnsPendingAccount_AndSetsToSyncing()
    {
        // Arrange
        var account = CreateTestAccount("puuid-1", "TestPlayer", "NA1", "pending");
        await _riotAccountsRepo.UpsertAsync(account);

        // Act
        var claimed = await _riotAccountsRepo.ClaimNextPendingForSyncAsync();

        // Assert
        claimed.Should().NotBeNull();
        claimed!.Puuid.Should().Be("puuid-1");
        claimed.SyncStatus.Should().Be("syncing");
    }

    [Fact]
    public async Task ClaimNextPending_SkipsNonPendingAccounts()
    {
        // Arrange
        var syncingAccount = CreateTestAccount("puuid-1", "Syncing", "NA1", "syncing");
        var completedAccount = CreateTestAccount("puuid-2", "Completed", "NA1", "completed");
        var failedAccount = CreateTestAccount("puuid-3", "Failed", "NA1", "failed");
        
        await _riotAccountsRepo.UpsertAsync(syncingAccount);
        await _riotAccountsRepo.UpsertAsync(completedAccount);
        await _riotAccountsRepo.UpsertAsync(failedAccount);

        // Act
        var claimed = await _riotAccountsRepo.ClaimNextPendingForSyncAsync();

        // Assert
        claimed.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSyncStatus_UpdatesStatusCorrectly()
    {
        // Arrange
        var account = CreateTestAccount("puuid-1", "Player", "NA1", "pending");
        await _riotAccountsRepo.UpsertAsync(account);

        // Act
        await _riotAccountsRepo.UpdateSyncStatusAsync("puuid-1", "completed", DateTime.UtcNow);
        var updated = await _riotAccountsRepo.GetByPuuidAsync("puuid-1");

        // Assert
        updated.Should().NotBeNull();
        updated!.SyncStatus.Should().Be("completed");
        updated.LastSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSyncProgress_TracksProgressCorrectly()
    {
        // Arrange
        var account = CreateTestAccount("puuid-1", "Player", "NA1", "syncing");
        await _riotAccountsRepo.UpsertAsync(account);

        // Act
        await _riotAccountsRepo.UpdateSyncProgressAsync("puuid-1", 5, 10);
        var updated = await _riotAccountsRepo.GetByPuuidAsync("puuid-1");

        // Assert
        updated.Should().NotBeNull();
        updated!.SyncProgress.Should().Be(5);
        updated.SyncTotal.Should().Be(10);
    }

    [Fact]
    public async Task ResetStuckSyncingAccounts_ResetsStaleSyncingJobs()
    {
        // Arrange - create an account that's been "syncing" for too long
        var stuckAccount = CreateTestAccount("puuid-1", "Stuck", "NA1", "syncing");
        stuckAccount.UpdatedAt = DateTime.UtcNow.AddMinutes(-15); // 15 min ago
        await _riotAccountsRepo.UpsertAsync(stuckAccount);

        // Act
        await _riotAccountsRepo.ResetStuckSyncingAccountsAsync(TimeSpan.FromMinutes(10));
        var updated = await _riotAccountsRepo.GetByPuuidAsync("puuid-1");

        // Assert
        updated.Should().NotBeNull();
        updated!.SyncStatus.Should().Be("pending");
    }

    [Fact]
    public async Task ResetStuckSyncingAccounts_DoesNotResetRecentSyncingJobs()
    {
        // Arrange - create an account that's been "syncing" for a short time
        var recentAccount = CreateTestAccount("puuid-1", "Recent", "NA1", "syncing");
        recentAccount.UpdatedAt = DateTime.UtcNow.AddMinutes(-5); // Only 5 min ago
        await _riotAccountsRepo.UpsertAsync(recentAccount);

        // Act
        await _riotAccountsRepo.ResetStuckSyncingAccountsAsync(TimeSpan.FromMinutes(10));
        var updated = await _riotAccountsRepo.GetByPuuidAsync("puuid-1");

        // Assert - should still be syncing
        updated.Should().NotBeNull();
        updated!.SyncStatus.Should().Be("syncing");
    }

    [Fact]
    public async Task GetMatchIdsForPuuid_ReturnsEmptySet_WhenNoMatches()
    {
        // Act
        var matchIds = await _participantsRepo.GetMatchIdsForPuuidAsync("unknown-puuid");

        // Assert
        matchIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchIdsForPuuid_ReturnsStoredMatchIds()
    {
        // Arrange
        _participantsRepo.AddMatchForPuuid("puuid-1", "NA1_12345");
        _participantsRepo.AddMatchForPuuid("puuid-1", "NA1_12346");
        _participantsRepo.AddMatchForPuuid("puuid-1", "NA1_12347");

        // Act
        var matchIds = await _participantsRepo.GetMatchIdsForPuuidAsync("puuid-1");

        // Assert
        matchIds.Should().HaveCount(3);
        matchIds.Should().Contain("NA1_12345");
        matchIds.Should().Contain("NA1_12346");
        matchIds.Should().Contain("NA1_12347");
    }

    [Fact]
    public async Task ClaimNextPending_ClaimsOldestFirst()
    {
        // Arrange - add accounts with different updated times
        var oldAccount = CreateTestAccount("puuid-old", "Old", "NA1", "pending");
        oldAccount.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);

        var newAccount = CreateTestAccount("puuid-new", "New", "NA1", "pending");
        newAccount.UpdatedAt = DateTime.UtcNow;

        await _riotAccountsRepo.UpsertAsync(newAccount);
        await _riotAccountsRepo.UpsertAsync(oldAccount);

        // Act
        var claimed = await _riotAccountsRepo.ClaimNextPendingForSyncAsync();

        // Assert - should claim the older one first
        claimed.Should().NotBeNull();
        claimed!.Puuid.Should().Be("puuid-old");
    }

    private static RiotAccount CreateTestAccount(string puuid, string gameName, string tagLine, string syncStatus)
    {
        return new RiotAccount
        {
            Puuid = puuid,
            UserId = 1,
            GameName = gameName,
            TagLine = tagLine,
            SummonerName = $"{gameName}#{tagLine}",
            Region = "na1",
            IsPrimary = false,
            SyncStatus = syncStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// In-memory fake implementation of V2RiotAccountsRepository for testing.
/// </summary>
internal sealed class FakeRiotAccountsRepository : RiotAccountsRepository
{
    private readonly ConcurrentDictionary<string, RiotAccount> _accounts = new();

    public FakeRiotAccountsRepository() : base(null!) { }

    public override Task UpsertAsync(RiotAccount account)
    {
        _accounts[account.Puuid] = account;
        return Task.CompletedTask;
    }

    public override Task<RiotAccount?> GetByPuuidAsync(string puuid)
    {
        _accounts.TryGetValue(puuid, out var account);
        return Task.FromResult(account);
    }

    public override Task<IList<RiotAccount>> GetByUserIdAsync(long userId)
    {
        var result = _accounts.Values.Where(a => a.UserId == userId).ToList();
        return Task.FromResult<IList<RiotAccount>>(result);
    }

    public override Task<RiotAccount?> ClaimNextPendingForSyncAsync()
    {
        var pending = _accounts.Values
            .Where(a => a.SyncStatus == "pending")
            .OrderBy(a => a.UpdatedAt)
            .FirstOrDefault();

        if (pending == null)
            return Task.FromResult<RiotAccount?>(null);

        pending.SyncStatus = "syncing";
        pending.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult<RiotAccount?>(pending);
    }

    public override Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
    {
        if (_accounts.TryGetValue(puuid, out var account))
        {
            account.SyncStatus = syncStatus;
            account.UpdatedAt = DateTime.UtcNow;
            if (lastSyncAt.HasValue)
                account.LastSyncAt = lastSyncAt;
        }
        return Task.CompletedTask;
    }

    public override Task UpdateSyncProgressAsync(string puuid, int progress, int total)
    {
        if (_accounts.TryGetValue(puuid, out var account))
        {
            account.SyncProgress = progress;
            account.SyncTotal = total;
            account.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public override Task ResetStuckSyncingAccountsAsync(TimeSpan threshold)
    {
        var cutoff = DateTime.UtcNow - threshold;
        var stuckAccounts = _accounts.Values
            .Where(a => a.SyncStatus == "syncing" && a.UpdatedAt < cutoff);

        foreach (var account in stuckAccounts)
        {
            account.SyncStatus = "pending";
            account.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory fake implementation of V2ParticipantsRepository for testing.
/// </summary>
internal sealed class FakeParticipantsRepository : ParticipantsRepository
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _matchIdsByPuuid = new();

    public FakeParticipantsRepository() : base(null!) { }

    public override Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid)
    {
        if (_matchIdsByPuuid.TryGetValue(puuid, out var matchIds))
            return Task.FromResult<ISet<string>>(matchIds);
        return Task.FromResult<ISet<string>>(new HashSet<string>());
    }

    public void AddMatchForPuuid(string puuid, string matchId)
    {
        _matchIdsByPuuid.GetOrAdd(puuid, _ => new HashSet<string>()).Add(matchId);
    }
}

