namespace Mongoose.Api.Tests;

/// <summary>
/// Serializes access to <c>Secrets</c>, which is process-wide static state. Every
/// <see cref="TestWebApplicationFactory"/> boot re-initializes it (Program reads configuration before
/// the factory's in-memory settings apply, so the API key comes back empty), so tests that set
/// <c>Secrets</c> and then read it hold this gate for their whole run, and host boots wait for it.
/// </summary>
internal static class SecretsTestGate
{
    public static readonly SemaphoreSlim Gate = new(1, 1);
}
