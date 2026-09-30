using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>A win factor row: win rate when the player hit the mark and when they missed it (FR22).</summary>
public sealed record WinFactor(string Key, int HitWinRate, int MissWinRate, int HitMatches, int MissMatches, int Gap);

/// <summary>
/// Win rate when the player hits each mark vs when they miss it (features/solo-trends.spec.md FR21–FR23).
/// Pure and static.
/// </summary>
public static class WinFactorCalculator
{
    public const string AheadAt15 = "aheadAt15";
    public const string LowDeaths = "lowDeaths";
    public const string Dragons = "dragons";
    public const string Vision = "vision";
    public const string Cs = "cs";

    // FR22: a row needs 5 matches on each side before the win rates mean anything.
    public const int MinMatchesPerSide = 5;

    // FR21 marks.
    private const int MaxLowDeaths = 4;
    private const int MinDragons = 2;
    private const int DragonsMinDurationSec = 1200;
    private const double VisionMark = 0.9;
    private const double SupportVisionMark = 2.0;
    private const double CsMark = 7.0;
    private const double JungleCsMark = 5.5;

    /// <summary>FR21 table order; the tie-break order when gaps are equal.</summary>
    public static readonly IReadOnlyList<string> Order = [AheadAt15, LowDeaths, Dragons, Vision, Cs];

    /// <summary>True when the match hit the mark, false when it missed, null when the factor doesn't apply.</summary>
    public static bool? Evaluate(string key, SoloMatchRow r) => key switch
    {
        AheadAt15 => SoloStats.GoldLeadAt15Value(r) is { } gold ? gold > 0 : null,
        LowDeaths => r.Deaths <= MaxLowDeaths,
        Dragons => r.DurationSec >= DragonsMinDurationSec && r.DragonsParticipated.HasValue
            ? r.DragonsParticipated.Value >= MinDragons
            : null,
        Vision => r.VisionPerMin is { } vision ? vision >= Mark(Vision, r.Role)!.Value : null,
        Cs => SoloStats.CsPerMinValue(r) is { } cs ? cs >= Mark(Cs, r.Role)!.Value : null,
        _ => null
    };

    /// <summary>The role-dependent mark of the vision and CS factors; null for the fixed ones.</summary>
    public static double? Mark(string key, string role) => key switch
    {
        Vision => role == SoloStats.Utility ? SupportVisionMark : VisionMark,
        Cs => role == SoloStats.Jungle ? JungleCsMark : CsMark,
        _ => null
    };

    public static IReadOnlyList<WinFactor> Calculate(IReadOnlyList<SoloMatchRow> rows)
    {
        var factors = new List<WinFactor>();
        foreach (var key in Order)
        {
            var hit = new List<SoloMatchRow>();
            var miss = new List<SoloMatchRow>();
            foreach (var row in rows)
            {
                switch (Evaluate(key, row))
                {
                    case true: hit.Add(row); break;
                    case false: miss.Add(row); break;
                }
            }

            if (hit.Count < MinMatchesPerSide || miss.Count < MinMatchesPerSide) continue;

            var hitRate = SoloMath.WinRate(hit);
            var missRate = SoloMath.WinRate(miss);
            factors.Add(new WinFactor(key, hitRate, missRate, hit.Count, miss.Count, hitRate - missRate));
        }

        // FR22: largest gap first; OrderBy is stable, so equal gaps keep the FR21 order.
        return factors.OrderByDescending(f => f.Gap).ToList();
    }
}
