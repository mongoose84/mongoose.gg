using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Jobs;
using Mongoose.Api.Infrastructure.Services;
using Mongoose.Api.Infrastructure.WebSocket;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class DeathDetailBackfillJobTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 20, 0, 0, DateTimeKind.Utc);

    private readonly FakeBackfillRepository _repo = new();
    private readonly FakeParticipants _participants = new();
    private readonly RecordingWriter _writer = new();
    private readonly TimelineRiotApiClient _riot = new();
    private readonly RecordingBroadcaster _broadcaster = new();
    private readonly FakeThrottle _throttle = new();
    private readonly DeathDetailBackfillState _state = new();
    private readonly RiotBackgroundActivity _activity = new();
    private readonly DeathDetailBackfillJob _sut;

    public DeathDetailBackfillJobTests()
    {
        var riotAccounts = new TestWebApplicationFactory.FakeRiotAccountsRepository();
        var links = new TestWebApplicationFactory.FakeUserRiotAccountsRepository(riotAccounts);
        foreach (var puuid in new[] { "a", "b" })
        {
            riotAccounts.AddRiotAccount(7, puuid, "Player", "EUW1", "Player#EUW", 100, 1);
            links.LinkAccount(7, puuid);
        }

        var services = new ServiceCollection();
        services.AddSingleton<IDeathDetailBackfillRepository>(_repo);
        services.AddSingleton<IParticipantsRepository>(_participants);
        services.AddSingleton<IDeathDetailWriter>(_writer);
        services.AddSingleton<IRiotApiClient>(_riot);
        services.AddSingleton<IUserSyncBroadcaster>(_broadcaster);
        services.AddSingleton<IUserRiotAccountsRepository>(links);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jobs:RankSnapshotActiveDays"] = "7"
        }).Build();

        _sut = new DeathDetailBackfillJob(services.BuildServiceProvider(), _state, _activity, _throttle,
            new RecordingLogger<DeathDetailBackfillJob>(), config);
    }

    /// <summary>An account with <paramref name="done"/> matches done and <paramref name="pending"/> to do, newest first.</summary>
    private void Account(string puuid, int done, int pending)
    {
        _repo.Accounts.Add(puuid);
        _repo.Matches[puuid] = Enumerable.Range(0, pending).Select(i => new BackfillMatch($"{puuid}_P{i}", false))
            .Concat(Enumerable.Range(0, done).Select(i => new BackfillMatch($"{puuid}_D{i}", true)))
            .ToList();
    }

    // ───────────────────────── Order ─────────────────────────

    [Fact]
    public async Task RunOnceAsync_BackfillsTheNewestPendingMatch_OfTheMostRecentlyActiveAccount()
    {
        Account("a", done: 2, pending: 3);
        Account("b", done: 0, pending: 1);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.Zero);
        _repo.LastActiveSince.Should().Be(Now.AddDays(-7));
        _riot.TimelineCalls.Should().Equal("a_P0");
        _writer.Written.Should().ContainSingle().Which.MatchId.Should().Be("a_P0");
        _participants.IdsSet.Should().ContainKey("a_P0");
    }

    [Fact]
    public async Task RunOnceAsync_WritesWithTheTimelinesParticipantIds()
    {
        Account("a", done: 0, pending: 1);

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        var (_, participants) = _writer.Written.Single();
        participants[1].Should().Be(new TimelineParticipant(1001, 101));
        participants[10].Should().Be(new TimelineParticipant(1010, 110));
        _participants.IdsSet["a_P0"]["puuid-3"].Should().Be(3);
    }

    [Fact]
    public async Task RunOnceAsync_TakesTheAccountAPlayerAskedFor_First()
    {
        Account("a", done: 0, pending: 1);
        Account("b", done: 0, pending: 1);
        _state.Prioritize("b");

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _riot.TimelineCalls.Should().Equal("b_P0");
    }

    [Fact]
    public async Task RunOnceAsync_FinishesAnAccount_WhenNothingIsLeft()
    {
        Account("a", done: 4, pending: 0);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.Zero);
        _repo.DoneAccounts.Should().Equal(("a", Now));
        _state.Get("a").Should().BeNull();
        _broadcaster.Messages.Should().ContainSingle().Which.Message.Status.Should().Be(DeathDetailBackfillProgress.Complete);
    }

    [Fact]
    public async Task RunOnceAsync_Idles_WithoutAccounts()
    {
        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.FromMinutes(5));
        _riot.TimelineCalls.Should().BeEmpty();
    }

    // ───────────────────────── Priority and budget ─────────────────────────

    [Fact]
    public async Task RunOnceAsync_WaitsBehindAMatchSync_AndSaysQueued()
    {
        Account("a", done: 1, pending: 2);
        _repo.SyncActive = true;

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.FromSeconds(15));
        _riot.TimelineCalls.Should().BeEmpty();
        _state.Get("a").Should().Be(new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Queued, 1, 3));
    }

    [Fact]
    public async Task RunOnceAsync_WaitsWhileRankSnapshotsRun()
    {
        Account("a", done: 0, pending: 1);

        using (_activity.RankSnapshotsTick())
        {
            await _sut.RunOnceAsync(Now, CancellationToken.None);
        }

        _riot.TimelineCalls.Should().BeEmpty();
        await _sut.RunOnceAsync(Now, CancellationToken.None);
        _riot.TimelineCalls.Should().Equal("a_P0");
    }

    [Fact]
    public async Task RunOnceAsync_SpendsAtMost20TimelineRequestsPer2Minutes()
    {
        Account("a", done: 0, pending: 25);

        for (var i = 0; i < 20; i++)
        {
            await _sut.RunOnceAsync(Now.AddSeconds(i), CancellationToken.None);
            _repo.Complete("a", $"a_P{i}");
        }

        var delay = await _sut.RunOnceAsync(Now.AddSeconds(20), CancellationToken.None);

        _riot.TimelineCalls.Should().HaveCount(20);
        delay.Should().Be(TimeSpan.FromSeconds(100)); // the first request leaves the window at 2:00
        await _sut.RunOnceAsync(Now.AddMinutes(2), CancellationToken.None);
        _riot.TimelineCalls.Should().HaveCount(21);
    }

    // ───────────────────────── Failures ─────────────────────────

    [Fact]
    public async Task RunOnceAsync_SkipsAMatchRiotNoLongerServes()
    {
        Account("a", done: 0, pending: 2);
        _riot.Failures["a_P0"] = new HttpRequestException("gone", null, HttpStatusCode.NotFound);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.Zero);
        _repo.Skipped.Should().Equal(("a_P0", DeathDetailBackfillJob.SkipNotFound));
        _writer.Written.Should().BeEmpty();
    }

    [Fact]
    public async Task RunOnceAsync_BacksOffOnARateLimit_AndSaysWaiting()
    {
        Account("a", done: 3, pending: 2);
        _riot.Failures["a_P0"] = new HttpRequestException("slow down", null, HttpStatusCode.TooManyRequests);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.FromMinutes(2));
        _state.Get("a").Should().Be(new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, 3, 5, Now.AddMinutes(2)));
        _repo.Skipped.Should().BeEmpty();
    }

    [Fact]
    public async Task RunOnceAsync_SkipsATimelineWithoutKnownParticipants()
    {
        Account("a", done: 0, pending: 1);
        _participants.Rows["a_P0"] = [];

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _repo.Skipped.Should().Equal(("a_P0", DeathDetailBackfillJob.SkipUnreadable));
        _participants.IdsSet.Should().BeEmpty();
    }

    [Fact]
    public async Task RunOnceAsync_SaysWaiting_WhileTheRateLimiterHoldsTheRequest()
    {
        Account("a", done: 1, pending: 1);
        var release = new TaskCompletionSource<JsonDocument>();
        _riot.Pending["a_P0"] = release.Task;
        var retryAt = Now.AddSeconds(40);
        _throttle.WaitingUntilUtc = retryAt;

        var run = _sut.RunOnceAsync(Now, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        _state.Get("a").Should().Be(new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, 1, 2, retryAt));

        _throttle.WaitingUntilUtc = null;
        release.SetResult(TimelineRiotApiClient.Timeline());
        await run;
        _state.Get("a")!.Status.Should().Be(DeathDetailBackfillProgress.Running);
    }

    // ───────────────────────── Progress ─────────────────────────

    [Fact]
    public async Task RunOnceAsync_SendsProgress_OnAStatusChangeAndEveryFiveMatches()
    {
        Account("a", done: 0, pending: 7);

        for (var i = 0; i < 7; i++)
        {
            await _sut.RunOnceAsync(Now.AddSeconds(i), CancellationToken.None);
            _repo.Complete("a", $"a_P{i}");
        }

        // running (status change), then 5 of 7, then 7 of 7
        _broadcaster.Messages.Select(m => (m.UserId, m.Message.Status, m.Message.Done)).Should().Equal(
            (7L, "running", 0), (7L, "running", 5), (7L, "running", 7));
        _state.Get("a").Should().Be(new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Running, 7, 7));
    }

    // ───────────────────────── Fakes ─────────────────────────

    private sealed class FakeBackfillRepository : IDeathDetailBackfillRepository
    {
        public List<string> Accounts { get; } = new();
        public Dictionary<string, List<BackfillMatch>> Matches { get; } = new();
        public List<(string MatchId, string Reason)> Skipped { get; } = new();
        public List<(string Puuid, DateTime At)> DoneAccounts { get; } = new();
        public bool SyncActive { get; set; }
        public DateTime? LastActiveSince { get; private set; }

        public void Complete(string puuid, string matchId)
        {
            var list = Matches[puuid];
            var i = list.FindIndex(m => m.MatchId == matchId);
            list[i] = list[i] with { Done = true };
        }

        public Task<IList<string>> GetAccountsToBackfillAsync(DateTime activeSinceUtc, int limit)
        {
            LastActiveSince = activeSinceUtc;
            return Task.FromResult<IList<string>>(Accounts.Where(a => !DoneAccounts.Any(d => d.Puuid == a)).Take(limit).ToList());
        }

        public Task<IList<BackfillMatch>> GetRecentMatchesAsync(string puuid, int limit)
        {
            var skipped = Skipped.Select(s => s.MatchId).ToHashSet();
            return Task.FromResult<IList<BackfillMatch>>(Matches.GetValueOrDefault(puuid, [])
                .Select(m => skipped.Contains(m.MatchId) ? m with { Done = true } : m)
                .Take(limit).ToList());
        }

        public Task MarkSkippedAsync(string matchId, string reason, DateTime skippedAtUtc)
        {
            Skipped.Add((matchId, reason));
            return Task.CompletedTask;
        }

        public Task MarkAccountDoneAsync(string puuid, DateTime doneAtUtc)
        {
            DoneAccounts.Add((puuid, doneAtUtc));
            return Task.CompletedTask;
        }

        public Task<bool> IsSyncActiveAsync() => Task.FromResult(SyncActive);
    }

    /// <summary>Every match has participants puuid-1 … puuid-10 with row IDs 1001-1010 and champions 101-110.</summary>
    private sealed class FakeParticipants : IParticipantsRepository
    {
        public Dictionary<string, IList<Participant>> Rows { get; } = new();
        public Dictionary<string, IReadOnlyDictionary<string, int>> IdsSet { get; } = new();

        public Task<IList<Participant>> GetByMatchAsync(string matchId) =>
            Task.FromResult(Rows.TryGetValue(matchId, out var rows)
                ? rows
                : (IList<Participant>)Enumerable.Range(1, 10)
                    .Select(i => new Participant { Id = 1000 + i, MatchId = matchId, Puuid = $"puuid-{i}", ChampionId = 100 + i })
                    .ToList());

        public Task SetRiotParticipantIdsAsync(string matchId, IReadOnlyDictionary<string, int> participantIds)
        {
            IdsSet[matchId] = participantIds;
            return Task.CompletedTask;
        }

        public Task<long> InsertAsync(Participant participant) => throw new NotSupportedException();
        public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank) => throw new NotSupportedException();
        public Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid) => throw new NotSupportedException();
        public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit) => throw new NotSupportedException();
    }

    private sealed class RecordingWriter : IDeathDetailWriter
    {
        public List<(string MatchId, IReadOnlyDictionary<int, TimelineParticipant> Participants)> Written { get; } = new();

        public Task WriteAsync(string matchId, JsonElement timelineRoot, IReadOnlyDictionary<int, TimelineParticipant> participants)
        {
            Written.Add((matchId, participants));
            return Task.CompletedTask;
        }
    }

    private sealed class TimelineRiotApiClient : IRiotApiClient
    {
        public List<string> TimelineCalls { get; } = new();
        public Dictionary<string, Exception> Failures { get; } = new();
        public Dictionary<string, Task<JsonDocument>> Pending { get; } = new();

        public static JsonDocument Timeline()
        {
            var participants = string.Join(",", Enumerable.Range(1, 10).Select(i => $$"""{"participantId":{{i}},"puuid":"puuid-{{i}}"}"""));
            return JsonDocument.Parse($$$"""{"info":{"participants":[{{{participants}}}],"frames":[]}}""");
        }

        public Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default)
        {
            TimelineCalls.Add(matchId);
            if (Failures.TryGetValue(matchId, out var failure)) return Task.FromException<JsonDocument>(failure);
            if (Pending.TryGetValue(matchId, out var pending)) return pending;
            return Task.FromResult(Timeline());
        }

        public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted { add { } remove { } }
        public Task<double> GetWinrateAsync(string puuid) => throw new NotSupportedException();
        public Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string> GetLolVersionAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class RecordingBroadcaster : IUserSyncBroadcaster
    {
        public List<(long UserId, DetailBackfillProgressMessage Message)> Messages { get; } = new();

        public Task BroadcastToUserAsync(long userId, SyncAggregateMessage message)
        {
            Messages.Add((userId, (DetailBackfillProgressMessage)message));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeThrottle : IRiotThrottleState
    {
        public DateTime? WaitingUntilUtc { get; set; }
    }
}
