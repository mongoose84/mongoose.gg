using FluentAssertions;
using Mongoose.Api.Infrastructure.Jobs;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for the wake-up signal that lets the sync endpoint start the background
/// job immediately. Verifies notify-before-wait, notify-after-wait, and coalescing.
/// </summary>
public class SyncQueueSignalTests
{
    [Fact]
    public async Task WaitAsync_CompletesImmediately_WhenNotifiedBeforeWaiting()
    {
        var signal = new SyncQueueSignal();
        signal.Notify();

        var wait = signal.WaitAsync(CancellationToken.None);

        (await Task.WhenAny(wait, Task.Delay(1000))).Should().Be(wait);
        await wait; // does not throw
    }

    [Fact]
    public async Task WaitAsync_Completes_WhenNotifiedWhileWaiting()
    {
        var signal = new SyncQueueSignal();
        var wait = signal.WaitAsync(CancellationToken.None);
        wait.IsCompleted.Should().BeFalse();

        signal.Notify();

        (await Task.WhenAny(wait, Task.Delay(1000))).Should().Be(wait);
    }

    [Fact]
    public async Task Notify_IsCoalesced_IntoASingleWakeUp()
    {
        var signal = new SyncQueueSignal();

        // A burst of notifications (e.g. several accounts queued) should release at most one permit.
        signal.Notify();
        signal.Notify();
        signal.Notify();

        var first = signal.WaitAsync(CancellationToken.None);
        (await Task.WhenAny(first, Task.Delay(1000))).Should().Be(first);

        // No second permit is pending — the job drains all work in one pass anyway.
        var second = signal.WaitAsync(CancellationToken.None);
        second.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task WaitAsync_Throws_WhenCancelled()
    {
        var signal = new SyncQueueSignal();
        using var cts = new CancellationTokenSource();
        var wait = signal.WaitAsync(cts.Token);

        cts.Cancel();

        await FluentActions.Awaiting(() => wait).Should().ThrowAsync<OperationCanceledException>();
    }
}
