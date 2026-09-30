namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Where the backfill stands for one account (FR 39): <c>running</c>, <c>waiting</c> (on Riot's rate
/// limit until <see cref="RetryAt"/>) or <c>queued</c> (behind a match sync), with matches done of total.
/// </summary>
public sealed record DeathDetailBackfillProgress(string Status, int Done, int Total, DateTime? RetryAt = null)
{
    public const string Running = "running";
    public const string Waiting = "waiting";
    public const string Queued = "queued";
    public const string Complete = "done";
}

/// <summary>
/// The death detail backfill's live state, shared by the job and the death-zones endpoint.
/// </summary>
public interface IDeathDetailBackfillState
{
    /// <summary>The account's progress while the job works on it; null otherwise.</summary>
    DeathDetailBackfillProgress? Get(string puuid);

    /// <summary>Moves the account to the front of the backfill (the player is looking at the Solo page).</summary>
    void Prioritize(string puuid);
}
