using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class WinFactorCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    /// <summary>
    /// <paramref name="hits"/> matches that hit the low-deaths mark and <paramref name="misses"/> that miss it,
    /// winning the first <paramref name="hitWins"/> and <paramref name="missWins"/> of each side.
    /// </summary>
    private static List<SoloMatchRow> LowDeathsRows(int hits, int hitWins, int misses, int missWins)
        => SoloRows.Many(hits + misses, i => i < hits
            ? SoloRows.Make(i, deaths: 2, win: i < hitWins)
            : SoloRows.Make(i, deaths: 8, win: i - hits < missWins));

    // ───────────────────────── FR21: marks ─────────────────────────

    [Theory]
    [InlineData("MIDDLE", 0.9, true)]
    [InlineData("MIDDLE", 0.89, false)]
    [InlineData("UTILITY", 1.9, false)]
    [InlineData("UTILITY", 2.0, true)]
    public void Evaluate_Vision_UsesSupportMark(string role, double visionPerMin, bool expected)
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.Vision, SoloRows.Make(0, role: role, visionPerMin: visionPerMin))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("BOTTOM", 210, true)]   // 7.0 per minute
    [InlineData("BOTTOM", 200, false)]
    [InlineData("JUNGLE", 165, true)]   // 5.5 per minute
    [InlineData("JUNGLE", 160, false)]
    public void Evaluate_Cs_UsesJungleMark(string role, int creepScore, bool expected)
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.Cs, SoloRows.Make(0, role: role, creepScore: creepScore, durationSec: 1800))
            .Should().Be(expected);
    }

    [Fact]
    public void Evaluate_Cs_DoesNotApplyToSupport()
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.Cs, SoloRows.Make(0, role: "UTILITY")).Should().BeNull();
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void Evaluate_LowDeaths_MarkIsFourOrFewer(int deaths, bool expected)
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.LowDeaths, SoloRows.Make(0, deaths: deaths)).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1800, true)]
    [InlineData(0, 1800, false)]
    [InlineData(-1, 1800, false)]
    [InlineData(500, 899, null)]
    public void Evaluate_AheadAt15_NeedsPositiveLeadAndFifteenMinutes(int goldDiff, int durationSec, bool? expected)
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.AheadAt15, SoloRows.Make(0, goldDiffAt15: goldDiff, durationSec: durationSec))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 1200, true)]
    [InlineData(1, 1200, false)]
    [InlineData(3, 1199, null)]
    public void Evaluate_Dragons_NeedsTwentyMinutes(int dragons, int durationSec, bool? expected)
    {
        WinFactorCalculator.Evaluate(WinFactorCalculator.Dragons, SoloRows.Make(0, dragonsParticipated: dragons, durationSec: durationSec))
            .Should().Be(expected);
    }

    [Fact]
    public void Mark_IsNull_ForFixedFactors()
    {
        WinFactorCalculator.Mark(WinFactorCalculator.LowDeaths, "MIDDLE").Should().BeNull();
        WinFactorCalculator.Mark(WinFactorCalculator.Vision, "UTILITY").Should().Be(2.0);
        WinFactorCalculator.Mark(WinFactorCalculator.Cs, "JUNGLE").Should().Be(5.5);
    }

    // ───────────────────────── FR22: rows ─────────────────────────

    [Fact]
    public void Calculate_ReturnsNoRows_WhenEveryMatchHitsEveryMark()
    {
        WinFactorCalculator.Calculate(SoloRows.Many(30)).Should().BeEmpty();
    }

    [Fact]
    public void Calculate_LeavesOutFactor_WithFewerThanFiveMatchesOnASide()
    {
        WinFactorCalculator.Calculate(LowDeathsRows(hits: 10, hitWins: 5, misses: 4, missWins: 0)).Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ReturnsWinRatesAndGap_WithFiveMatchesOnEachSide()
    {
        var factors = WinFactorCalculator.Calculate(LowDeathsRows(hits: 6, hitWins: 4, misses: 5, missWins: 1));

        factors.Should().ContainSingle().Which.Should().Be(new WinFactor(WinFactorCalculator.LowDeaths, 67, 20, 6, 5, 47));
    }

    [Fact]
    public void Calculate_SortsByGap_AndKeepsNegativeGaps()
    {
        // Low deaths: hit 80%, miss 20% (gap 60). Vision: hit 40%, miss 60% (gap −20).
        var rows = SoloRows.Many(10, i => SoloRows.Make(
            i,
            win: i is 0 or 1 or 2 or 3 or 5,
            deaths: i < 5 ? 2 : 8,
            visionPerMin: i % 2 == 0 ? 1.0 : 0.5));

        var factors = WinFactorCalculator.Calculate(rows);

        factors.Select(f => f.Key).Should().Equal(WinFactorCalculator.LowDeaths, WinFactorCalculator.Vision);
        factors[0].Gap.Should().Be(60);
        factors[1].Gap.Should().Be(-20);
    }

    [Fact]
    public void Calculate_EqualGaps_KeepFr21Order()
    {
        // Low deaths and CS miss on the same matches, so their gaps are equal; low deaths comes first in FR21.
        var rows = SoloRows.Many(10, i => SoloRows.Make(
            i,
            win: i < 5,
            deaths: i < 5 ? 2 : 8,
            creepScore: i < 5 ? 270 : 90));

        WinFactorCalculator.Calculate(rows).Select(f => f.Key)
            .Should().Equal(WinFactorCalculator.LowDeaths, WinFactorCalculator.Cs);
    }
}
