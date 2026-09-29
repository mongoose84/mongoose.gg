using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Services;

/// <summary>What a new rank reading means compared with the previous stored snapshot.</summary>
public enum RankReadingOutcome
{
    /// <summary>Nothing changed: no row is stored.</summary>
    Unchanged,

    /// <summary>The rank moved without a match (decay, dodge): stored as a comparison point.</summary>
    NoMatch,

    /// <summary>Exactly one match ended in between: stored and attributed to that match.</summary>
    Pending,

    /// <summary>No match can be told apart (first reading, several matches, season reset).</summary>
    Baseline
}

public enum MatchAttributionOutcome
{
    /// <summary>The match isn't stored yet (or never will be): try again later.</summary>
    NoCandidate,
    Single,
    Ambiguous
}

/// <summary>
/// Domain rules for rank snapshots: when a reading belongs to exactly one ranked match, and which
/// stored match that is. A wrong attribution would show a wrong LP change, so every uncertain case
/// attributes nothing.
/// </summary>
public static class RankSnapshotRules
{
    public static RankReadingOutcome Classify(RankSnapshotRecord? previous, LeagueRankReading current)
    {
        if (previous is null) return RankReadingOutcome.Baseline;

        var gamesPlayed = current.Games - previous.Games;
        if (gamesPlayed == 1) return RankReadingOutcome.Pending;
        if (gamesPlayed != 0) return RankReadingOutcome.Baseline;

        var rankMoved = !string.Equals(previous.Tier, current.Tier, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(previous.Division, current.Division, StringComparison.OrdinalIgnoreCase)
            || previous.Lp != current.Lp;
        return rankMoved ? RankReadingOutcome.NoMatch : RankReadingOutcome.Unchanged;
    }

    /// <summary>
    /// The window a pending snapshot's match must have ended in, in epoch milliseconds: from the
    /// previous reading to this one, widened by the clock skew on both sides.
    /// </summary>
    public static (long FromMs, long ToMs) AttributionWindow(DateTime previousCapturedAtUtc, DateTime capturedAtUtc, TimeSpan clockSkew)
    {
        var from = new DateTimeOffset(DateTime.SpecifyKind(previousCapturedAtUtc, DateTimeKind.Utc)) - clockSkew;
        var to = new DateTimeOffset(DateTime.SpecifyKind(capturedAtUtc, DateTimeKind.Utc)) + clockSkew;
        return (from.ToUnixTimeMilliseconds(), to.ToUnixTimeMilliseconds());
    }

    /// <summary>The one stored match that ended in the window, or why there isn't one.</summary>
    public static (MatchAttributionOutcome Outcome, string? MatchId) ChooseMatch(IReadOnlyCollection<string> candidateMatchIds)
    {
        return candidateMatchIds.Count switch
        {
            0 => (MatchAttributionOutcome.NoCandidate, null),
            1 => (MatchAttributionOutcome.Single, candidateMatchIds.First()),
            _ => (MatchAttributionOutcome.Ambiguous, null)
        };
    }

    /// <summary>The ranked queue id for a League-v4 queue type, or null for anything else.</summary>
    public static int? QueueIdFor(string? leagueQueueType) => leagueQueueType switch
    {
        "RANKED_SOLO_5x5" => 420,
        "RANKED_FLEX_SR" => 440,
        _ => null
    };
}
