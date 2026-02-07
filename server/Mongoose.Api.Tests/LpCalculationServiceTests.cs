using FluentAssertions;
using RiotProxy.Application.Services;
using Xunit;

namespace RiotProxy.Tests;

/// <summary>
/// Tests for LpCalculationService - LP calculation business logic.
/// These are pure functions with no dependencies, making them ideal for unit testing.
/// </summary>
public class LpCalculationServiceTests
{
    private readonly LpCalculationService _sut = new();

    #region CalculateAbsoluteLp Tests

    [Theory]
    [InlineData("IRON", "IV", 0, 0)]
    [InlineData("IRON", "IV", 50, 50)]
    [InlineData("IRON", "III", 0, 100)]
    [InlineData("IRON", "II", 0, 200)]
    [InlineData("IRON", "I", 0, 300)]
    [InlineData("IRON", "I", 99, 399)]
    public void CalculateAbsoluteLp_Iron_ReturnsCorrectValue(string tier, string division, int lp, int expected)
    {
        _sut.CalculateAbsoluteLp(tier, division, lp).Should().Be(expected);
    }

    [Theory]
    [InlineData("BRONZE", "IV", 0, 400)]
    [InlineData("BRONZE", "I", 99, 799)]
    [InlineData("SILVER", "IV", 0, 800)]
    [InlineData("GOLD", "IV", 0, 1200)]
    [InlineData("PLATINUM", "IV", 0, 1600)]
    [InlineData("EMERALD", "IV", 0, 2000)]
    [InlineData("DIAMOND", "IV", 0, 2400)]
    [InlineData("DIAMOND", "I", 99, 2799)]
    public void CalculateAbsoluteLp_AllTiers_ReturnsCorrectValue(string tier, string division, int lp, int expected)
    {
        _sut.CalculateAbsoluteLp(tier, division, lp).Should().Be(expected);
    }

    [Theory]
    [InlineData("MASTER", null, 0, 2800)]
    [InlineData("MASTER", null, 150, 2950)]
    [InlineData("GRANDMASTER", null, 500, 3300)]
    [InlineData("CHALLENGER", null, 1000, 3800)]
    public void CalculateAbsoluteLp_MasterPlus_ReturnsCorrectValue(string tier, string? division, int lp, int expected)
    {
        _sut.CalculateAbsoluteLp(tier, division, lp).Should().Be(expected);
    }

    [Fact]
    public void CalculateAbsoluteLp_NullTier_ReturnsLpOnly()
    {
        _sut.CalculateAbsoluteLp(null, "IV", 50).Should().Be(50);
    }

    [Fact]
    public void CalculateAbsoluteLp_CaseInsensitive()
    {
        _sut.CalculateAbsoluteLp("gold", "iv", 50).Should().Be(1250);
        _sut.CalculateAbsoluteLp("Gold", "IV", 50).Should().Be(1250);
        _sut.CalculateAbsoluteLp("GOLD", "IV", 50).Should().Be(1250);
    }

    #endregion

    #region GetTierValue Tests

    [Theory]
    [InlineData("IRON", 0)]
    [InlineData("BRONZE", 400)]
    [InlineData("SILVER", 800)]
    [InlineData("GOLD", 1200)]
    [InlineData("PLATINUM", 1600)]
    [InlineData("EMERALD", 2000)]
    [InlineData("DIAMOND", 2400)]
    [InlineData("MASTER", 2800)]
    [InlineData("GRANDMASTER", 2800)]
    [InlineData("CHALLENGER", 2800)]
    public void GetTierValue_ReturnsCorrectValue(string tier, int expected)
    {
        _sut.GetTierValue(tier).Should().Be(expected);
    }

    [Fact]
    public void GetTierValue_UnknownTier_ReturnsZero()
    {
        _sut.GetTierValue("UNKNOWN").Should().Be(0);
        _sut.GetTierValue("").Should().Be(0);
        _sut.GetTierValue(null).Should().Be(0);
    }

    #endregion

    #region GetDivisionValue Tests

    [Theory]
    [InlineData("IV", 0)]
    [InlineData("III", 100)]
    [InlineData("II", 200)]
    [InlineData("I", 300)]
    public void GetDivisionValue_ReturnsCorrectValue(string division, int expected)
    {
        _sut.GetDivisionValue(division).Should().Be(expected);
    }

