namespace Mongoose.Api.Core.Entities;

/// <summary>
/// One reading of a player's rank in one ranked queue (League-v4), stored so the LP after a
/// match can be attributed to that match. See .github/specs/features/rank-snapshots.spec.md.
/// </summary>
public sealed class RankSnapshotRecord
{
    public long Id { get; set; }
    public string Puuid { get; set; } = string.Empty;
    public int QueueId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string? Division { get; set; }
    public int Lp { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public DateTime CapturedAt { get; set; }

    /// <summary>For a pending snapshot: the previous snapshot's time, where its match window starts.</summary>
    public DateTime? WindowStartAt { get; set; }

    public string Source { get; set; } = RankSnapshotSource.Poll;
    public string Status { get; set; } = RankSnapshotStatus.Baseline;
    public string? MatchId { get; set; }

    public int Games => Wins + Losses;
}

/// <summary>A rank as League-v4 reports it for one ranked queue.</summary>
public sealed record LeagueRankReading(int QueueId, string Tier, string? Division, int Lp, int Wins, int Losses)
{
    public int Games => Wins + Losses;
}

public static class RankSnapshotStatus
{
    /// <summary>A starting point: no match can be attributed (first reading, several matches, season reset).</summary>
    public const string Baseline = "baseline";

    /// <summary>Exactly one ranked match ended since the previous reading; waiting for that match to be stored.</summary>
    public const string Pending = "pending";

    public const string Attributed = "attributed";

    /// <summary>Could not be attributed safely (two candidate matches, or none before the timeout).</summary>
    public const string Skipped = "skipped";

    /// <summary>The rank changed without a match (decay, dodge): kept as the next comparison point.</summary>
    public const string NoMatch = "no_match";
}

public static class RankSnapshotSource
{
    public const string Poll = "poll";
    public const string Sync = "sync";
    public const string Login = "login";
}
