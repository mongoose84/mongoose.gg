using FluentAssertions;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class MapRegionsTests
{
    [Theory]
    [InlineData(0.66, 0.30, MapRegions.DragonPit)]
    [InlineData(0.34, 0.70, MapRegions.BaronPit)]
    [InlineData(0.05, 0.10, MapRegions.YourBase)]
    [InlineData(0.95, 0.90, MapRegions.EnemyBase)]
    [InlineData(0.05, 0.50, MapRegions.TopLaneYours)]
    [InlineData(0.50, 0.95, MapRegions.TopLaneEnemy)]
    [InlineData(0.50, 0.05, MapRegions.BotLaneYours)]
    [InlineData(0.95, 0.50, MapRegions.BotLaneEnemy)]
    [InlineData(0.42, 0.40, MapRegions.MidLaneYours)]
    [InlineData(0.58, 0.60, MapRegions.MidLaneEnemy)]
    [InlineData(0.22, 0.80, MapRegions.RiverTop)]
    [InlineData(0.80, 0.22, MapRegions.RiverBot)]
    [InlineData(0.25, 0.55, MapRegions.JungleYoursTop)]
    [InlineData(0.55, 0.25, MapRegions.JungleYoursBot)]
    [InlineData(0.40, 0.75, MapRegions.JungleEnemyTop)]
    [InlineData(0.75, 0.40, MapRegions.JungleEnemyBot)]
    public void Classify_PutsEachPositionInOneRegion(double u, double v, string expected)
    {
        MapRegions.Classify(u, v).Should().Be(expected);
    }

    [Fact]
    public void Classify_TestsPitsBeforeTheRiver()
    {
        // (0.64, 0.33) is on the river line and inside the dragon pit
        Math.Abs(0.64 + 0.33 - 1).Should().BeLessThan(0.07);
        MapRegions.Classify(0.64, 0.33).Should().Be(MapRegions.DragonPit);
    }

    [Fact]
    public void Classify_TestsBasesBeforeLanes()
    {
        // (0.10, 0.15) is inside the top-lane edge and inside the base
        MapRegions.Classify(0.10, 0.15).Should().Be(MapRegions.YourBase);
    }

    [Fact]
    public void Classify_PutsEveryAnchorInItsOwnRegion()
    {
        MapRegions.All.Should().OnlyContain(r => MapRegions.Classify(r.U, r.V) == r.Key);
    }

    [Fact]
    public void Normalize_MirrorsRedSide_SoTheBaseIsBottomLeft()
    {
        var blue = MapRegions.Normalize(1487, 2974, teamId: 100);
        var red = MapRegions.Normalize(1487, 2974, teamId: 200);

        blue.U.Should().BeApproximately(0.1, 1e-9);
        blue.V.Should().BeApproximately(0.2, 1e-9);
        red.U.Should().BeApproximately(0.9, 1e-9);
        red.V.Should().BeApproximately(0.8, 1e-9);

        // A red player's death in their own base lands in "your base" once mirrored
        var home = MapRegions.Normalize(14000, 14000, teamId: 200);
        MapRegions.Classify(home.U, home.V).Should().Be(MapRegions.YourBase);
    }

    [Theory]
    [InlineData("TOP", MapRegions.TopLaneYours)]
    [InlineData("MIDDLE", MapRegions.MidLaneEnemy)]
    [InlineData("UTILITY", MapRegions.BotLaneYours)]
    public void LaneOf_GivesBothHalvesOfTheRolesLane(string role, string region)
    {
        MapRegions.LaneOf(role).Should().Contain(region).And.HaveCount(2);
    }

    [Fact]
    public void LaneOf_IsEmptyForTheJungle()
    {
        MapRegions.LaneOf("JUNGLE").Should().BeEmpty();
    }
}
