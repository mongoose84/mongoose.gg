using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class RankSnapshotServiceTests
{
    private const string Puuid = "puuid-1";
    private static readonly DateTime T0 = new(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc);

    private readonly FakeRankSnapshotsRepository _snapshots = new();
    private readonly RecordingParticipantsRepository _participants = new();
    private readonly InMemoryRiotAccountsRepository _accounts = new();
    private readonly CountingQueueSignal _signal = new();
    private readonly LeagueOnlyRiotApiClient _riot = new();
    private readonly RankSnapshotService _sut;

    public RankSnapshotServiceTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jobs:RankSnapshotClockSkewSeconds"] = "120",
            ["Jobs:RankSnapshotPendingTimeoutMinutes"] = "60"
        }).Build();
        _sut = new RankSnapshotService(_snapshots, _participants, _accounts, _signal, config, NullLogger<RankSnapshotService>.Instance);
        _accounts.Add(Puuid);
    }

    private static LeagueRankReading Solo(int lp, int wins, int losses, string tier = "EMERALD", string division = "II") =>
        new(420, tier, division, lp, wins, losses);

    private static long EndMs(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeMilliseconds();

    [Fact]
    public void ParseLeagueEntries_ReadsRankedQueuesOnly()
    {
        using var doc = JsonDocument.Parse("[" +
            LeagueOnlyRiotApiClient.Entry("RANKED_SOLO_5x5", "EMERALD", "II", 64, 30, 28) + "," +
            LeagueOnlyRiotApiClient.Entry("RANKED_FLEX_SR", "GOLD", "I", 12, 5, 7) + "," +
            LeagueOnlyRiotApiClient.Entry("CHERRY", "GOLD", "I", 12, 5, 7) + "," +
            """{"queueType":"RANKED_SOLO_5x5"}""" + "]");

        RankSnapshotService.ParseLeagueEntries(doc).Should().BeEquivalentTo(new[]
        {
            new LeagueRankReading(420, "EMERALD", "II", 64, 30, 28),
            new LeagueRankReading(440, "GOLD", "I", 12, 5, 7)
        });
    }

    [Fact]
    public async Task RecordAsync_StoresABaselineFirst_ThenAPendingSnapshotAfterOneMatch()
    {
        (await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0)).Should().BeFalse();
        (await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40))).Should().BeTrue();

        _snapshots.Snapshots.Select(s => s.Status).Should().Equal(RankSnapshotStatus.Baseline, RankSnapshotStatus.Pending);
        _snapshots.Snapshots[1].WindowStartAt.Should().Be(T0);
        _snapshots.RankCheckedAt[Puuid].Should().Be(T0.AddMinutes(40));
    }

    [Fact]
    public async Task RecordAsync_AddsNoRowWhenNothingChanged_ButStillMarksTheAccountRead()
    {
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(20));

        _snapshots.Snapshots.Should().ContainSingle();
        _snapshots.RankCheckedAt[Puuid].Should().Be(T0.AddMinutes(20));
    }

    [Fact]
    public async Task CaptureAsync_ReadsRiot_UpdatesTheAccountRank_AndStoresSnapshots()
    {
        _riot.LeagueJsonByPuuid[Puuid] = "[" + LeagueOnlyRiotApiClient.Entry("RANKED_FLEX_SR", "BRONZE", "III", 17, 4, 6) + "]";

        var pending = await _sut.CaptureAsync(_accounts.Accounts[Puuid], RankSnapshotSource.Poll, _riot, CancellationToken.None);

        pending.Should().BeFalse();
        _accounts.RankUpdates.Should().ContainSingle().Which.Should().Be((Puuid, (string?)null, (int?)null, (string?)"BRONZE", (int?)17));
        _snapshots.Snapshots.Should().ContainSingle(s => s.QueueId == 440 && s.Lp == 17 && s.Status == RankSnapshotStatus.Baseline);
    }

    [Fact]
    public async Task AttributePendingAsync_WritesTheReadingOntoTheOneMatchThatEndedInTheWindow()
    {
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40));
        _snapshots.MatchEnds.Add((Puuid, 420, "EUW1_OLD", EndMs(T0.AddMinutes(-30))));   // before the window
        _snapshots.MatchEnds.Add((Puuid, 440, "EUW1_FLEX", EndMs(T0.AddMinutes(20))));   // other queue
        _snapshots.MatchEnds.Add((Puuid, 420, "EUW1_NEW", EndMs(T0.AddMinutes(35))));

        var attributed = await _sut.AttributePendingAsync(null, T0.AddMinutes(41), queueSyncWhenMissing: true);

        attributed.Should().Be(1);
        _participants.LpData.Should().ContainSingle().Which.Should().Be(
            new KeyValuePair<(string, string), (int?, string?, string?)>(("EUW1_NEW", Puuid), (59, "EMERALD", "II")));
        _snapshots.Snapshots[1].Status.Should().Be(RankSnapshotStatus.Attributed);
        _snapshots.Snapshots[1].MatchId.Should().Be("EUW1_NEW");
    }

    [Fact]
    public async Task AttributePendingAsync_SkipsWhenTwoMatchesEndedInTheWindow()
    {
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40));
        _snapshots.MatchEnds.Add((Puuid, 420, "EUW1_A", EndMs(T0.AddMinutes(10))));
        _snapshots.MatchEnds.Add((Puuid, 420, "EUW1_B", EndMs(T0.AddMinutes(35))));

        await _sut.AttributePendingAsync(Puuid, T0.AddMinutes(41), queueSyncWhenMissing: false);

        _participants.LpData.Should().BeEmpty();
        _snapshots.Snapshots[1].Status.Should().Be(RankSnapshotStatus.Skipped);
    }

    [Fact]
    public async Task AttributePendingAsync_WaitsForAMatchNotStoredYet_AndQueuesASyncOnce()
    {
        _accounts.Accounts[Puuid].LastSyncAt = T0;
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40));
        // The previous match is stored, but it ended before the window: never a candidate
        _snapshots.MatchEnds.Add((Puuid, 420, "EUW1_PREVIOUS", EndMs(T0.AddMinutes(-5))));

        await _sut.AttributePendingAsync(null, T0.AddMinutes(41), queueSyncWhenMissing: true);

        _participants.LpData.Should().BeEmpty();
        _snapshots.Snapshots[1].Status.Should().Be(RankSnapshotStatus.Pending);
        _accounts.Accounts[Puuid].SyncStatus.Should().Be("pending");
        _signal.Notifications.Should().Be(1);

        // Already queued: no second sync
        await _sut.AttributePendingAsync(null, T0.AddMinutes(42), queueSyncWhenMissing: true);
        _signal.Notifications.Should().Be(1);
    }

    [Fact]
    public async Task AttributePendingAsync_QueuesNoSync_OnceASyncRanWellAfterTheReading()
    {
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40));
        _accounts.Accounts[Puuid].LastSyncAt = T0.AddMinutes(45);

        await _sut.AttributePendingAsync(null, T0.AddMinutes(46), queueSyncWhenMissing: true);

        _signal.Notifications.Should().Be(0);
        _snapshots.Snapshots[1].Status.Should().Be(RankSnapshotStatus.Pending);
    }

    [Fact]
    public async Task AttributePendingAsync_SkipsAfterTheTimeout()
    {
        await _sut.RecordAsync(Puuid, new[] { Solo(40, 10, 10) }, RankSnapshotSource.Poll, T0);
        await _sut.RecordAsync(Puuid, new[] { Solo(59, 11, 10) }, RankSnapshotSource.Poll, T0.AddMinutes(40));

        await _sut.AttributePendingAsync(null, T0.AddMinutes(101), queueSyncWhenMissing: true);

        _snapshots.Snapshots[1].Status.Should().Be(RankSnapshotStatus.Skipped);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("syncing")]
    public async Task QueueSyncAsync_LeavesAQueuedOrRunningSyncAlone(string status)
    {
        _accounts.Accounts[Puuid].SyncStatus = status;

        (await _sut.QueueSyncAsync(Puuid)).Should().BeFalse();
        _signal.Notifications.Should().Be(0);
    }
}
