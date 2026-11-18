using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class LpCoverageRuleTests
{
    /// <summary><paramref name="count"/> matches of which the first <paramref name="known"/> have an LP change.</summary>
    private static List<SoloMatchRow> Rows(int count, int known)
        => SoloRows.Many(count, i => SoloRows.Make(i, lpChange: i < known ? 20 : null));

    [Theory]
    [InlineData(20, 16, LpCoverageRule.LpMode)]        // 80% exactly
    [InlineData(20, 15, LpCoverageRule.WinRateMode)]   // 75%
    [InlineData(12, 10, LpCoverageRule.LpMode)]        // 10 known, 83%
    [InlineData(10, 9, LpCoverageRule.WinRateMode)]    // 90% but only 9 known
    [InlineData(10, 10, LpCoverageRule.LpMode)]
    public void Mode_NeedsEightyPercentAndTenKnownChanges(int count, int known, string expected)
    {
        LpCoverageRule.Mode(SoloScope.RankedSolo, singleAccount: true, Rows(count, known)).Should().Be(expected);
    }

    [Fact]
    public void Mode_IsWinRate_ForAllQueues()
    {
        LpCoverageRule.Mode(SoloScope.AllQueues, singleAccount: true, Rows(20, 20)).Should().Be(LpCoverageRule.WinRateMode);
    }

    [Fact]
    public void Mode_IsWinRate_ForSeveralAccounts()
    {
        LpCoverageRule.Mode(SoloScope.RankedFlex, singleAccount: false, Rows(20, 20)).Should().Be(LpCoverageRule.WinRateMode);
    }

    [Fact]
    public void Mode_IsWinRate_WithoutMatches()
    {
        LpCoverageRule.Mode(SoloScope.RankedSolo, singleAccount: true, []).Should().Be(LpCoverageRule.WinRateMode);
    }
}
