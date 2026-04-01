namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// TEMPORARY: Event arguments for rate limit wait events.
/// Contains the PUUID context to identify which account triggered the rate limit.
/// TODO: Remove this once we have a more sophisticated rate limiting UX.
/// </summary>
/// <param name="Puuid">The PUUID of the account that triggered the rate limit wait, or null if unknown.</param>
public class RateLimitWaitEventArgs(string? Puuid = null) : EventArgs
{
    /// <summary>
    /// The PUUID of the account that triggered the rate limit wait, or null if unknown.
    /// </summary>
    public string? Puuid { get; } = Puuid;
}
