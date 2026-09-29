using Mongoose.Api.Core.ValueObjects;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>A place on the ladder: tier, division (null for Master and above) and LP.</summary>
public sealed record LadderRank(string Tier, string? Division, int Lp);

/// <summary>
/// One continuous LP ladder (features/solo-trends.spec.md FR 10): Iron IV 0 LP = 0, 100 LP per
/// division through Diamond I, then Master, Grandmaster and Challenger share one LP count from the
/// Master floor. Shared by <see cref="LpChangeCalculator"/> and the climb card. Pure and static.
/// </summary>
public static class LpLadder
{
    public const int LpPerDivision = 100;
    private const int DivisionsPerTier = 4;

    private static readonly string[] TiersWithDivisions =
        ["IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "EMERALD", "DIAMOND"];

    private static readonly string[] ApexTiers = ["MASTER", "GRANDMASTER", "CHALLENGER"];

    private static readonly string[] Divisions = ["IV", "III", "II", "I"];

    /// <summary>The Master floor: Diamond I 100 LP.</summary>
    public const int ApexFloor = 7 * DivisionsPerTier * LpPerDivision;

    /// <summary>
    /// The position on the ladder, or null when the rank is incomplete or unknown.
    /// </summary>
    public static int? Score(RankSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Lp is null || string.IsNullOrWhiteSpace(snapshot.Tier)) return null;

        var tier = snapshot.Tier.Trim().ToUpperInvariant();
        var lp = snapshot.Lp.Value;
        if (lp < 0) return null;

        if (IsApex(tier)) return ApexFloor + lp;

        var tierIndex = Array.IndexOf(TiersWithDivisions, tier);
        var divisionIndex = Array.IndexOf(Divisions, snapshot.Division?.Trim().ToUpperInvariant());
        if (tierIndex < 0 || divisionIndex < 0) return null;

        return (tierIndex * DivisionsPerTier + divisionIndex) * LpPerDivision + lp;
    }

    public static int? Score(string? tier, string? division, int? lp) => Score(new RankSnapshot(tier, division, lp));

    /// <summary>
    /// The rank at a ladder score. Above the Master floor the tier can't be told from the score
    /// alone (Grandmaster and Challenger are cut-offs, not LP bands), so it is reported as Master.
    /// </summary>
    public static LadderRank FromScore(int score)
    {
        if (score >= ApexFloor) return new LadderRank(ApexTiers[0], null, score - ApexFloor);

        var clamped = Math.Max(score, 0);
        var divisionNumber = clamped / LpPerDivision;
        return new LadderRank(
            TiersWithDivisions[divisionNumber / DivisionsPerTier],
            Divisions[divisionNumber % DivisionsPerTier],
            clamped % LpPerDivision);
    }

    public static bool IsApex(string? tier)
        => tier is not null && Array.IndexOf(ApexTiers, tier.Trim().ToUpperInvariant()) >= 0;
}
