using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>A point of the rolling average; <see cref="Index"/> is the match's position in the range.</summary>
public sealed record RollingPoint(int Index, double Value);

/// <summary>The dashed line on a trend tile (FR16). Kind "season" until rank averages exist (5g).</summary>
public sealed record StatBenchmark(string Kind, double Value);

/// <summary>One stat's trend over the range (FR14–FR16).</summary>
public sealed record StatTrend(
    string Key,
    IReadOnlyList<double?>? Values,
    IReadOnlyList<RollingPoint> Rolling,
    double? Was,
    double? Now,
    int Count,
    string? Verdict,
    double? NormalizedChange,
    StatBenchmark? Benchmark);

/// <summary>
/// Rolling averages, was / now and the Improving / Slipping / Steady verdict per stat
/// (features/solo-trends.spec.md FR13–FR16). Pure and static.
/// </summary>
public static class StatTrendCalculator
{
    public const string Improving = "improving";
    public const string Slipping = "slipping";
    public const string Steady = "steady";

    // FR14: a 10-match rolling average; was = first 10 values, now = last 10.
    public const int Window = 10;

    // FR15: with fewer than 20 values was and now would overlap, so no verdict.
    public const int MinValuesForVerdict = 20;

    // FR14: a Season range with more than 100 matches is sampled to 100 points, without dots.
    public const int MaxPoints = 100;

    // FR16: the season average needs 20 values to be a fair benchmark.
    public const int MinValuesForBenchmark = 20;

    public static IReadOnlyList<StatTrend> Calculate(IReadOnlyList<SoloMatchRow> rows, IReadOnlyList<SoloMatchRow> seasonRows)
        => SoloStats.All.Select(stat => Calculate(stat, rows, seasonRows)).ToList();

    public static StatTrend Calculate(SoloStatDefinition stat, IReadOnlyList<SoloMatchRow> rows, IReadOnlyList<SoloMatchRow> seasonRows)
    {
        var values = rows.Select(stat.Value).ToList();
        var present = values
            .Select((value, index) => (value, index))
            .Where(v => v.value.HasValue)
            .Select(v => (Value: v.value!.Value, v.index))
            .ToList();

        var rolling = new List<RollingPoint>();
        for (var i = Window - 1; i < present.Count; i++)
        {
            var average = present.Skip(i - Window + 1).Take(Window).Average(v => v.Value);
            rolling.Add(new RollingPoint(present[i].index, Round(average)));
        }

        double? now = present.Count == 0 ? null : Round(present.TakeLast(Window).Average(v => v.Value));
        double? was = present.Count >= MinValuesForVerdict ? Round(present.Take(Window).Average(v => v.Value)) : null;

        string? verdict = null;
        double? normalizedChange = null;
        if (was.HasValue && now.HasValue)
        {
            var sign = stat.Direction == StatDirection.LowerIsBetter ? -1 : 1;
            // was and now carry 2 decimals; rounding the change keeps 1.0 − 0.9 at the 0.1 threshold.
            var change = Round((now.Value - was.Value) * sign);
            normalizedChange = Math.Round(change / stat.SteadyThreshold, 2);
            verdict = Math.Abs(change) < stat.SteadyThreshold ? Steady : change > 0 ? Improving : Slipping;
        }

        var sampled = rows.Count > MaxPoints;
        return new StatTrend(
            stat.Key,
            sampled ? null : values.Select(v => v.HasValue ? Round(v.Value) : (double?)null).ToList(),
            sampled ? Sample(rolling, MaxPoints) : rolling,
            was,
            now,
            present.Count,
            verdict,
            normalizedChange,
            Benchmark(stat, seasonRows));
    }

    private static StatBenchmark? Benchmark(SoloStatDefinition stat, IReadOnlyList<SoloMatchRow> seasonRows)
    {
        var seasonValues = seasonRows.Select(stat.Value).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return seasonValues.Count >= MinValuesForBenchmark
            ? new StatBenchmark("season", Round(seasonValues.Average()))
            : null;
    }

    /// <summary>Evenly spaced points that always keep the first and the last.</summary>
    internal static IReadOnlyList<RollingPoint> Sample(IReadOnlyList<RollingPoint> points, int max)
    {
        if (points.Count <= max) return points;

        var result = new List<RollingPoint>(max);
        for (var i = 0; i < max; i++)
        {
            var index = (int)Math.Round(i * (points.Count - 1) / (double)(max - 1));
            result.Add(points[index]);
        }
        return result;
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
