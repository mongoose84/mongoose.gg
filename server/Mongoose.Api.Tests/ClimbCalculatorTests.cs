using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class ClimbCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private static SoloMatchRow Ranked(int index, string tier, string? division, int lp, int? change, bool? win = null)
        => SoloRows.Make(index, win: win ?? change is null or > 0, tierAfter: tier, rankAfter: division, lpAfter: lp, lpChange: change);

    /// <summary>Loss / win matches with only an LP change (no rank), for the drop rule.</summary>
    private static SoloMatchRow Change(int index, int? change, bool win)
        => SoloRows.Make(index, win: win, lpChange: change);

    // ───────────────────────── FR 10: ladder and net LP ─────────────────────────

    [Fact]
    public void LpClimb_StartsBeforeTheFirstMatch_WhenItsChangeIsKnown()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "EMERALD", "III", 90, 20),
            Ranked(1, "EMERALD", "II", 10, 20),
            Ranked(2, "EMERALD", "II", 30, 20)])!;

        climb.Start.Should().Be(new LadderRank("EMERALD", "III", 70));
        climb.End.Should().Be(new LadderRank("EMERALD", "II", 30));
        climb.Net.Should().Be(60);
        climb.Points.Select(p => p.Ladder).Should().Equal(2190, 2210, 2230);
    }

    [Fact]
    public void LpClimb_StartsAtTheFirstMatch_WhenItsChangeIsUnknown()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "GOLD", "II", 40, null),
            Ranked(1, "GOLD", "II", 58, 18)])!;

        climb.Start.Should().Be(new LadderRank("GOLD", "II", 40));
        climb.Net.Should().Be(18);
    }

    [Fact]
    public void LpClimb_KeepsTheMatchIndex_AcrossMatchesWithoutLp()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "GOLD", "II", 40, null),
            SoloRows.Make(1),
            Ranked(2, "GOLD", "II", 60, null)])!;

        climb.Points.Select(p => p.Index).Should().Equal(0, 2);
    }

    [Fact]
    public void LpClimb_IsNull_WithoutAnyRank()
    {
        ClimbCalculator.LpClimb(SoloRows.Many(5)).Should().BeNull();
    }

    // ───────────────────────── FR 11: promotions and demotions ─────────────────────────

    [Fact]
    public void LpClimb_RecordsAPromotion_WhenTheLadderCrossesADivision()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "EMERALD", "III", 90, 20),
            Ranked(1, "EMERALD", "II", 10, 20)])!;

        climb.Events.Should().Equal(new LadderEvent(1, ClimbCalculator.Promotion, "EMERALD", "II"));
    }

    [Fact]
    public void LpClimb_RecordsAPromotion_InTheFirstMatch()
    {
        var climb = ClimbCalculator.LpClimb([Ranked(0, "PLATINUM", "IV", 10, 25)])!;

        climb.Start.Should().Be(new LadderRank("GOLD", "I", 85));
        climb.Events.Should().Equal(new LadderEvent(0, ClimbCalculator.Promotion, "PLATINUM", "IV"));
    }

    [Fact]
    public void LpClimb_RecordsADemotion()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "GOLD", "IV", 10, null),
            Ranked(1, "SILVER", "I", 85, -25, win: false)])!;

        climb.Events.Should().Equal(new LadderEvent(1, ClimbCalculator.Demotion, "SILVER", "I"));
        climb.Net.Should().Be(-25);
    }

    [Fact]
    public void LpClimb_SharesOneCountAboveMaster_AndNamesTheApexTier()
    {
        var climb = ClimbCalculator.LpClimb([
            Ranked(0, "DIAMOND", "I", 90, null),
            Ranked(1, "MASTER", "I", 15, 25),
            Ranked(2, "MASTER", "I", 480, null),
            Ranked(3, "GRANDMASTER", "I", 501, 21)])!;

        climb.Points.Select(p => p.Ladder).Should().Equal(2790, 2815, 3280, 3301);
        climb.Events.Should().Equal(
            new LadderEvent(1, ClimbCalculator.Promotion, "MASTER", null),
            new LadderEvent(3, ClimbCalculator.Promotion, "GRANDMASTER", null));
        climb.End.Should().Be(new LadderRank("GRANDMASTER", null, 501));
    }

    // ───────────────────────── FR 11: biggest drop ─────────────────────────

    [Fact]
    public void BiggestDrop_IsTheWorstRunOfTwoOrMoreLosses()
    {
        var drop = ClimbCalculator.BiggestDrop([
            Change(0, -20, win: false),
            Change(1, -25, win: false),
            Change(2, 20, win: true),
            Change(3, -30, win: false),
            Change(4, -22, win: false),
            Change(5, -18, win: false)]);

        drop.Should().Be(new LpDrop(5, -70, 3));
    }

    [Fact]
    public void BiggestDrop_BreaksARun_AtALossWithUnknownLp()
    {
        var drop = ClimbCalculator.BiggestDrop([
            Change(0, -25, win: false),
            Change(1, null, win: false),
            Change(2, -25, win: false)]);

        drop.Should().BeNull();
    }

    [Theory]
    [InlineData(-20, -20, true)]
    [InlineData(-20, -19, false)]
    public void BiggestDrop_NeedsFortyLp(int first, int second, bool shown)
    {
        var drop = ClimbCalculator.BiggestDrop([Change(0, first, win: false), Change(1, second, win: false)]);

        (drop is not null).Should().Be(shown);
    }

    [Fact]
    public void BiggestDrop_IgnoresASingleLoss()
    {
        ClimbCalculator.BiggestDrop([Change(0, -60, win: false), Change(1, 20, win: true)]).Should().BeNull();
    }

    // ───────────────────────── FR 12: win-rate mode ─────────────────────────

    [Fact]
    public void WinRate_ComparesTheFirstAndLastTenMatches()
    {
        // First 10: 5 wins. Last 10: 8 wins.
        var rows = SoloRows.Many(20, i => SoloRows.Make(i, win: i < 10 ? i % 2 == 0 : i < 18));

        var winRate = ClimbCalculator.WinRate(rows)!;

        winRate.Was.Should().Be(50);
        winRate.Now.Should().Be(80);
        winRate.Points.Should().HaveCount(11);
        winRate.Points[0].Should().Be(new WinRatePoint(9, 50));
    }

    [Fact]
    public void WinRate_IsNull_UnderTwentyMatches()
    {
        ClimbCalculator.WinRate(SoloRows.Many(19)).Should().BeNull();
    }

    // ───────────────────────── FR 24: champions ─────────────────────────

    [Fact]
    public void Champions_InLpMode_SumsKnownLp_AndLeavesOutChampionsUnderThreeMatches()
    {
        SoloMatchRow Match(int i, int id, string name, int? lp, bool win = true)
            => SoloRows.Make(i, win: win, championId: id, championName: name, lpChange: lp);

        var (champions, leftOut) = ClimbCalculator.Champions([
            Match(0, 1, "Ahri", 20), Match(1, 1, "Ahri", 22), Match(2, 1, "Ahri", null),
            Match(3, 2, "Syndra", -18, win: false), Match(4, 2, "Syndra", 20), Match(5, 2, "Syndra", -17, win: false),
            Match(6, 3, "Orianna", 20), Match(7, 3, "Orianna", 21),
            Match(8, 4, "Zed", 20)], LpCoverageRule.LpMode);

        champions.Should().Equal(
            new ChampionClimb(1, "Ahri", 3, 3, 42),
            new ChampionClimb(2, "Syndra", 3, 1, -15));
        leftOut.Should().Equal("Orianna", "Zed");
    }

    [Fact]
    public void Champions_InWinRateMode_IsNetWins_AtMostFive()
    {
        var rows = SoloRows.Many(21, i => SoloRows.Make(i, win: i % 3 != 0, championId: i % 7, championName: $"Champ{i % 7}"));

        var (champions, leftOut) = ClimbCalculator.Champions(rows, LpCoverageRule.WinRateMode);

        champions.Should().HaveCount(5);
        champions.Should().BeInDescendingOrder(c => c.Value);
        champions.Should().OnlyContain(c => c.Value == c.Wins - (c.Matches - c.Wins));
        leftOut.Should().BeEmpty();
    }

    // ───────────────────────── Mode and rank ─────────────────────────

    [Fact]
    public void Calculate_FillsTheModesPart_AndCountsWinsAndLosses()
    {
        var rows = SoloRows.Many(20, i => Ranked(i, "GOLD", "II", 10 + i, i == 0 ? null : 1, win: true));

        var lp = ClimbCalculator.Calculate(rows, SoloScope.RankedSolo, singleAccount: true);
        var all = ClimbCalculator.Calculate(rows, SoloScope.AllQueues, singleAccount: true);

        lp.Mode.Should().Be(LpCoverageRule.LpMode);
        lp.Lp.Should().NotBeNull();
        lp.WinRate.Should().BeNull();
        lp.Wins.Should().Be(20);
        lp.Losses.Should().Be(0);
        all.Mode.Should().Be(LpCoverageRule.WinRateMode);
        all.Lp.Should().BeNull();
        all.WinRate.Should().NotBeNull();
    }

    [Fact]
    public void CurrentRank_IsTheLatestRecordedRank_ForOneRankedQueueOfOneAccount()
    {
        var rows = new[] { Ranked(0, "GOLD", "II", 40, null), Ranked(1, "GOLD", "I", 12, null), SoloRows.Make(2) };

        ClimbCalculator.CurrentRank(rows, SoloScope.RankedFlex, singleAccount: true).Should().Be(new LadderRank("GOLD", "I", 12));
        ClimbCalculator.CurrentRank(rows, SoloScope.AllQueues, singleAccount: true).Should().BeNull();
        ClimbCalculator.CurrentRank(rows, SoloScope.RankedSolo, singleAccount: false).Should().BeNull();
    }
}
