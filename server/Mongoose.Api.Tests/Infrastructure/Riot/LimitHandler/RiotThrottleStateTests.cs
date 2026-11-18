using FluentAssertions;
using Mongoose.Api.Infrastructure.Riot.LimitHandler;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// FR 40: the limiter reports waiting once a request has waited more than 2 seconds, until the
/// bucket's next refill, and clears when the request gets its token.
/// </summary>
public class RiotThrottleStateTests
{
    /// <summary>A clock the test moves by hand; timers stay real.</summary>
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task TokenBucket_TracksTheOldestWaiter_UntilItStopsWaiting()
    {
        var clock = new ManualClock();
        using var bucket = new TokenBucket(1, TimeSpan.FromHours(1), clock);
        await bucket.WaitAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource();
        var waiting = bucket.WaitAsync(cts.Token);

        bucket.OldestWaitStartedAt.Should().Be(clock.Now);
        bucket.NextRefillAt.Should().Be(clock.Now + TimeSpan.FromHours(1));

        cts.Cancel();
        await FluentActions.Awaiting(() => waiting).Should().ThrowAsync<OperationCanceledException>();
        bucket.OldestWaitStartedAt.Should().BeNull();
    }

    [Fact]
    public async Task WaitingUntilUtc_IsNull_WhileNothingWaits()
    {
        using var handler = new RiotLimitHandler(new ManualClock());

        await handler.WaitAsync();

        handler.WaitingUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task WaitingUntilUtc_ReportsTheRefill_AfterTwoSecondsOfWaiting_AndClearsWhenGranted()
    {
        var clock = new ManualClock();
        using var handler = new RiotLimitHandler(clock);
        for (var i = 0; i < 10; i++) await handler.WaitAsync();

        // The 11th request waits for the per-second bucket, which really refills within a second
        var waiting = handler.WaitAsync();

        clock.Now += TimeSpan.FromSeconds(1.5);
        handler.WaitingUntilUtc.Should().BeNull("1.5 seconds is not yet waiting on Riot");

        clock.Now += TimeSpan.FromSeconds(1);
        handler.WaitingUntilUtc.Should().NotBeNull();
        handler.WaitingUntilUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);

        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        handler.WaitingUntilUtc.Should().BeNull();
    }
}