    [Fact]
    public void GetDivisionValue_NullOrEmpty_ReturnsZero()
    {
        _sut.GetDivisionValue(null).Should().Be(0);
        _sut.GetDivisionValue("").Should().Be(0);
    }

    #endregion

    #region GetTierLevel Tests

    [Theory]
    [InlineData("IRON", 1)]
    [InlineData("BRONZE", 2)]
    [InlineData("SILVER", 3)]
    [InlineData("GOLD", 4)]
    [InlineData("PLATINUM", 5)]
    [InlineData("EMERALD", 6)]
    [InlineData("DIAMOND", 7)]
    [InlineData("MASTER", 8)]
    [InlineData("GRANDMASTER", 9)]
    [InlineData("CHALLENGER", 10)]
    public void GetTierLevel_ReturnsCorrectLevel(string tier, int expected)
    {
        _sut.GetTierLevel(tier).Should().Be(expected);
    }

    [Fact]
    public void GetTierLevel_UnknownTier_ReturnsZero()
    {
        _sut.GetTierLevel("UNKNOWN").Should().Be(0);
        _sut.GetTierLevel(null).Should().Be(0);
    }

    #endregion

    #region GetDivisionLevel Tests

    [Theory]
    [InlineData("IV", 1)]
    [InlineData("III", 2)]
    [InlineData("II", 3)]
    [InlineData("I", 4)]
    public void GetDivisionLevel_ReturnsCorrectLevel(string division, int expected)
    {
        _sut.GetDivisionLevel(division).Should().Be(expected);
    }

    [Fact]
    public void GetDivisionLevel_NullOrEmpty_ReturnsZero()
    {
        _sut.GetDivisionLevel(null).Should().Be(0);
        _sut.GetDivisionLevel("").Should().Be(0);
    }

    #endregion

    #region IsPromotion Tests

    [Theory]
    [InlineData("SILVER", "I", "GOLD", "IV")]   // Tier promotion
    [InlineData("GOLD", "IV", "GOLD", "III")]   // Division promotion
    [InlineData("GOLD", "II", "GOLD", "I")]     // Division promotion
    [InlineData("DIAMOND", "I", "MASTER", null)] // To Master
    public void IsPromotion_WhenPromoted_ReturnsTrue(string prevTier, string? prevDiv, string currTier, string? currDiv)
    {
        _sut.IsPromotion(prevTier, prevDiv, currTier, currDiv!).Should().BeTrue();
    }

    [Theory]
    [InlineData("GOLD", "III", "GOLD", "III")]  // Same rank
    [InlineData("GOLD", "III", "GOLD", "IV")]   // Demotion
    [InlineData("GOLD", "IV", "SILVER", "I")]   // Tier demotion
    [InlineData("MASTER", null, "DIAMOND", "I")] // From Master
    public void IsPromotion_WhenNotPromoted_ReturnsFalse(string prevTier, string? prevDiv, string currTier, string? currDiv)
    {
        _sut.IsPromotion(prevTier, prevDiv, currTier, currDiv!).Should().BeFalse();
    }

    [Fact]
    public void IsPromotion_NullPreviousTier_ReturnsFalse()
    {
        _sut.IsPromotion(null, "IV", "GOLD", "IV").Should().BeFalse();
    }

    #endregion

    #region IsDemotion Tests

    [Theory]
    [InlineData("GOLD", "IV", "SILVER", "I")]    // Tier demotion
    [InlineData("GOLD", "III", "GOLD", "IV")]    // Division demotion
    [InlineData("GOLD", "I", "GOLD", "II")]      // Division demotion
    [InlineData("MASTER", null, "DIAMOND", "I")] // From Master
    public void IsDemotion_WhenDemoted_ReturnsTrue(string prevTier, string? prevDiv, string currTier, string? currDiv)
    {
        _sut.IsDemotion(prevTier, prevDiv, currTier, currDiv!).Should().BeTrue();
    }

