using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Infrastructure.WebSocket;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for the per-user "Analyze all" aggregator. Uses a capturing fake broadcaster
/// (the <see cref="IUserSyncBroadcaster"/> seam) so combine logic is verified without sockets.
/// </summary>
public class SyncProgressAggregatorTests
{
    private sealed class CapturingBroadcaster : IUserSyncBroadcaster
    {
        public List<(long UserId, SyncAggregateMessage Message)> Sent { get; } = new();

        public Task BroadcastToUserAsync(long userId, SyncAggregateMessage message)
        {
            Sent.Add((userId, message));
            return Task.CompletedTask;
        }

        public SyncAggregateMessage? Last(long userId) =>
            Sent.Where(s => s.UserId == userId).Select(s => s.Message).LastOrDefault();
    }

    private static SyncProgressAggregator CreateAggregator(out CapturingBroadcaster broadcaster)
    {
        broadcaster = new CapturingBroadcaster();
        return new SyncProgressAggregator(broadcaster, NullLogger<SyncProgressAggregator>.Instance);
    }

    [Fact]
    public async Task StartRun_BroadcastsInitialSyncingState()
    {
        var aggregator = CreateAggregator(out var broadcaster);

        await aggregator.StartRunAsync(1, new[] { "p1", "p2" });

        var msg = broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>().Subject;
        msg.Status.Should().Be("syncing");
        msg.AccountsTotal.Should().Be(2);
        msg.AccountsDone.Should().Be(0);
        msg.Progress.Should().Be(0);
        msg.Total.Should().Be(0);
    }

    [Fact]
    public async Task Progress_SumsAcrossAccounts()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1", "p2" });

        await aggregator.OnProgressAsync("p1", 3, 10, "m1");
        await aggregator.OnProgressAsync("p2", 5, 20, "m2");

        var msg = broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>().Subject;
        msg.Progress.Should().Be(8);   // 3 + 5
        msg.Total.Should().Be(30);     // 10 + 20
        msg.AccountsTotal.Should().Be(2);
        msg.AccountsDone.Should().Be(0);
    }

    [Fact]
    public async Task Complete_IsBroadcastOnlyWhenAllAccountsSettle()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1", "p2" });

        await aggregator.OnProgressAsync("p1", 10, 10, null);
        await aggregator.OnCompleteAsync("p1", 10);

        // p2 is still pending → the run is not complete yet.
        broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>();

        await aggregator.OnProgressAsync("p2", 20, 20, null);
        await aggregator.OnCompleteAsync("p2", 20);

        var msg = broadcaster.Last(1).Should().BeOfType<SyncAggregateCompleteMessage>().Subject;
        msg.Status.Should().Be("completed");
        msg.TotalSynced.Should().Be(30);
        msg.AccountsTotal.Should().Be(2);
    }

    [Fact]
    public async Task Run_ResolvesAsFailed_WhenAnyAccountFails()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1", "p2" });

        await aggregator.OnErrorAsync("p1", "boom");

        // p2 not settled yet → still reporting progress, not failed.
        broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>();

        await aggregator.OnCompleteAsync("p2", 5);

        var msg = broadcaster.Last(1).Should().BeOfType<SyncAggregateErrorMessage>().Subject;
        msg.Status.Should().Be("failed");
        msg.Error.Should().Be("boom");
    }

    [Fact]
    public async Task SharedAccount_UpdatesEveryOwningUsersRun()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "shared" });
        await aggregator.StartRunAsync(2, new[] { "shared" });

        await aggregator.OnProgressAsync("shared", 4, 8, "m");

        broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>().Which.Progress.Should().Be(4);
        broadcaster.Last(2).Should().BeOfType<SyncAggregateProgressMessage>().Which.Progress.Should().Be(4);
    }

    [Fact]
    public async Task EventsForPuuidNotInAnyRun_AreIgnored()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1" });
        broadcaster.Sent.Clear();

        await aggregator.OnProgressAsync("not-in-run", 1, 1, null);

        broadcaster.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task CompletedRun_IsClearedSoLateEventsAreIgnored()
    {
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1" });
        await aggregator.OnCompleteAsync("p1", 3);
        broadcaster.Sent.Clear();

        // A late event for the same account does nothing once the run has been removed.
        await aggregator.OnProgressAsync("p1", 1, 1, null);

        broadcaster.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task StartRun_WithNoAccounts_DoesNotBroadcast()
    {
        var aggregator = CreateAggregator(out var broadcaster);

        await aggregator.StartRunAsync(1, Array.Empty<string>());

        broadcaster.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task StartRun_ExtendsExistingRun_RatherThanReplacingIt()
    {
        // A multi-account seed plus the job's per-account ensure must converge on one run.
        var aggregator = CreateAggregator(out var broadcaster);
        await aggregator.StartRunAsync(1, new[] { "p1" });
        await aggregator.StartRunAsync(1, new[] { "p2" }); // extend, not replace

        var seeded = broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>().Subject;
        seeded.AccountsTotal.Should().Be(2);

        // Both accounts now feed the same combined run and only complete together.
        await aggregator.OnProgressAsync("p1", 4, 4, null);
        await aggregator.OnCompleteAsync("p1", 4);
        broadcaster.Last(1).Should().BeOfType<SyncAggregateProgressMessage>(); // p2 still pending

        await aggregator.OnProgressAsync("p2", 6, 6, null);
        await aggregator.OnCompleteAsync("p2", 6);

        var done = broadcaster.Last(1).Should().BeOfType<SyncAggregateCompleteMessage>().Subject;
        done.TotalSynced.Should().Be(10);
        done.AccountsTotal.Should().Be(2);
    }
}
