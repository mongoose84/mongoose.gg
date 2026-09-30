using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class RankBenchmarkRuleTests
{
    private static List<SoloMatchRow> Mid(int count, string? tier = "EMERALD") =>
        SoloRows.Many(count, i => SoloRows.Make(i, tierAfter: tier));

    [Fact]
    public void Target_IsTheQueueTierAndMainRole_ForOneRankedQueueOfOneAccount()
    {
        var target = RankBenchmarkRule.Target(SoloScope.RankedSolo, singleAccount: true, Mid(20), []);

        target.Should().Be(new RankBenchmarkTarget(420, "EMERALD", "MIDDLE"));
    }

    [Theory]
    [InlineData(SoloScope.RankedFlex, 440)]
    [InlineData(SoloScope.RankedSolo, 420)]
    public void Target_UsesTheQueueId(string queue, int queueId)
    {
        RankBenchmarkRule.Target(queue, true, Mid(20), [])!.QueueId.Should().Be(queueId);
    }

    [Fact]
    public void Target_IsNull_ForAllQueues_OrSeveralAccounts()
    {
        RankBenchmarkRule.Target(SoloScope.AllQueues, true, Mid(20), []).Should().BeNull();
        RankBenchmarkRule.Target(SoloScope.RankedSolo, false, Mid(20), []).Should().BeNull();
    }

    [Fact]
    public void Target_TakesTheLatestTier_FromTheRangeOrTheSeason()
    {
        var rows = SoloRows.Many(20, i => SoloRows.Make(i, tierAfter: i < 15 ? "PLATINUM" : null));
        var season = new List<SoloMatchRow> { SoloRows.Make(18, tierAfter: "EMERALD") };

        RankBenchmarkRule.Target(SoloScope.RankedSolo, true, rows, season)!.Tier.Should().Be("EMERALD");
    }

    [Fact]
    public void Target_IsNull_WithoutAKnownTier()
    {
        RankBenchmarkRule.Target(SoloScope.RankedSolo, true, Mid(20, tier: null), []).Should().BeNull();
    }

    [Fact]
    public void Target_NeedsAMainRole_In70PercentOfTheRange()
    {
        // 14 of 20 is exactly 70%
        var mostlyMid = SoloRows.Many(20, i => SoloRows.Make(i, role: i < 14 ? "MIDDLE" : "TOP", tierAfter: "EMERALD"));
        var mixed = SoloRows.Many(20, i => SoloRows.Make(i, role: i < 13 ? "MIDDLE" : "TOP", tierAfter: "EMERALD"));

        RankBenchmarkRule.Target(SoloScope.RankedSolo, true, mostlyMid, [])!.Role.Should().Be("MIDDLE");
        RankBenchmarkRule.Target(SoloScope.RankedSolo, true, mixed, []).Should().BeNull();
    }

    [Fact]
    public void Target_IgnoresUnknownRoles_AsAMainRole()
    {
        var unknown = SoloRows.Many(20, i => SoloRows.Make(i, role: "UNKNOWN", tierAfter: "EMERALD"));

        RankBenchmarkRule.Target(SoloScope.RankedSolo, true, unknown, []).Should().BeNull();
    }

    [Theory]
    [InlineData(200, 20, true)]
    [InlineData(199, 20, false)]
    [InlineData(200, 19, false)]
    public void Qualifies_Needs200MatchesFrom20Players(int matches, int players, bool expected)
    {
        var pool = Enumerable.Range(0, matches)
            .Select(i => new SoloRankPoolRow($"player-{i % players}", SoloRows.Make(i)))
            .ToList();

        RankBenchmarkRule.Qualifies(pool).Should().Be(expected);
    }
}
