using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Jobs;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class RankSnapshotJobTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc);

    private readonly FakeRankSnapshotsRepository _snapshots = new();
    private readonly InMemoryRiotAccountsRepository _accounts = new();
    private readonly CountingQueueSignal _signal = new();
    private readonly LeagueOnlyRiotApiClient _riot = new();
    private readonly RecordingLogger<RankSnapshotJob> _logger = new();
    private readonly RankSnapshotJob _sut;

    public RankSnapshotJobTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jobs:RankSnapshotIntervalMinutes"] = "20",
            ["Jobs:RankSnapshotActiveDays"] = "7",
            ["Jobs:RankSnapshotMaxCallsPerMinute"] = "2",
            ["Jobs:RankSnapshotRetentionDays"] = "30"
        }).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<ILogger<RankSnapshotService>>(NullLogger<RankSnapshotService>.Instance);
        services.AddSingleton<IRankSnapshotsRepository>(_snapshots);
        services.AddSingleton<IRiotAccountsRepository>(_accounts);
        services.AddSingleton<IParticipantsRepository>(new RecordingParticipantsRepository());
        services.AddSingleton<ISyncQueueSignal>(_signal);
        services.AddSingleton<IRiotApiClient>(_riot);
        services.AddScoped<RankSnapshotService>();

        _sut = new RankSnapshotJob(services.BuildServiceProvider(), _logger, config);
    }

    [Fact]
    public async Task RunOnceAsync_AsksForAccountsOfUsersActiveInTheLastWeekNotReadFor20Minutes_WithinTheCap()
    {
        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _snapshots.LastDueQuery.Should().Be((Now.AddDays(-7), Now.AddMinutes(-20), 2));
    }

    [Fact]
    public async Task RunOnceAsync_ReadsDueAccounts_AndQueuesASyncWhenOneMatchWasPlayed()
    {
        _accounts.Add("a");
        _accounts.Add("b");
        _snapshots.DuePuuids.AddRange(new[] { "a", "b", "c" });
        _snapshots.Snapshots.Add(new RankSnapshotRecord
        {
            Id = 99, Puuid = "a", QueueId = 420, Tier = "GOLD", Division = "II", Lp = 40, Wins = 10, Losses = 10,
            CapturedAt = Now.AddMinutes(-40), Status = RankSnapshotStatus.Baseline
        });
        _riot.LeagueJsonByPuuid["a"] = "[" + LeagueOnlyRiotApiClient.Entry("RANKED_SOLO_5x5", "GOLD", "II", 58, 11, 10) + "]";
        _riot.LeagueJsonByPuuid["b"] = "[" + LeagueOnlyRiotApiClient.Entry("RANKED_SOLO_5x5", "GOLD", "IV", 5, 3, 3) + "]";

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _riot.LeagueCalls.Should().Equal("a", "b");   // the cap of 2 leaves "c" for the next tick
        _accounts.Accounts["a"].SyncStatus.Should().Be("pending");
        _accounts.Accounts["b"].SyncStatus.Should().Be("completed");
        _signal.Notifications.Should().Be(1);
    }

    [Fact]
    public async Task RunOnceAsync_MarksAFailingAccountRead_SoItDoesNotBlockTheRest()
    {
        _accounts.Add("a");
        _snapshots.DuePuuids.Add("a");
        _riot.FailuresByPuuid["a"] = new HttpRequestException("boom", null, HttpStatusCode.InternalServerError);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.FromMinutes(1));
        _snapshots.RankCheckedAt["a"].Should().Be(Now);
    }

    [Fact]
    public async Task RunOnceAsync_BacksOffOnARiotRateLimit()
    {
        _accounts.Add("a");
        _accounts.Add("b");
        _snapshots.DuePuuids.AddRange(new[] { "a", "b" });
        _riot.FailuresByPuuid["a"] = new HttpRequestException("slow down", null, HttpStatusCode.TooManyRequests);

        var delay = await _sut.RunOnceAsync(Now, CancellationToken.None);

        delay.Should().Be(TimeSpan.FromMinutes(2));
        _riot.LeagueCalls.Should().Equal("a");
    }

    [Fact]
    public async Task RunOnceAsync_LogsItsLeagueCallsAgainstTheCapAtDebug()
    {
        _accounts.Add("a");
        _accounts.Add("b");
        _snapshots.DuePuuids.AddRange(new[] { "a", "b" });

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Debug)
            .Which.Message.Should().Be("RankSnapshotJob made 2 of 2 League-v4 calls this minute (2 due, 0 syncs queued)");
    }

    [Fact]
    public async Task RunOnceAsync_LogsTheCallThatHitTheRateLimit()
    {
        _accounts.Add("a");
        _snapshots.DuePuuids.Add("a");
        _riot.FailuresByPuuid["a"] = new HttpRequestException("slow down", null, HttpStatusCode.TooManyRequests);

        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Debug && e.Message.StartsWith("RankSnapshotJob made 1 of 2"));
    }

    [Fact]
    public async Task RunOnceAsync_LogsNoCallCountWhenNothingIsDue()
    {
        await _sut.RunOnceAsync(Now, CancellationToken.None);

        _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task RunOnceAsync_PrunesOldSnapshotsOnceADay()
    {
        await _sut.RunOnceAsync(Now, CancellationToken.None);
        _snapshots.LastRetentionCutoff.Should().Be(Now.AddDays(-30));

        await _sut.RunOnceAsync(Now.AddHours(1), CancellationToken.None);
        _snapshots.LastRetentionCutoff.Should().Be(Now.AddDays(-30));

        await _sut.RunOnceAsync(Now.AddDays(1), CancellationToken.None);
        _snapshots.LastRetentionCutoff.Should().Be(Now.AddDays(1).AddDays(-30));
    }
}
