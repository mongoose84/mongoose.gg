using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class DeathZonesCalculatorTests
{
    private static int _match;

    /// <summary>A counted death in its own match at an anchor, optionally followed by an enemy dragon.</summary>
    private static (SoloDeathRow Death, SoloObjectiveRow? Objective) At(string region, int timestampSec = 600, bool costly = false)
    {
        var anchor = MapRegions.Get(region);
        var matchId = $"M{Interlocked.Increment(ref _match)}";
        var death = new SoloDeathRow(matchId, 100, "MIDDLE", 3,
            (int)(anchor.U * MapRegions.MapSize), (int)(anchor.V * MapRegions.MapSize), timestampSec, 8, [], 1);
        return (death, costly ? new SoloObjectiveRow(matchId, 200, "dragon", timestampSec + 30) : null);
    }

    private static SoloDeathZones Calculate(IEnumerable<(SoloDeathRow Death, SoloObjectiveRow? Objective)> items, IEnumerable<SoloDeathRow>? extra = null)
    {
        var list = items.ToList();
        return DeathZonesCalculator.Calculate(
            list.Select(i => i.Death).Concat(extra ?? []).ToList(),
            [],
            list.Where(i => i.Objective != null).Select(i => i.Objective!).ToList());
    }

    private static IEnumerable<(SoloDeathRow, SoloObjectiveRow?)> Many(int count, string region, int costly = 0, int timestampSec = 600)
        => Enumerable.Range(0, count).Select(i => At(region, timestampSec, i < costly));

    [Fact]
    public void Calculate_IsReady_At30CountedDeaths_AndCountsTheRestAsMissing()
    {
        var uncounted = Enumerable.Range(0, 4).Select(_ => At(MapRegions.RiverBot).Death with { TimestampSec = null });

        var notReady = Calculate(Many(29, MapRegions.RiverBot), uncounted);
        var ready = Calculate(Many(30, MapRegions.RiverBot));

        notReady.Ready.Should().BeFalse();
        notReady.Deaths.Should().Be(29);
        notReady.MissingDetail.Should().Be(4);
        ready.Ready.Should().BeTrue();
    }

    [Fact]
    public void Calculate_ListsRegionsWithFiveDeaths_SortedByLostObjectivesThenDeaths()
    {
        var zones = Calculate(
            Many(12, MapRegions.MidLaneYours, costly: 1)
                .Concat(Many(6, MapRegions.RiverBot, costly: 4))
                .Concat(Many(8, MapRegions.JungleEnemyBot, costly: 1))
                .Concat(Many(4, MapRegions.BaronPit, costly: 4))).Zones;

        zones.Select(z => z.Key).Should().Equal(MapRegions.RiverBot, MapRegions.MidLaneYours, MapRegions.JungleEnemyBot);
        zones[0].Should().BeEquivalentTo(new { Deaths = 6, LostObjectives = 4, Costly = true });
        zones[1].Costly.Should().BeFalse();
        zones[0].Anchor.Should().Be(MapRegions.Get(MapRegions.RiverBot));
    }

    [Fact]
    public void Calculate_ListsAtMostFiveZones()
    {
        var regions = new[] { MapRegions.RiverBot, MapRegions.RiverTop, MapRegions.MidLaneYours, MapRegions.MidLaneEnemy, MapRegions.JungleYoursTop, MapRegions.JungleYoursBot };

        Calculate(regions.SelectMany(r => Many(5, r))).Zones.Should().HaveCount(5);
    }

    [Fact]
    public void Timing_NamesThePhaseWith60PercentOrMore_ElseSpread()
    {
        var late = Calculate(Many(3, MapRegions.RiverBot, timestampSec: 1600).Concat(Many(2, MapRegions.RiverBot))).Zones.Single();
        var spread = Calculate(Many(2, MapRegions.RiverTop, timestampSec: 1600).Concat(Many(2, MapRegions.RiverTop)).Concat(Many(1, MapRegions.RiverTop, timestampSec: 1000))).Zones.Single();

        late.Timing.Should().Be(new ZoneTiming(DeathClassifier.Late, 3));
        spread.Timing.Phase.Should().BeNull();
    }

    [Fact]
    public void Calculate_BreaksDownAllDeathsAndEachZone()
    {
        var result = Calculate(Many(6, MapRegions.RiverBot, costly: 2).Concat(Many(3, MapRegions.MidLaneYours, timestampSec: 1600)));

        result.All.Phase.Should().BeEquivalentTo(new Dictionary<string, int> { ["early"] = 6, ["mid"] = 0, ["late"] = 3 });
        result.All.Cost["dragon"].Should().Be(2);
        result.All.How.Values.Sum().Should().Be(9);
        result.ByZone.Keys.Should().Equal(MapRegions.RiverBot);
        result.ByZone[MapRegions.RiverBot].Phase["early"].Should().Be(6);
    }
}
