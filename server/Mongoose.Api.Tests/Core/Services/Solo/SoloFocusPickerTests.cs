using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloFocusPickerTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private static SoloFocus? Pick(IReadOnlyList<SoloMatchRow> rows)
    {
        var trends = StatTrendCalculator.Calculate(rows, Array.Empty<SoloMatchRow>());
        var factors = WinFactorCalculator.Calculate(rows);
        return SoloFocusPicker.Pick(rows, trends, factors);
    }

    /// <summary>
    /// 20 matches, 10 wins. Vision drops from 1.2 to 0.6 (Slipping) and hits its mark in the first 10:
    /// hit 60%, miss 40% (gap 20). Low deaths holds steady (4 on wins, 5 on losses) with a bigger gap (100).
    /// </summary>
    private static List<SoloMatchRow> VisionSlippingRows() => SoloRows.Many(20, i =>
    {
        var win = i < 10 ? i < 6 : i < 14;
        return SoloRows.Make(i, win: win, deaths: win ? 4 : 5, visionPerMin: i < 10 ? 1.2 : 0.6);
    });

    // ───────────────────────── FR18: minimums ─────────────────────────

    [Fact]
    public void Pick_ReturnsNull_WithFewerThan20Matches()
    {
        var rows = VisionSlippingRows().Take(19).ToList();

        Pick(rows).Should().BeNull();
    }

    [Fact]
    public void Pick_ReturnsNull_WithoutAnyWinFactorRow()
    {
        Pick(SoloRows.Many(30)).Should().BeNull();
    }

    // ───────────────────────── FR18.1: Slipping first ─────────────────────────

    [Fact]
    public void Pick_PrefersSlippingStat_OverLargerGap()
    {
        var focus = Pick(VisionSlippingRows());

        focus.Should().NotBeNull();
        focus!.Stat.Should().Be(SoloStats.VisionPerMin);
        focus.Factor.Should().Be(WinFactorCalculator.Vision);
        focus.Mark.Should().Be(0.9);
        focus.Was.Should().Be(1.2);
        focus.Now.Should().Be(0.6);
        focus.HitWinRate.Should().Be(60);
        focus.MissWinRate.Should().Be(40);
    }

    [Fact]
    public void Pick_Last20_IsHitOrMissPerMatch_OldestFirst()
    {
        var focus = Pick(VisionSlippingRows())!;

        focus.Last20.Should().HaveCount(20);
        focus.Last20.Take(10).Should().OnlyContain(s => s == SoloFocusPicker.Hit);
        focus.Last20.Skip(10).Should().OnlyContain(s => s == SoloFocusPicker.Miss);
        focus.Hits.Should().Be(10);
    }

    [Fact]
    public void Pick_Last20_IsNull_WhereStatDoesNotApply()
    {
        // CS is the only factor (misses on the first 10) and slips; the two support matches have no CS.
        var rows = SoloRows.Many(22, i => i >= 20
            ? SoloRows.Make(i, role: "UTILITY", visionPerMin: 2.5)
            : SoloRows.Make(i, win: i % 2 == 0 || i < 10, creepScore: i < 10 ? 270 : 150));

        var focus = Pick(rows)!;

        focus.Stat.Should().Be(SoloStats.CsPerMin);
        focus.Last20.Should().HaveCount(20);
        focus.Last20.TakeLast(2).Should().Equal(null, null);
        focus.Hits.Should().Be(8); // matches 2–9 of the 22
        focus.Mark.Should().Be(7.0);
    }

    // ───────────────────────── FR18.2 / FR18.3: fallback and ties ─────────────────────────

    [Fact]
    public void Pick_WithoutSlipping_TakesLowestHitRateAmongLargestGaps()
    {
        // Nothing slips. Low deaths misses 5 of 20 (hit rate 75%); CS hits only 5 of 20 (hit rate 25%).
        var rows = SoloRows.Many(20, i => SoloRows.Make(
            i,
            win: i % 4 is 1 or 2,
            deaths: i % 4 == 0 ? 5 : 4,
            creepScore: i % 4 == 1 ? 225 : 195));

        var focus = Pick(rows)!;

        focus.Stat.Should().Be(SoloStats.CsPerMin);
        focus.Factor.Should().Be(WinFactorCalculator.Cs);
        focus.Mark.Should().Be(7.0);
        focus.Hits.Should().Be(5);
    }

    [Fact]
    public void Pick_Ties_FollowFr13Order()
    {
        // Low deaths and CS miss on the same matches: same gap, same hit rate. Deaths comes first in FR13.
        var rows = SoloRows.Many(20, i => SoloRows.Make(
            i,
            win: i % 4 != 0,
            deaths: i % 4 == 0 ? 5 : 4,
            creepScore: i % 4 == 0 ? 195 : 225));

        Pick(rows)!.Stat.Should().Be(SoloStats.Deaths);
    }

    [Fact]
    public void Pick_Mark_FollowsMostCommonRole()
    {
        // Vision is the only factor; 12 of the last 20 are support games, so the mark is the support one.
        var rows = SoloRows.Many(20, i => i < 12
            ? SoloRows.Make(i, role: "UTILITY", win: i < 6, visionPerMin: i < 6 ? 2.2 : 1.5)
            : SoloRows.Make(i, win: i % 2 == 0, visionPerMin: 1.0));

        Pick(rows)!.Mark.Should().Be(2.0);
    }
}
