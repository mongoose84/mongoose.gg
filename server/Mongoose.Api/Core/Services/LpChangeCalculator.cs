using Mongoose.Api.Core.ValueObjects;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Works out the LP a ranked match gained or lost from the rank recorded after it and after the
/// player's previous match in the same queue.
/// </summary>
/// <remarks>
/// Sync records LP only on the newest ranked match at the time it runs, so a match gets a change
/// only when the match right before it in the same queue also has its LP. Promotions and demotions
/// are resolved on one ladder: 100 LP per division from Iron IV up to Diamond I, then Master,
/// Grandmaster and Challenger share one LP count above it.
/// </remarks>
public static class LpChangeCalculator
{
    // A single ranked match never moves this far; a bigger jump means the recorded LP is stale
    // (read before Riot applied the match) or spans matches we don't have.
    public const int MaxPlausibleChange = 100;

    private const int LpPerDivision = 100;
    private const int DivisionsPerTier = 4;

    private static readonly string[] TiersWithDivisions =
        ["IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "EMERALD", "DIAMOND"];

    private static readonly string[] ApexTiers = ["MASTER", "GRANDMASTER", "CHALLENGER"];

    private static readonly string[] Divisions = ["IV", "III", "II", "I"];

    /// <summary>
    /// The LP change for a ranked match, or null when it can't be told: a side has no recorded
    /// rank, the jump is implausible, or its sign contradicts the result (a win that lost LP, or a
    /// loss that gained LP, means one of the readings is stale).
    /// </summary>
    public static int? Compute(RankSnapshot? before, RankSnapshot? after, bool win)
    {
        var start = LadderScore(before);
        var end = LadderScore(after);
        if (start is null || end is null) return null;

        var change = end.Value - start.Value;
        if (Math.Abs(change) > MaxPlausibleChange) return null;
        if (win && change <= 0) return null;
        if (!win && change > 0) return null;

        return change;
    }

    /// <summary>
    /// The position on one continuous ladder (Iron IV 0 LP = 0), or null when the rank is incomplete
    /// or unknown.
    /// </summary>
    public static int? LadderScore(RankSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Lp is null || string.IsNullOrWhiteSpace(snapshot.Tier)) return null;

        var tier = snapshot.Tier.Trim().ToUpperInvariant();
        var lp = snapshot.Lp.Value;
        if (lp < 0) return null;

        if (Array.IndexOf(ApexTiers, tier) >= 0)
        {
            return TiersWithDivisions.Length * DivisionsPerTier * LpPerDivision + lp;
        }

        var tierIndex = Array.IndexOf(TiersWithDivisions, tier);
        var divisionIndex = Array.IndexOf(Divisions, snapshot.Division?.Trim().ToUpperInvariant());
        if (tierIndex < 0 || divisionIndex < 0) return null;

        return (tierIndex * DivisionsPerTier + divisionIndex) * LpPerDivision + lp;
    }
}