    [Theory]
    [InlineData("GOLD", "III", "GOLD", "III")]  // Same rank
    [InlineData("GOLD", "IV", "GOLD", "III")]   // Promotion
    [InlineData("SILVER", "I", "GOLD", "IV")]   // Tier promotion
    [InlineData("DIAMOND", "I", "MASTER", null)] // To Master
    public void IsDemotion_WhenNotDemoted_ReturnsFalse(string prevTier, string? prevDiv, string currTier, string? currDiv)
    {
        _sut.IsDemotion(prevTier, prevDiv, currTier, currDiv!).Should().BeFalse();
    }

    [Fact]
    public void IsDemotion_NullPreviousTier_ReturnsFalse()
    {
        _sut.IsDemotion(null, "IV", "GOLD", "IV").Should().BeFalse();
    }

    #endregion

    #region FormatRank Tests

    [Theory]
    [InlineData("GOLD", "IV", "Gold IV")]
    [InlineData("PLATINUM", "I", "Platinum I")]
    [InlineData("DIAMOND", "II", "Diamond II")]
    [InlineData("gold", "iv", "Gold iv")]  // Division case preserved
    public void FormatRank_WithDivision_ReturnsFormattedString(string tier, string division, string expected)
    {
        _sut.FormatRank(tier, division).Should().Be(expected);
    }

    [Theory]
    [InlineData("MASTER", null, "Master")]
    [InlineData("GRANDMASTER", "", "Grandmaster")]
    [InlineData("CHALLENGER", null, "Challenger")]
    public void FormatRank_WithoutDivision_ReturnsTierOnly(string tier, string? division, string expected)
    {
        _sut.FormatRank(tier, division).Should().Be(expected);
    }

    [Fact]
    public void FormatRank_EmptyTier_WithDivision_ReturnsSpacePlusDivision()
    {
        // When tier is empty but division is provided, result is " {division}"
        _sut.FormatRank("", "IV").Should().Be(" IV");
    }

    [Fact]
    public void FormatRank_EmptyTierAndDivision_ReturnsEmptyString()
    {
        _sut.FormatRank("", "").Should().Be("");
        _sut.FormatRank("", null).Should().Be("");
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void CalculateAbsoluteLp_ProgressionFromIronToChallenger_IsMonotonicallyIncreasing()
    {
        // Note: Master, Grandmaster, and Challenger all share the same base LP (2800).
        // Progression within Master+ is purely by LP, not by tier name.
        // This test validates LP-based progression through the ranks.
        var ranks = new[]
        {
            ("IRON", "IV", 0),
            ("IRON", "I", 99),
            ("BRONZE", "IV", 0),
            ("SILVER", "IV", 0),
            ("GOLD", "IV", 0),
            ("PLATINUM", "IV", 0),
            ("EMERALD", "IV", 0),
            ("DIAMOND", "IV", 0),
            ("DIAMOND", "I", 99),
            ("MASTER", null as string, 0),
            ("MASTER", null, 500),
            ("GRANDMASTER", null, 600),  // Higher LP than Master to show progression
            ("CHALLENGER", null, 1000),
        };

        var previousLp = -1;
        foreach (var (tier, division, lp) in ranks)
        {
            var absoluteLp = _sut.CalculateAbsoluteLp(tier, division, lp);
            absoluteLp.Should().BeGreaterThan(previousLp,
                $"{tier} {division} {lp}LP should be greater than previous rank");
            previousLp = absoluteLp;
        }
    }

    [Fact]
    public void IsPromotion_And_IsDemotion_AreMutuallyExclusive()
    {
        var testCases = new[]
        {
            ("GOLD", "IV", "GOLD", "III"),
            ("GOLD", "III", "GOLD", "IV"),
            ("SILVER", "I", "GOLD", "IV"),
            ("GOLD", "IV", "SILVER", "I"),
        };

        foreach (var (prevTier, prevDiv, currTier, currDiv) in testCases)
        {
            var isPromotion = _sut.IsPromotion(prevTier, prevDiv, currTier, currDiv);
            var isDemotion = _sut.IsDemotion(prevTier, prevDiv, currTier, currDiv);

            // Cannot be both promotion and demotion
            (isPromotion && isDemotion).Should().BeFalse(
                $"Rank change from {prevTier} {prevDiv} to {currTier} {currDiv} cannot be both promotion and demotion");
        }
    }

    #endregion
}

