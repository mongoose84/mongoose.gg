using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot;
using Mongoose.Api.Infrastructure.WebSocket;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for LoginSyncService — verifies login-time Riot profile refresh and conditional sync triggering.
/// </summary>
public class LoginSyncServiceTests
{
    private readonly TrackingRiotAccountsRepository _riotAccountsRepo;
    private readonly TrackingUserRiotAccountsRepository _userRiotAccountsRepo;
    private readonly ControllableRiotApiClient _riotApiClient;
    private readonly TrackingSyncBroadcaster _broadcaster;
    private readonly TrackingAggregator _aggregator;
    private readonly TrackingQueueSignal _queueSignal;
    // Records the relative order of key calls across the fakes so tests can assert sequencing.
    private readonly List<string> _callLog = new();
    private readonly LoginSyncService _sut;

    public LoginSyncServiceTests()
    {
        _riotAccountsRepo = new TrackingRiotAccountsRepository { CallLog = _callLog };
        _riotApiClient = new ControllableRiotApiClient();
        _broadcaster = new TrackingSyncBroadcaster();
        _aggregator = new TrackingAggregator { CallLog = _callLog };
        _queueSignal = new TrackingQueueSignal();
        _userRiotAccountsRepo = new TrackingUserRiotAccountsRepository(_riotAccountsRepo);

        _sut = new LoginSyncService(
            _riotAccountsRepo,
            _userRiotAccountsRepo,
            _riotApiClient,
            _broadcaster,
            _aggregator,
            _queueSignal,
            NullLogger<LoginSyncService>.Instance);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_DoesNothing_WhenNoLinkedAccounts()
    {
        // Arrange — user 1 has no linked Riot accounts

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotApiClient.SummonerCallCount.Should().Be(0);
        _riotAccountsRepo.UpdateProfileCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_UpdatesProfile_WhenProfileIconChanges()
    {
        // Arrange
        var account = CreateAccount("puuid-1", profileIconId: 10, summonerLevel: 100);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", profileIconId: 99, summonerLevel: 100); // icon changed
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotAccountsRepo.UpdateProfileCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_UpdatesProfile_WhenSummonerLevelChanges()
    {
        // Arrange
        var account = CreateAccount("puuid-1", profileIconId: 10, summonerLevel: 100);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", profileIconId: 10, summonerLevel: 150); // level changed
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotAccountsRepo.UpdateProfileCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_UpdatesRankData_WhenRankChanges()
    {
        // Arrange
        var account = CreateAccount("puuid-1", soloTier: "GOLD", soloRank: "I", soloLp: 50);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", account.ProfileIconId ?? 1, account.SummonerLevel ?? 100);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1",
            """[{"queueType":"RANKED_SOLO_5x5","tier":"PLATINUM","rank":"IV","leaguePoints":75}]""");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert — rank changed from GOLD I 50lp to PLATINUM IV 75lp
        _riotAccountsRepo.UpdateRankCallCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_SkipsMatchSync_WhenLastSyncAtIsWithinCooldown()
    {
        // Arrange — last sync was 2 minutes ago, cooldown is 5 minutes
        var account = CreateAccount("puuid-1");
        account.LastSyncAt = DateTime.UtcNow.AddMinutes(-2);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert — match history is never checked
        _riotApiClient.MatchHistoryCallCount.Should().Be(0);
        _riotAccountsRepo.UpdateSyncStatusCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_SkipsMatchSync_WhenAccountStatusIsPending()
    {
        // Arrange — account is already queued for sync
        var account = CreateAccount("puuid-1", syncStatus: "pending");
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotApiClient.MatchHistoryCallCount.Should().Be(0);
        _riotAccountsRepo.UpdateSyncStatusCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_SkipsMatchSync_WhenAccountStatusIsSyncing()
    {
        // Arrange — account is currently syncing
        var account = CreateAccount("puuid-1", syncStatus: "syncing");
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotApiClient.MatchHistoryCallCount.Should().Be(0);
        _riotAccountsRepo.UpdateSyncStatusCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_SetsPendingAndBroadcasts_WhenNewMatchesFound()
    {
        // Arrange — account has new matches since last sync
        var account = CreateAccount("puuid-1");
        account.LastSyncAt = DateTime.UtcNow.AddHours(-2); // outside 5-min cooldown
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");
        _riotApiClient.SetMatchHistoryResponse("puuid-1", """["EUW1_123456789"]""");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotAccountsRepo.UpdateSyncStatusCallCount.Should().Be(1);
        _riotAccountsRepo.LastSyncStatusSet.Should().Be("pending");
        _broadcaster.BroadcastProgressCallCount.Should().Be(1);
        // The Overview aggregate run is opened for the queued account, and the job is woken.
        _aggregator.StartRunCalls.Should().ContainSingle()
            .Which.Should().BeEquivalentTo((UserId: 1L, Puuids: new[] { "puuid-1" }));
        _queueSignal.NotifyCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_SeedsAggregateRun_BeforeMarkingAccountsPending()
    {
        // Arrange — account has new matches since last sync
        var account = CreateAccount("puuid-1");
        account.LastSyncAt = DateTime.UtcNow.AddHours(-2);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");
        _riotApiClient.SetMatchHistoryResponse("puuid-1", """["EUW1_123456789"]""");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert — the aggregate run must be seeded BEFORE the account is marked 'pending'.
        // Otherwise the background job could claim and complete the account in the gap before
        // its slot exists, leaving an orphaned 'pending' slot that never settles.
        _callLog.Should().ContainInOrder("StartRun", "UpdateSyncStatus:pending");
    }

    [Fact]
    public async Task CheckAccountsOnLogin_DoesNotTriggerSync_WhenNoNewMatchesFound()
    {
        // Arrange — account has no new matches
        var account = CreateAccount("puuid-1");
        account.LastSyncAt = DateTime.UtcNow.AddHours(-2);
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-1", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-1", "[]");
        _riotApiClient.SetMatchHistoryResponse("puuid-1", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert
        _riotAccountsRepo.UpdateSyncStatusCallCount.Should().Be(0);
        _broadcaster.BroadcastProgressCallCount.Should().Be(0);
        _aggregator.StartRunCalls.Should().BeEmpty();
        _queueSignal.NotifyCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAccountsOnLogin_DoesNotThrow_WhenRiotApiFails()
    {
        // Arrange — Riot API will throw during profile fetch
        var account = CreateAccount("puuid-1");
        _riotAccountsRepo.AddAccount(account);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _riotApiClient.SetSummonerThrows("puuid-1");

        // Act
        var act = () => _sut.CheckAccountsOnLoginAsync(1);

        // Assert — login must not fail even when Riot API is unavailable
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckAccountsOnLogin_ContinuesProcessing_WhenOneAccountFails()
    {
        // Arrange — first account throws, second succeeds
        var account1 = CreateAccount("puuid-1");
        var account2 = CreateAccount("puuid-2");
        _riotAccountsRepo.AddAccount(account1);
        _riotAccountsRepo.AddAccount(account2);
        _userRiotAccountsRepo.Link(1, "puuid-1");
        _userRiotAccountsRepo.Link(1, "puuid-2");
        _riotApiClient.SetSummonerThrows("puuid-1");
        _riotApiClient.SetSummonerResponse("puuid-2", 1, 50);
        _riotApiClient.SetLeagueEntriesResponse("puuid-2", "[]");

        // Act
        await _sut.CheckAccountsOnLoginAsync(1);

        // Assert — both accounts were attempted despite first failure
        _riotApiClient.SummonerCallCount.Should().Be(2);
    }

    // ---- helpers ----

    private static RiotAccount CreateAccount(
        string puuid,
        int? profileIconId = 1,
        int? summonerLevel = 50,
        string syncStatus = "synced",
        string? soloTier = null,
        string? soloRank = null,
        int? soloLp = null)
    {
        return new RiotAccount
        {
            Puuid = puuid,
            GameName = "TestPlayer",
            TagLine = "NA1",
            SummonerName = "TestPlayer",
            Region = "EUW",
            SyncStatus = syncStatus,
            ProfileIconId = profileIconId,
            SummonerLevel = summonerLevel,
            SoloTier = soloTier,
            SoloRank = soloRank,
            SoloLp = soloLp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ---- in-test fakes ----

    private sealed class TrackingRiotAccountsRepository : IRiotAccountsRepository
    {
        private readonly ConcurrentDictionary<string, RiotAccount> _accounts = new();

        public int UpdateProfileCallCount { get; private set; }
        public int UpdateRankCallCount { get; private set; }
        public int UpdateSyncStatusCallCount { get; private set; }
        public string? LastSyncStatusSet { get; private set; }
        public List<string>? CallLog { get; init; }

        public void AddAccount(RiotAccount account) => _accounts[account.Puuid] = account;

        public Task UpsertAsync(RiotAccount account) { _accounts[account.Puuid] = account; return Task.CompletedTask; }
        public Task<RiotAccount?> GetByPuuidAsync(string puuid) { _accounts.TryGetValue(puuid, out var a); return Task.FromResult(a); }
        public Task<bool> ExistsByPuuidAsync(string puuid) => Task.FromResult(_accounts.ContainsKey(puuid));
        public Task DeleteAsync(string puuid) { _accounts.TryRemove(puuid, out _); return Task.CompletedTask; }

        public Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel)
        {
            UpdateProfileCallCount++;
            if (_accounts.TryGetValue(puuid, out var a))
            {
                a.ProfileIconId = profileIconId;
                a.SummonerLevel = summonerLevel;
            }
            return Task.CompletedTask;
        }

        public Task UpdateRankDataAsync(string puuid, string? summonerId, string? soloTier, string? soloRank, int? soloLp, string? flexTier, string? flexRank, int? flexLp)
        {
            UpdateRankCallCount++;
            return Task.CompletedTask;
        }

        public Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
        {
            UpdateSyncStatusCallCount++;
            LastSyncStatusSet = syncStatus;
            CallLog?.Add($"UpdateSyncStatus:{syncStatus}");
            if (_accounts.TryGetValue(puuid, out var a)) a.SyncStatus = syncStatus;
            return Task.CompletedTask;
        }

        public Task<RiotAccount?> ClaimNextPendingForSyncAsync() => Task.FromResult<RiotAccount?>(null);
        public Task ResetStuckSyncingAccountsAsync(TimeSpan threshold) => Task.CompletedTask;
        public Task UpdateSyncProgressAsync(string puuid, int progress, int total) => Task.CompletedTask;
    }

    private sealed class TrackingUserRiotAccountsRepository : IUserRiotAccountsRepository
    {
        private readonly ConcurrentDictionary<(long, string), UserRiotAccountLink> _links = new();
        private readonly TrackingRiotAccountsRepository _riotAccountsRepo;

        public TrackingUserRiotAccountsRepository(TrackingRiotAccountsRepository riotAccountsRepo)
            => _riotAccountsRepo = riotAccountsRepo;

        public void Link(long userId, string puuid, bool isPrimary = true)
            => _links[(userId, puuid)] = new UserRiotAccountLink { UserId = userId, Puuid = puuid, IsPrimary = isPrimary, LinkedAt = DateTime.UtcNow };

        public async Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId)
        {
            var results = new List<(UserRiotAccountLink, RiotAccount)>();
            foreach (var (key, link) in _links.Where(kv => kv.Key.Item1 == userId))
            {
                var account = await _riotAccountsRepo.GetByPuuidAsync(key.Item2);
                if (account != null) results.Add((link, account));
            }
            return results;
        }

        public Task LinkAsync(long userId, string puuid, bool isPrimary) { Link(userId, puuid, isPrimary); return Task.CompletedTask; }
        public Task UnlinkAsync(long userId, string puuid) { _links.TryRemove((userId, puuid), out _); return Task.CompletedTask; }
        public Task<bool> IsLinkedAsync(long userId, string puuid) => Task.FromResult(_links.ContainsKey((userId, puuid)));
        public Task<IList<long>> GetUserIdsByPuuidAsync(string puuid) => Task.FromResult<IList<long>>(_links.Where(kv => kv.Key.Item2 == puuid).Select(kv => kv.Key.Item1).ToList());
        public Task SetPrimaryAsync(long userId, string puuid) => Task.CompletedTask;
        public Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId) => Task.FromResult<(UserRiotAccountLink, RiotAccount)?>(null);
        public Task<bool> HasAnyLinksAsync(string puuid) => Task.FromResult(_links.Any(kv => kv.Key.Item2 == puuid));
        public Task<int> GetLinkCountAsync(string puuid) => Task.FromResult(_links.Count(kv => kv.Key.Item2 == puuid));
        public Task<int> GetLinkCountForUserAsync(long userId) => Task.FromResult(_links.Count(kv => kv.Key.Item1 == userId));
    }

    private sealed class ControllableRiotApiClient : IRiotApiClient
    {
        private readonly Dictionary<string, string> _summonerResponses = new();
        private readonly HashSet<string> _summonerThrows = new();
        private readonly Dictionary<string, string> _leagueEntriesResponses = new();
        private readonly Dictionary<string, string> _matchHistoryResponses = new();

        public int SummonerCallCount { get; private set; }
        public int MatchHistoryCallCount { get; private set; }

        public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;

        private void NotifyRateLimitWaitStarted(RateLimitWaitEventArgs args)
        {
            RateLimitWaitStarted?.Invoke(this, args);
        }

        public void SetSummonerResponse(string puuid, int profileIconId, int summonerLevel)
            => _summonerResponses[puuid] = $"{{\"profileIconId\":{profileIconId},\"summonerLevel\":{summonerLevel}}}";

        public void SetSummonerThrows(string puuid)
            => _summonerThrows.Add(puuid);

        public void SetLeagueEntriesResponse(string puuid, string json)
            => _leagueEntriesResponses[puuid] = json;

        public void SetMatchHistoryResponse(string puuid, string json)
            => _matchHistoryResponses[puuid] = json;

        public Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default)
        {
            SummonerCallCount++;
            if (_summonerThrows.Contains(puuid))
                throw new HttpRequestException("Simulated Riot API failure");
            _summonerResponses.TryGetValue(puuid, out var json);
            return Task.FromResult(JsonDocument.Parse(json ?? "{}"));
        }

        public Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default)
        {
            _leagueEntriesResponses.TryGetValue(puuid, out var json);
            return Task.FromResult(JsonDocument.Parse(json ?? "[]"));
        }

        public Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default)
        {
            MatchHistoryCallCount++;
            _matchHistoryResponses.TryGetValue(puuid, out var json);
            return Task.FromResult(JsonDocument.Parse(json ?? "[]"));
        }

        public Task<double> GetWinrateAsync(string puuid) => Task.FromResult(50.0);
        public Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default) => Task.FromResult("test-puuid");
        public Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default) => Task.FromResult(JsonDocument.Parse("{}"));
        public Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default) => Task.FromResult(JsonDocument.Parse("{}"));
        public Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default) => Task.FromResult(JsonDocument.Parse("[]"));
        public Task<string> GetLolVersionAsync(CancellationToken ct = default) => Task.FromResult("14.1.1");
        public void Dispose() { }
    }

    private sealed class TrackingSyncBroadcaster : ISyncProgressBroadcaster
    {
        public int BroadcastProgressCallCount { get; private set; }

        public Task BroadcastProgressAsync(string puuid, int progress, int total, string? currentMatchId = null)
        {
            BroadcastProgressCallCount++;
            return Task.CompletedTask;
        }

        public Task BroadcastCompleteAsync(string puuid, int totalSynced) => Task.CompletedTask;
        public Task BroadcastErrorAsync(string puuid, string error) => Task.CompletedTask;
        public Task BroadcastRateLimitedAsync(string puuid) => Task.CompletedTask;
    }

    private sealed class TrackingAggregator : ISyncProgressAggregator
    {
        public List<(long UserId, string[] Puuids)> StartRunCalls { get; } = new();
        public List<string>? CallLog { get; init; }

        public Task StartRunAsync(long userId, IReadOnlyList<string> puuids)
        {
            StartRunCalls.Add((userId, puuids.ToArray()));
            CallLog?.Add("StartRun");
            return Task.CompletedTask;
        }

        public Task OnProgressAsync(string puuid, int progress, int total, string? matchId) => Task.CompletedTask;
        public Task OnCompleteAsync(string puuid, int totalSynced) => Task.CompletedTask;
        public Task OnErrorAsync(string puuid, string error) => Task.CompletedTask;
    }

    private sealed class TrackingQueueSignal : ISyncQueueSignal
    {
        public int NotifyCount { get; private set; }

        public void Notify() => NotifyCount++;

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
