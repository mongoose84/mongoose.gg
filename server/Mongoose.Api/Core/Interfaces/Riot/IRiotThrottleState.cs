namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Read-only view of the Riot rate limiter (features/solo-trends.spec.md FR 40): whether a Riot
/// request has been waiting for a token for more than <see cref="WaitThreshold"/>, and until when.
/// Jobs use it to report "Waiting on Riot's servers"; it changes nothing about how requests are
/// throttled.
/// </summary>
public interface IRiotThrottleState
{
    /// <summary>How long a request waits before it counts as waiting on Riot.</summary>
    static readonly TimeSpan WaitThreshold = TimeSpan.FromSeconds(2);

    /// <summary>
    /// When the bucket a request has waited on for more than <see cref="WaitThreshold"/> refills
    /// (UTC); null while no request has waited that long. Clears once the request gets its token.
    /// </summary>
    DateTime? WaitingUntilUtc { get; }
}
