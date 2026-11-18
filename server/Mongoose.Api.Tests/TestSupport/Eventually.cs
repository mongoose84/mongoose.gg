namespace Mongoose.Api.Tests;

/// <summary>
/// Waits for a condition driven by background work, instead of a fixed <c>Task.Delay</c> that
/// either wastes time or flakes on a slow runner. Returns as soon as the condition holds and
/// throws <see cref="TimeoutException"/> with <paramref name="because"/> if it never does.
/// </summary>
internal static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    public static async Task UntilAsync(Func<bool> condition, string because, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting until {because}.");
            }
            await Task.Delay(PollInterval);
        }
    }
}
