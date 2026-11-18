using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class StatTrendCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private static readonly IReadOnlyList<SoloMatchRow> NoSeason = Array.Empty<SoloMatchRow>();

    /// <summary>20 rows whose first 10 take <paramref name="first"/> and last 10 take <paramref name="last"/>.</summary>
    private static List<SoloMatchRow> Halves(Func<int, SoloMatchRow> first, Func<int, SoloMatchRow> last)
        => SoloRows.Many(20, i => i < 10 ? first(i) : last(i));

    private static StatTrend Trend(string key, IReadOnlyList<SoloMatchRow> rows, IReadOnlyList<SoloMatchRow>? season = null)
        => StatTrendCalculator.Calculate(SoloStats.Get(key), rows, season ?? NoSeason);

    // ───────────────────────── FR13: the six stats ─────────────────────────

    [Fact]
    public void Calculate_ReturnsSixStats_InFr13Order()
    {
        var trends = StatTrendCalculator.Calculate(SoloRows.Many(5), NoSeason);

        trends.Select(t => t.Key).Should().Equal(
            "deaths", "goldLeadAt15", "dragonParticipation", "visionPerMin", "csPerMin", "killParticipation");
    }

    [Fact]
    public void Calculate_ExcludesGoldLead_WhenMatchEndedBefore15Minutes()
    {
        var rows = new[] { SoloRows.Make(0, durationSec: 899, goldDiffAt15: 300), SoloRows.Make(1, durationSec: 900, goldDiffAt15: 300) };

        Trend(SoloStats.GoldLeadAt15, rows).Values.Should().Equal(null, 300);
    }

    [Fact]
    public void Calculate_ExcludesDragonParticipation_WhenTeamTookNoDragon()
    {
        var rows = new[] { SoloRows.Make(0, dragonsParticipated: 0, teamDragons: 0), SoloRows.Make(1, dragonsParticipated: 1, teamDragons: 4) };

        Trend(SoloStats.DragonParticipation, rows).Values.Should().Equal(null, 25);
    }

    [Fact]
    public void Calculate_ExcludesCs_ForSupport()
    {
        var rows = new[] { SoloRows.Make(0, role: "UTILITY"), SoloRows.Make(1, creepScore: 240, durationSec: 1800) };

        Trend(SoloStats.CsPerMin, rows).Values.Should().Equal(null, 8);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public void Calculate_ExcludesKillParticipation_WhenTeamHadFewerThanFiveKills(int teamKills, bool counted)
    {
        var rows = new[] { SoloRows.Make(0, teamKills: teamKills, killParticipationPct: 60) };

        Trend(SoloStats.KillParticipation, rows).Count.Should().Be(counted ? 1 : 0);
    }

    // ───────────────────────── FR14: series ─────────────────────────

    [Fact]
    public void Calculate_SkipsNulls_InRollingAverageAndCount()
    {
        // Every fifth match has no dragon participation: indexes 0, 5, 10, 15, 20 are null.
        var rows = SoloRows.Many(25, i => i % 5 == 0
            ? SoloRows.Make(i, teamDragons: 0)
            : SoloRows.Make(i, dragonsParticipated: 1, teamDragons: 2));

        var trend = Trend(SoloStats.DragonParticipation, rows);

        trend.Count.Should().Be(20);
        trend.Values.Should().HaveCount(25);
        trend.Values![0].Should().BeNull();
        trend.Values[1].Should().Be(50);
        // The 10th non-null value is at index 12, so the rolling line starts there.
        trend.Rolling.First().Index.Should().Be(12);
        trend.Rolling.Should().OnlyContain(p => p.Value == 50);
        trend.Rolling.Should().HaveCount(11);
    }

    [Fact]
    public void Calculate_WasAndNow_UseFirstAndLastTenNonNullValues()
    {
        // Nulls in the first half push the 10th non-null value into the second half.
        var rows = SoloRows.Many(24, i => i < 4
            ? SoloRows.Make(i, visionPerMin: null)
            : SoloRows.Make(i, visionPerMin: i < 14 ? 1.0 : 2.0));

        var trend = Trend(SoloStats.VisionPerMin, rows);

        trend.Count.Should().Be(20);
        trend.Was.Should().Be(1.0);
        trend.Now.Should().Be(2.0);
    }

    [Fact]
    public void Calculate_SamplesRollingLineAndDropsValues_Above100Matches()
    {
        var rows = SoloRows.Many(150, i => SoloRows.Make(i, deaths: i % 7));

        var trend = Trend(SoloStats.Deaths, rows);

        trend.Values.Should().BeNull();
        trend.Rolling.Should().HaveCount(100);
        trend.Rolling.First().Index.Should().Be(9);
        trend.Rolling.Last().Index.Should().Be(149);
    }

    [Fact]
    public void Calculate_KeepsValues_AtExactly100Matches()
    {
        var trend = Trend(SoloStats.Deaths, SoloRows.Many(100));

        trend.Values.Should().HaveCount(100);
        trend.Rolling.Should().HaveCount(91);
    }

    // ───────────────────────── FR15: verdict ─────────────────────────

    [Fact]
    public void Calculate_NoVerdict_WithFewerThan20Values()
    {
        var rows = SoloRows.Many(19, i => SoloRows.Make(i, deaths: i < 10 ? 8 : 2));

        var trend = Trend(SoloStats.Deaths, rows);

        trend.Verdict.Should().BeNull();
        trend.Was.Should().BeNull();
        trend.NormalizedChange.Should().BeNull();
        trend.Now.Should().NotBeNull();
    }

    [Fact]
    public void Calculate_FewerDeaths_IsImproving()
    {
        var rows = Halves(i => SoloRows.Make(i, deaths: 6), i => SoloRows.Make(i, deaths: 4));

        var trend = Trend(SoloStats.Deaths, rows);

        trend.Was.Should().Be(6);
        trend.Now.Should().Be(4);
        trend.Verdict.Should().Be(StatTrendCalculator.Improving);
        trend.NormalizedChange.Should().Be(4);
    }

    [Fact]
    public void Calculate_MoreDeaths_IsSlipping()
    {
        var rows = Halves(i => SoloRows.Make(i, deaths: 4), i => SoloRows.Make(i, deaths: 6));

        var trend = Trend(SoloStats.Deaths, rows);

        trend.Verdict.Should().Be(StatTrendCalculator.Slipping);
        trend.NormalizedChange.Should().Be(-4);
    }

    [Fact]
    public void Calculate_DeathsChangeAtThreshold_IsImproving()
    {
        // Last ten average 4.5, a drop of exactly the 0.5 threshold.
        var rows = Halves(i => SoloRows.Make(i, deaths: 5), i => SoloRows.Make(i, deaths: i % 2 == 0 ? 4 : 5));

        Trend(SoloStats.Deaths, rows).Verdict.Should().Be(StatTrendCalculator.Improving);
    }

    [Fact]
    public void Calculate_DeathsChangeUnderThreshold_IsSteady()
    {
        // Last ten average 4.6, a drop of 0.4.
        var rows = Halves(i => SoloRows.Make(i, deaths: 5), i => SoloRows.Make(i, deaths: i < 14 ? 4 : 5));

        Trend(SoloStats.Deaths, rows).Verdict.Should().Be(StatTrendCalculator.Steady);
    }

    [Theory]
    [InlineData(0.9, 1.0, StatTrendCalculator.Improving)]
    [InlineData(1.0, 1.1, StatTrendCalculator.Improving)]
    [InlineData(1.0, 0.9, StatTrendCalculator.Slipping)]
    [InlineData(0.7, 0.8, StatTrendCalculator.Improving)]
    [InlineData(1.0, 1.09, StatTrendCalculator.Steady)]
    public void Calculate_VisionChangeOfExactlyTheThreshold_IsNotSteady(double was, double now, string expected)
    {
        // 0.1 is not exact in binary; 1.0 − 0.9 must still count as a change of 0.1.
        var rows = Halves(i => SoloRows.Make(i, visionPerMin: was), i => SoloRows.Make(i, visionPerMin: now));

        Trend(SoloStats.VisionPerMin, rows).Verdict.Should().Be(expected);
    }

    [Fact]
    public void Calculate_HigherCs_IsImproving()
    {
        var rows = Halves(i => SoloRows.Make(i, creepScore: 180), i => SoloRows.Make(i, creepScore: 210));

        var trend = Trend(SoloStats.CsPerMin, rows);

        trend.Was.Should().Be(6);
        trend.Now.Should().Be(7);
        trend.Verdict.Should().Be(StatTrendCalculator.Improving);
    }

    // ───────────────────────── FR16: benchmark ─────────────────────────

    [Fact]
    public void Calculate_Benchmark_IsSeasonAverage()
    {
        var season = SoloRows.Many(20, i => SoloRows.Make(i, deaths: i % 2 == 0 ? 2 : 5));

        var trend = Trend(SoloStats.Deaths, SoloRows.Many(5), season);

        trend.Benchmark.Should().Be(new StatBenchmark("season", 3.5));
    }

    [Fact]
    public void Calculate_Benchmark_IsNull_WithFewerThan20SeasonValues()
    {
        var season = SoloRows.Many(25, i => SoloRows.Make(i, visionPerMin: i < 6 ? null : 1.0));

        Trend(SoloStats.VisionPerMin, SoloRows.Many(5), season).Benchmark.Should().BeNull();
    }

    [Fact]
    public void Calculate_Benchmark_IsTheRankAverage_WhenARankPoolIsGiven()
    {
        var season = SoloRows.Many(20, i => SoloRows.Make(i, deaths: 9));
        var pool = new RankBenchmarkPool("EMERALD", SoloRows.Many(200, i => SoloRows.Make(i, deaths: i % 2 == 0 ? 4 : 6)));

        var trend = StatTrendCalculator.Calculate(SoloStats.Get(SoloStats.Deaths), SoloRows.Many(5), season, pool);

        trend.Benchmark.Should().Be(new StatBenchmark("rank", 5, "EMERALD"));
    }

    [Fact]
    public void Calculate_RankBenchmark_IsNull_WhenMostPoolRowsLackTheStat()
    {
        // 99 of 200 rows have a gold lead at 15: under half the pool's minimum
        var pool = new RankBenchmarkPool("EMERALD", SoloRows.Many(200, i => SoloRows.Make(i, goldDiffAt15: i < 99 ? 300 : null)));

        StatTrendCalculator.Calculate(SoloStats.Get(SoloStats.GoldLeadAt15), SoloRows.Many(5), NoSeason, pool)
            .Benchmark.Should().BeNull();
    }
}
