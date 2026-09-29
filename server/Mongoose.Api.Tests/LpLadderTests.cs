using FluentAssertions;
using Mongoose.Api.Core.Services.Solo;
using Mongoose.Api.Core.ValueObjects;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class LpLadderTests
{
    private static RankSnapshot Rank(string? tier, string? division, int? lp) => new(tier, division, lp);

    [Theory]
    [InlineData("IRON", "IV", 0, 0)]
    [InlineData("IRON", "I", 50, 350)]
    [InlineData("EMERALD", "II", 64, 2264)]
    [InlineData("DIAMOND", "I", 99, 2799)]
    [InlineData("MASTER", "I", 0, 2800)]
    [InlineData("CHALLENGER", null, 1200, 4000)]
    [InlineData("emerald", "ii", 64, 2264)]
    public void Score_PlacesEveryRankOnOneLadder(string tier, string? division, int lp, int expected)
    {
        LpLadder.Score(Rank(tier, division, lp)).Should().Be(expected);
    }

    [Theory]
    [InlineData("UNRANKED", "I", 10)]
    [InlineData("GOLD", null, 10)]
    [InlineData("GOLD", "V", 10)]
    [InlineData(null, "I", 10)]
    [InlineData("GOLD", "I", -5)]
    public void Score_ReturnsNull_ForAnUnknownOrIncompleteRank(string? tier, string? division, int lp)
    {
        LpLadder.Score(Rank(tier, division, lp)).Should().BeNull();
    }

    [Theory]
    [InlineData(0, "IRON", "IV", 0)]
    [InlineData(2264, "EMERALD", "II", 64)]
    [InlineData(2799, "DIAMOND", "I", 99)]
    [InlineData(2800, "MASTER", null, 0)]
    [InlineData(3150, "MASTER", null, 350)]
    public void FromScore_IsTheInverseOfScore(int score, string tier, string? division, int lp)
    {
        LpLadder.FromScore(score).Should().Be(new LadderRank(tier, division, lp));
    }

    [Fact]
    public void FromScore_ClampsBelowIronIvToZero()
    {
        LpLadder.FromScore(-20).Should().Be(new LadderRank("IRON", "IV", 0));
    }
}
