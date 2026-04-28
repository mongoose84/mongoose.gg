using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class TrendBadgeCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private static RoleBaseline MakeBaseline(
        int gamesCount = 10,
        double avgDamageDealt = 20000,
        double avgDamageTaken = 15000,
        double avgDeaths = 4,
        double avgKills = 5,
        double avgAssists = 6,
        double avgKda = 2.75,
        double avgVisionScore = 20,
        double avgCsPerMin = 7,
        double avgKillParticipation = 0.55,
        double avgGoldEarned = 12000,
        double avgGoldPerMin = 400,
        double avgGameDurationSec = 1800,
        double avgCreepScore = 180,
        double winRate = 0.5) =>
        new RoleBaseline(
            "MID", gamesCount,
            avgKills, avgDeaths, avgAssists, avgKda,
            avgCreepScore, avgCsPerMin, avgGoldEarned, avgGoldPerMin,
            avgDamageDealt, avgDamageTaken, avgVisionScore,
            avgKillParticipation, avgGameDurationSec, winRate);

    // 1800s = 30min, 7 cs/min => 210 cs
    private static MatchListRawData MakeMatch(
        int damageDealt = 20000,
        int damageTaken = 15000,
        int deaths = 4,
        int kills = 5,
        int assists = 6,
        int visionScore = 20,
        int creepScore = 210,     // 7 cs/min at 30 min
        int gameDurationSec = 1800,
        decimal killParticipation = 0.55m) =>
        new MatchListRawData(
            "MATCH1", 420, 103, "Malzahar", "MID", "MID",
            true,
            kills, deaths, assists,
            creepScore, 12000, gameDurationSec,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            damageDealt, damageTaken, visionScore,
            killParticipation, 0.20m, 0,
            100, 30, 25, null,
            80000, 70000, null,
            2, 3, 1, 2, 8, 7);

    private static MatchListSummaryRawData MakeSummaryMatch(
        int deaths = 4,
        int kills = 5,
        int assists = 6,
        int creepScore = 210,
        int gameDurationSec = 1800) =>
        new MatchListSummaryRawData(
            "MATCH1", "Player", "EUW", "EUW1",
            420, 103, "Malzahar", "MID", "MID",
            true,
            kills, deaths, assists,
            creepScore, 12000, gameDurationSec,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    // ─────────────── ComputeTrendBadge — baseline guard ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsNull_WhenBaselineGameCountLessThan3()
    {
        var baseline = MakeBaseline(gamesCount: 2);
        var match = MakeMatch();

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull();
    }

    [Fact]
    public void ComputeTrendBadge_ReturnsNull_WhenStatsMatchBaseline()
    {
        var baseline = MakeBaseline();
        // Match exactly at baseline — no threshold exceeded
        var match = MakeMatch(
            damageDealt: 20000,
            damageTaken: 15000,
            deaths: 4,
            visionScore: 20,
            creepScore: 210,
            killParticipation: 0.55m);

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull();
    }

    [Fact]
    public void ComputeTrendBadge_ReturnsNull_WhenBaselineIsAllZeros()
    {
        var baseline = MakeBaseline(
            gamesCount: 10,
            avgDamageDealt: 0, avgDamageTaken: 0, avgDeaths: 0,
            avgVisionScore: 0, avgCsPerMin: 0, avgKillParticipation: 0);
        var match = MakeMatch();

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull();
    }

    // ─────────────── ComputeTrendBadge — damage insights ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsAboveAvgDamage_WhenDamageMore20PctAboveBaseline()
    {
        var baseline = MakeBaseline(avgDamageDealt: 20000);
        var match = MakeMatch(damageDealt: 24001); // > 20% above

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Above avg damage");
        result.Type.Should().Be("positive");
    }

    [Fact]
    public void ComputeTrendBadge_ReturnsBelowAvgDamage_WhenDamageMore20PctBelowBaseline()
    {
        var baseline = MakeBaseline(avgDamageDealt: 20000);
        var match = MakeMatch(damageDealt: 15999); // > 20% below

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Below avg damage");
        result.Type.Should().Be("neutral");
    }

    // ─────────────── ComputeTrendBadge — tankiness ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsTankierThanUsual_WhenDamageTakenMore25PctAboveBaseline()
    {
        var baseline = MakeBaseline(avgDamageTaken: 15000);
        var match = MakeMatch(damageTaken: 18751); // > 25% above

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Tankier than usual");
        result.Type.Should().Be("positive");
    }

    // ─────────────── ComputeTrendBadge — deaths ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsHigherDeaths_WhenDeathsMore30PctAboveBaseline()
    {
        var baseline = MakeBaseline(avgDeaths: 4);
        var match = MakeMatch(deaths: 6); // 50% above 4

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Higher deaths vs trend");
        result.Type.Should().Be("neutral");
    }

    [Fact]
    public void ComputeTrendBadge_ReturnsCleanGame_WhenDeathsMore30PctBelowBaselineAndDeathsLeq3()
    {
        var baseline = MakeBaseline(avgDeaths: 6);
        var match = MakeMatch(deaths: 1); // well below 30% threshold and <= 3

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Clean game");
        result.Type.Should().Be("positive");
    }

    [Fact]
    public void ComputeTrendBadge_DoesNotReturnCleanGame_WhenDeathsAbove3EvenIfBelowBaseline()
    {
        // deaths=4 (> 3) with very high baseline of 10 — "Clean game" requires deaths <= 3.
        // All other metrics are exactly at baseline, so no other badge fires either.
        var baseline = MakeBaseline(avgDeaths: 10);
        var match = MakeMatch(
            deaths: 4,
            damageDealt: 20000, damageTaken: 15000,
            visionScore: 20, creepScore: 210, gameDurationSec: 1800,
            killParticipation: 0.55m);

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull("deaths=4 exceeds CleanGameMaxDeaths=3 and no other thresholds are breached");
    }

    // ─────────────── ComputeTrendBadge — vision ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsStrongVisionControl_WhenVisionMore25PctAboveBaselineAndBaselineGt10()
    {
        var baseline = MakeBaseline(avgVisionScore: 20);
        var match = MakeMatch(visionScore: 26); // 30% above

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Strong vision control");
        result.Type.Should().Be("positive");
    }

    [Fact]
    public void ComputeTrendBadge_DoesNotReturnVisionBadge_WhenBaselineVisionLeq10()
    {
        // avgVisionScore=8 is below MinimumVisionBaseline=10, so vision badge cannot fire.
        // All other stats are exactly at baseline, so overall result is null.
        var baseline = MakeBaseline(avgVisionScore: 8);
        var match = MakeMatch(
            visionScore: 30,
            damageDealt: 20000, damageTaken: 15000,
            deaths: 4, creepScore: 210, gameDurationSec: 1800,
            killParticipation: 0.55m);

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull("avgVisionScore=8 is below MinimumVisionBaseline=10 and no other thresholds are breached");
    }

    // ─────────────── ComputeTrendBadge — CS ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsHighCsEfficiency_WhenCsPerMinMore15PctAboveBaselineAndBaselineGt4()
    {
        // baseline avgCsPerMin=7 at 30min => 210 cs
        // 15% above 7 = 8.05 cs/min; at 30min = 241.5 cs
        var baseline = MakeBaseline(avgCsPerMin: 7);
        var match = MakeMatch(creepScore: 250, gameDurationSec: 1800); // 8.33 cs/min

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("High CS efficiency");
        result.Type.Should().Be("positive");
    }

    [Fact]
    public void ComputeTrendBadge_DoesNotReturnCsBadge_WhenBaselineCsPerMinLeq4()
    {
        // avgCsPerMin=3 is below MinimumCsPerMinuteBaseline=4, so CS badge cannot fire.
        // All other stats are exactly at baseline, so overall result is null.
        var baseline = MakeBaseline(avgCsPerMin: 3);
        var match = MakeMatch(
            creepScore: 250, gameDurationSec: 1800,
            damageDealt: 20000, damageTaken: 15000,
            deaths: 4, visionScore: 20,
            killParticipation: 0.55m);

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().BeNull("avgCsPerMin=3 is below MinimumCsPerMinuteBaseline=4 and no other thresholds are breached");
    }

    // ─────────────── ComputeTrendBadge — kill participation ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsHighKillParticipation_WhenKpMore20PctAboveBaseline()
    {
        var baseline = MakeBaseline(avgKillParticipation: 0.50);
        var match = MakeMatch(killParticipation: 0.65m); // 30% above

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("High kill participation");
        result.Type.Should().Be("positive");
    }

    // ─────────────── ComputeTrendBadge — highest deviation wins ───────────────

    [Fact]
    public void ComputeTrendBadge_ReturnsInsightWithHighestDeviation_WhenMultipleInsightsExist()
    {
        // Damage 50% above baseline (huge deviation)
        // Deaths also high (40% above baseline) but damage deviation is larger
        var baseline = MakeBaseline(avgDamageDealt: 20000, avgDeaths: 5, avgDamageTaken: 15000);
        var match = MakeMatch(damageDealt: 30000, deaths: 7, damageTaken: 15000); // damage 50% above; deaths 40% above

        var result = TrendBadgeCalculator.ComputeTrendBadge(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Above avg damage"); // 50% deviation is larger than 40%
    }

    // ─────────────── ComputeTrendBadgeSummary — baseline guard ───────────────

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsNull_WhenBaselineGameCountLessThan3()
    {
        var baseline = MakeBaseline(gamesCount: 1);
        var match = MakeSummaryMatch();

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().BeNull();
    }

    // ─────────────── ComputeTrendBadgeSummary — deaths ───────────────

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsHigherDeaths_WhenDeathsMore30PctAboveBaseline()
    {
        var baseline = MakeBaseline(avgDeaths: 4);
        var match = MakeSummaryMatch(deaths: 7);

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Higher deaths vs trend");
    }

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsCleanGame_WhenDeathsLowAndLeq3()
    {
        // deaths=1, avgDeaths=6 -> death deviation = 83%
        // Suppress KDA badge: match kda=(1+1)/1=2 vs avgKda=5.0 -> negative deviation, no badge
        // Suppress CS badge: avgCsPerMin < 4
        var baseline = MakeBaseline(avgDeaths: 6, avgKda: 5.0, avgCsPerMin: 3.0);
        var match = MakeSummaryMatch(deaths: 1, kills: 1, assists: 1, creepScore: 60, gameDurationSec: 1800);

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Clean game");
    }

    // ─────────────── ComputeTrendBadgeSummary — CS ───────────────

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsHighCsEfficiency_WhenCsHighAndBaselineGt4()
    {
        var baseline = MakeBaseline(avgCsPerMin: 7);
        var match = MakeSummaryMatch(creepScore: 250, gameDurationSec: 1800);

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("High CS efficiency");
    }

    // ─────────────── ComputeTrendBadgeSummary — KDA ───────────────

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsStrongKda_WhenKdaMore25PctAboveBaseline()
    {
        // Baseline KDA 2.75. Need match KDA > 2.75 * 1.25 = 3.4375
        // kills=10, assists=5, deaths=2 => kda=(10+5)/2=7.5
        var baseline = MakeBaseline(avgKda: 2.75);
        var match = MakeSummaryMatch(kills: 10, assists: 5, deaths: 2);

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Strong KDA");
        result.Type.Should().Be("positive");
    }

    [Fact]
    public void ComputeTrendBadgeSummary_KdaCalculation_UsesKillsPlusAssistsWhenDeathsAreZero()
    {
        // deaths=0 => kda = kills + assists
        var baseline = MakeBaseline(avgKda: 5);
        var match = MakeSummaryMatch(kills: 8, assists: 5, deaths: 0); // kda = 13, 160% above 5

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Strong KDA");
    }

    [Fact]
    public void ComputeTrendBadgeSummary_ReturnsHighestDeviationInsight_WhenMultipleInsightsExist()
    {
        // deaths much higher than baseline, AND CS is a bit high — deaths deviation should win
        var baseline = MakeBaseline(avgDeaths: 3, avgCsPerMin: 7, avgKda: 2.0);
        // deaths=8 (166% above 3), cs deviation small
        var match = MakeSummaryMatch(deaths: 8, kills: 3, assists: 2, creepScore: 215, gameDurationSec: 1800);

        var result = TrendBadgeCalculator.ComputeTrendBadgeSummary(match, baseline);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Higher deaths vs trend");
    }
}
