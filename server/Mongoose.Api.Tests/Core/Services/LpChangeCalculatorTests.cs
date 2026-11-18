using FluentAssertions;
using Mongoose.Api.Core.Services;
using Mongoose.Api.Core.ValueObjects;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class LpChangeCalculatorTests
{
    private static RankSnapshot Rank(string? tier, string? division, int? lp) => new(tier, division, lp);

    [Fact]
    public void Compute_ReturnsTheDifference_WithinADivision()
    {
        LpChangeCalculator.Compute(Rank("EMERALD", "II", 45), Rank("EMERALD", "II", 64), win: true).Should().Be(19);
        LpChangeCalculator.Compute(Rank("EMERALD", "II", 64), Rank("EMERALD", "II", 47), win: false).Should().Be(-17);
    }

    [Fact]
    public void Compute_ResolvesPromotionsAndDemotions_AcrossDivisionsAndTiers()
    {
        // Gold I 90 → Platinum IV 12: +22
        LpChangeCalculator.Compute(Rank("GOLD", "I", 90), Rank("PLATINUM", "IV", 12), win: true).Should().Be(22);
        // Silver III 5 → Silver IV 85: −20
        LpChangeCalculator.Compute(Rank("SILVER", "III", 5), Rank("SILVER", "IV", 85), win: false).Should().Be(-20);
        // Diamond I 95 → Master 18: +23
        LpChangeCalculator.Compute(Rank("DIAMOND", "I", 95), Rank("MASTER", "I", 18), win: true).Should().Be(23);
    }

    [Fact]
    public void Compute_TreatsApexTiersAsOneLpLadder()
    {
        LpChangeCalculator.Compute(Rank("MASTER", "I", 480), Rank("GRANDMASTER", "I", 501), win: true).Should().Be(21);
    }

    [Fact]
    public void Compute_AllowsALossThatKeepsTheSameLp()
    {
        // 0 LP in Iron IV, or a demotion shield, can hold LP after a loss
        LpChangeCalculator.Compute(Rank("IRON", "IV", 0), Rank("IRON", "IV", 0), win: false).Should().Be(0);
    }

    [Theory]
    [InlineData(true, 30, 20)]    // a win that lost LP: one reading is stale
    [InlineData(true, 30, 30)]    // a win with no gain: the LP after was read before Riot applied it
    [InlineData(false, 30, 50)]   // a loss that gained LP
    public void Compute_ReturnsNull_WhenTheChangeContradictsTheResult(bool win, int before, int after)
    {
        LpChangeCalculator.Compute(Rank("GOLD", "II", before), Rank("GOLD", "II", after), win).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_ForAnImplausiblyLargeJump()
    {
        LpChangeCalculator.Compute(Rank("GOLD", "IV", 10), Rank("GOLD", "II", 20), win: true).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_WhenEitherSideHasNoRecordedRank()
    {
        LpChangeCalculator.Compute(null, Rank("GOLD", "II", 20), win: true).Should().BeNull();
        LpChangeCalculator.Compute(Rank("GOLD", "II", 20), null, win: true).Should().BeNull();
        LpChangeCalculator.Compute(Rank("GOLD", "II", null), Rank("GOLD", "II", 40), win: true).Should().BeNull();
    }
}
