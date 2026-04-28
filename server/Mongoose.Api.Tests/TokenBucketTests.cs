using FluentAssertions;
using Mongoose.Api.Infrastructure.Riot.LimitHandler;
using Xunit;

namespace Mongoose.Api.Tests;

public class TokenBucketTests
{
    // ─────────────── Constructor validation ───────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_ThrowsArgumentOutOfRangeException_ForNonPositiveCapacity(int capacity)
    {
        var act = () => new TokenBucket(capacity, TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("capacity");
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_ForZeroRefillPeriod()
    {
        var act = () => new TokenBucket(1, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("refillPeriod");
    }

    [Fact]
    public void Constructor_ThrowsArgumentOutOfRangeException_ForNegativeRefillPeriod()
    {
        var act = () => new TokenBucket(1, TimeSpan.FromSeconds(-1));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("refillPeriod");
    }

    // ─────────────── WaitAsync — happy path ───────────────

    [Fact]
    public async Task WaitAsync_AcquiresTokenImmediately_WhenTokensAvailable()
    {
        using var bucket = new TokenBucket(1, TimeSpan.FromHours(1));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Should complete immediately without waiting
        await bucket.WaitAsync(cts.Token);
    }

    [Fact]
    public async Task WaitAsync_AcquiresAllCapacityTokensSequentially_WithoutBlocking()
    {
        const int capacity = 5;
        using var bucket = new TokenBucket(capacity, TimeSpan.FromHours(1));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Should acquire all tokens via the fast path
        for (var i = 0; i < capacity; i++)
        {
            await bucket.WaitAsync(cts.Token);
        }
    }

    // ─────────────── WaitAsync — disposed ───────────────

    [Fact]
    public async Task WaitAsync_ThrowsObjectDisposedException_AfterDispose()
    {
        var bucket = new TokenBucket(1, TimeSpan.FromHours(1));
        bucket.Dispose();

        var act = async () => await bucket.WaitAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    // ─────────────── WaitingStartedEvent ───────────────

    [Fact]
    public async Task WaitAsync_RaisesWaitingStartedEvent_WhenNoTokensAvailable()
    {
        using var bucket = new TokenBucket(1, TimeSpan.FromHours(1));

        // Consume the only token
        await bucket.WaitAsync(CancellationToken.None);

        var eventFired = false;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bucket.WaitingStartedEvent += (_, _) =>
        {
            eventFired = true;
            tcs.TrySetResult(true);
        };

        // Start a wait that will block (no tokens left, refill is far away)
        using var waitCts = new CancellationTokenSource();
        var waitTask = Task.Run(async () =>
        {
            try { await bucket.WaitAsync(waitCts.Token); }
            catch (OperationCanceledException) { /* expected */ }
        });

        // Wait for the event to fire (or timeout after 3 seconds)
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        waitCts.Cancel(); // unblock the waiting task
        await waitTask;

        completedTask.Should().Be(tcs.Task, because: "WaitingStartedEvent should fire when blocked");
        eventFired.Should().BeTrue();
    }

    // ─────────────── Dispose ───────────────

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        var bucket = new TokenBucket(1, TimeSpan.FromHours(1));

        var act = () =>
        {
            bucket.Dispose();
            bucket.Dispose();
            bucket.Dispose();
        };

        act.Should().NotThrow();
    }
}
