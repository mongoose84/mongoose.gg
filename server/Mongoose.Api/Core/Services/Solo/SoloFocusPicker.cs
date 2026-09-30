using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>The "Your focus" card's data (FR19). <see cref="Last20"/> holds "hit", "miss" or null per match, oldest first.</summary>
public sealed record SoloFocus(
    string Stat,
    string Factor,
    double? Mark,
    double? Was,
    double? Now,
    int HitWinRate,
    int MissWinRate,
    IReadOnlyList<string?> Last20,
    int Hits);

/// <summary>
/// Picks the one stat to work on (features/solo-trends.spec.md FR18–FR19). Pure and static.
/// </summary>
public static class SoloFocusPicker
{
    public const string Hit = "hit";
    public const string Miss = "miss";

    // FR18: with fewer than 20 matches no focus is honest.
    public const int MinMatches = 20;

    // FR18: without a slipping stat, choose among the three biggest gaps.
    private const int TopFactorsWithoutSlipping = 3;

    private const int StripLength = 20;

    public static SoloFocus? Pick(IReadOnlyList<SoloMatchRow> rows, IReadOnlyList<StatTrend> trends, IReadOnlyList<WinFactor> factors)
    {
        if (rows.Count < MinMatches || factors.Count == 0) return null;

        var candidates = SoloStats.All
            .Where(s => s.FactorKey != null)
            .Select(s => (Stat: s, Factor: factors.FirstOrDefault(f => f.Key == s.FactorKey), Trend: trends.FirstOrDefault(t => t.Key == s.Key)))
            .Where(c => c.Factor != null && c.Trend != null)
            .ToList();
        if (candidates.Count == 0) return null;

        // FR18.1: the slipping candidate with the largest gap. Candidates are in stat order and the
        // sort is stable, so ties keep that order (FR18.3).
        var chosen = candidates
            .Where(c => c.Trend!.Verdict == StatTrendCalculator.Slipping)
            .OrderByDescending(c => c.Factor!.Gap)
            .FirstOrDefault();

        // FR18.2: otherwise the lowest hit rate among the three largest gaps.
        if (chosen.Stat == null)
        {
            var topKeys = factors.Take(TopFactorsWithoutSlipping).Select(f => f.Key).ToHashSet();
            chosen = candidates
                .Where(c => topKeys.Contains(c.Factor!.Key))
                .OrderBy(c => (double)c.Factor!.HitMatches / (c.Factor.HitMatches + c.Factor.MissMatches))
                .FirstOrDefault();
        }

        if (chosen.Stat == null) return null;

        var factorKey = chosen.Factor!.Key;
        var last = rows.TakeLast(StripLength).ToList();
        var strip = last
            .Select(r => WinFactorCalculator.Evaluate(factorKey, r) switch
            {
                true => Hit,
                false => Miss,
                null => (string?)null
            })
            .ToList();

        return new SoloFocus(
            chosen.Stat.Key,
            factorKey,
            MostCommonRoleMark(factorKey, last),
            chosen.Trend!.Was,
            chosen.Trend.Now,
            chosen.Factor.HitWinRate,
            chosen.Factor.MissWinRate,
            strip,
            strip.Count(s => s == Hit));
    }

    /// <summary>The mark for the role played most in the strip, for role-dependent factors.</summary>
    private static double? MostCommonRoleMark(string factorKey, IReadOnlyList<SoloMatchRow> rows)
    {
        var role = rows
            .Where(r => WinFactorCalculator.Evaluate(factorKey, r).HasValue)
            .GroupBy(r => r.Role)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        return role == null ? null : WinFactorCalculator.Mark(factorKey, role);
    }
}
