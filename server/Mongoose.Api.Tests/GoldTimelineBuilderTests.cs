using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class GoldTimelineBuilderTests
{
    private static GoldTimelineData Data(
        int teamId,
        IEnumerable<(int Minute, int Team100, int Team200)> frames,
        IEnumerable<ObjectiveTaken>? objectives = null,
        bool fromCheckpoints = false) =>
        new(teamId, frames.Select(f => new TeamGoldFrame(f.Minute, f.Team100, f.Team200)).ToList(), fromCheckpoints, objectives?.ToList() ?? []);

    // Team 100 gold per minute with team 200 flat at 1,000, so the lead is team100 - 1,000
    private static IEnumerable<(int, int, int)> Leads(params int[] leads) =>
        leads.Select((lead, minute) => (minute, lead + 1000, 1000));

    [Fact]
    public void Build_TakesTheLead_FromThePlayersTeam()
    {
        var frames = new[] { (0, 2500, 2500), (1, 3000, 2800), (2, 3200, 4000) };

        GoldTimelineBuilder.Build(Data(100, frames))!.Points.Select(p => p.Lead).Should().Equal(0, 200, -800);
        GoldTimelineBuilder.Build(Data(200, frames))!.Points.Select(p => p.Lead).Should().Equal(0, -200, 800);
    }

    [Fact]
    public void Build_SaysWhereTheGoldCameFrom()
    {
        GoldTimelineBuilder.Build(Data(100, Leads(0, 0, 0)))!.Source.Should().Be("minute");
        GoldTimelineBuilder.Build(Data(100, Leads(0, 0, 0), fromCheckpoints: true))!.Source.Should().Be("checkpoints");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    public void Build_IsNull_WithFewerThanThreePoints(int count)
    {
        GoldTimelineBuilder.Build(Data(100, Leads(new int[count]))).Should().BeNull();
    }

    [Fact]
    public void Build_IsNull_ForATeamOutsideSummonersRift()
    {
        GoldTimelineBuilder.Build(Data(0, Leads(0, 0, 0))).Should().BeNull();
    }

    [Fact]
    public void Build_KeepsDragonsBaronsHeraldsAndTowers_InTimeOrder_AsOursOrTheirs()
    {
        var objectives = new[]
        {
            new ObjectiveTaken(200, "tower", 900),
            new ObjectiveTaken(100, "dragon", 400),
            new ObjectiveTaken(100, "grubs", 350),
            new ObjectiveTaken(200, "inhibitor", 1500),
            new ObjectiveTaken(100, "herald", 840),
            new ObjectiveTaken(200, "baron", 1500)
        };

        var moments = GoldTimelineBuilder.Build(Data(100, Leads(0, 0, 0), objectives))!.Moments;

        moments.Select(m => (m.Type, m.TimestampSec, m.Ours)).Should().Equal(
            ("dragon", 400, true), ("herald", 840, true), ("tower", 900, false), ("baron", 1500, false));
    }

    [Fact]
    public void Build_FindsTheBiggestSwing_WithinThreeMinutes()
    {
        // A slow climb to +2,000 over 8 minutes isn't a swing; the drop from +2,000 to −500 is
        var timeline = GoldTimelineBuilder.Build(Data(100, Leads(0, 250, 500, 750, 1000, 1250, 1500, 1750, 2000, 1800, 300, -500, -500, -500, -500)))!;

        timeline.Swing.Should().Be(new GoldTimelineSwing(FromMinute: 8, Minute: 11, Change: -2500));
    }

    [Fact]
    public void Build_LeavesOutTheEndPush_FromTheSwing()
    {
        // The ace and the towers in the last 3 minutes (+8,500) are the result; the drop at 6 decided it
        var timeline = GoldTimelineBuilder.Build(Data(100, Leads(0, 0, 0, 0, 0, 0, -2500, -2500, -2500, -2500, 1000, 6000)))!;

        timeline.Swing.Should().Be(new GoldTimelineSwing(FromMinute: 5, Minute: 6, Change: -2500));
    }

    [Fact]
    public void Build_LeavesOutTheSwing_UnderTheMinimum()
    {
        var timeline = GoldTimelineBuilder.Build(Data(100, Leads(0, 400, 800, 1200, 1400)))!;

        timeline.Swing.Should().BeNull();
    }

    [Fact]
    public void Build_ComparesNeighbouringCheckpoints_FiveMinutesApart()
    {
        var frames = new[] { (0, 2500, 2500), (5, 8000, 8200), (10, 15000, 17000), (15, 22000, 24000) };

        var timeline = GoldTimelineBuilder.Build(Data(100, frames, fromCheckpoints: true))!;

        timeline.Swing.Should().Be(new GoldTimelineSwing(FromMinute: 5, Minute: 10, Change: -1800));
    }

    [Fact]
    public void Build_KeepsTheEarliestSwing_OnATie()
    {
        var timeline = GoldTimelineBuilder.Build(Data(100, Leads(0, 2000, 2000, 2000, 2000, 2000, 0)))!;

        timeline.Swing.Should().Be(new GoldTimelineSwing(FromMinute: 0, Minute: 1, Change: 2000));
    }
}
