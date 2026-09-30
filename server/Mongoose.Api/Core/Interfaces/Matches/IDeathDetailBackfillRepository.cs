namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// One of an account's recent Summoner's Rift matches, newest first, and whether it already has
/// its death detail (or was skipped because Riot no longer serves its timeline).
/// </summary>
public sealed record BackfillMatch(string MatchId, bool Done);

/// <summary>
/// Reads and marks the work of <c>DeathDetailBackfillJob</c> (features/solo-trends.spec.md FR 38).
/// </summary>
public interface IDeathDetailBackfillRepository
{
    /// <summary>
    /// Accounts of users active since <paramref name="activeSinceUtc"/> whose backfill isn't done,
    /// the most recently active user first.
    /// </summary>
    Task<IList<string>> GetAccountsToBackfillAsync(DateTime activeSinceUtc, int limit);

    /// <summary>The account's last <paramref name="limit"/> Summoner's Rift matches, newest first.</summary>
    Task<IList<BackfillMatch>> GetRecentMatchesAsync(string puuid, int limit);

    /// <summary>Records a match whose timeline Riot no longer serves, so it isn't retried.</summary>
    Task MarkSkippedAsync(string matchId, string reason, DateTime skippedAtUtc);

    /// <summary>Sets <c>riot_accounts.death_detail_backfilled_at</c>.</summary>
    Task MarkAccountDoneAsync(string puuid, DateTime doneAtUtc);

    /// <summary>True while any account's match sync is queued or running.</summary>
    Task<bool> IsSyncActiveAsync();
}
