using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class DecidingStatCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private static DecidingStatInput MakeInput(
        double? goldLeadAt10 = null,
        double? csAt10 = null,
        double? deathsBefore10 = null,
        double? killParticipation = null,
        double? visionPerMin = null,
        string role = "MIDDLE",
        int queueId = 420,
        int gameDurationSec = 1800,
        bool win = true,
        int teamKills = 20,
        bool isRemake = false) =>
        new(goldLeadAt10, csAt10, deathsBefore10, killParticipation, visionPerMin, role, queueId, gameDurationSec, win, teamKills, isRemake);

    private static StatUsual Usual(double average, double stdDev, int matches = 20) => new(average, stdDev, matches);

    private static Dictionary<string, StatUsual> Usuals(params (string Key, StatUsual Value)[] entries) =>
        entries.ToDictionary(e => e.Key, e => e.Value);

    // ───────────────────────── FR9: card omitted entirely ─────────────────────────

    [Fact]
    public void Compute_ReturnsNull_ForRemake()
    {
        var input = MakeInput(deathsBefore10: 0, isRemake: true);
        var usuals = Usuals(("deathsBefore10", Usual(0.4, 0.7)));

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_ForAramQueue()
    {
        var input = MakeInput(visionPerMin: 2.0, queueId: 450, role: "UNKNOWN");
        var usuals = Usuals(("visionPerMin", Usual(0.9, 0.25)));

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_ForNonSummonersRiftQueue()
    {
        // Arena (1700) is not in the Summoner's Rift set even with a real role
        var input = MakeInput(visionPerMin: 2.0, queueId: 1700);
        var usuals = Usuals(("visionPerMin", Usual(0.9, 0.25)));

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_ForUnknownRole()
    {
        var input = MakeInput(visionPerMin: 2.0, role: "UNKNOWN");
        var usuals = Usuals(("visionPerMin", Usual(0.9, 0.25)));

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull();
    }

    [Fact]
    public void Compute_ReturnsNull_WhenNoStatIsEligible()
    {
        // Under 10 minutes excludes gold/CS/deaths; too few team kills excludes kill participation;
        // vision has no usual on record — nothing left to score.
        var input = MakeInput(goldLeadAt10: 1000, csAt10: 90, deathsBefore10: 0, killParticipation: 80, visionPerMin: 2.0,
            gameDurationSec: 500, teamKills: 2);
        var usuals = Usuals(("visionPerMin", Usual(0.9, 0.25, matches: 2))); // below MinUsualMatches

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull();
    }

    // ───────────────────────── FR3: eligibility gates ─────────────────────────

    [Fact]
    public void Compute_ExcludesStat_WithFewerThanFiveUsualMatches()
    {
        var input = MakeInput(visionPerMin: 3.0, teamKills: 0, gameDurationSec: 500);
        var usuals = Usuals(("visionPerMin", Usual(0.9, 0.25, matches: 4)));

        DecidingStatCalculator.Compute(input, usuals).Should().BeNull("the only candidate stat has fewer than 5 usual matches");
    }

    [Fact]
    public void Compute_ExcludesStat_WithNullValueThisMatch()
    {
        // No minute-10 checkpoint recorded for gold, so it can never be scored even with a usual on file
        var input = MakeInput(goldLeadAt10: null, killParticipation: 80, teamKills: 20, gameDurationSec: 1800);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),
            ("killParticipation", Usual(58, 8)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Meters.Should().NotContain(m => m.Stat == "goldLeadAt10");
    }

    [Fact]
    public void Compute_ExcludesEarlyStats_WhenMatchUnder10Minutes()
    {
        var input = MakeInput(goldLeadAt10: 1000, csAt10: 90, deathsBefore10: 0, killParticipation: 80, visionPerMin: 2.0,
            gameDurationSec: 599, teamKills: 20);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),
            ("csAt10", Usual(71, 8)),
            ("deathsBefore10", Usual(0.4, 0.7)),
            ("killParticipation", Usual(58, 8)),
            ("visionPerMin", Usual(0.9, 0.25)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Meters.Select(m => m.Stat).Should().NotContain(new[] { "goldLeadAt10", "csAt10", "deathsBefore10" });
        result.Meters.Select(m => m.Stat).Should().Contain(new[] { "killParticipation", "visionPerMin" });
    }

    [Fact]
    public void Compute_ExcludesCsAt10_ForSupportRole()
    {
        var input = MakeInput(csAt10: 30, killParticipation: 80, role: "UTILITY", teamKills: 20, gameDurationSec: 1800);
        var usuals = Usuals(
            ("csAt10", Usual(20, 8)),
            ("killParticipation", Usual(58, 8)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Meters.Should().NotContain(m => m.Stat == "csAt10");
    }

    [Fact]
    public void Compute_ExcludesKillParticipation_WhenTeamKillsBelowFive()
    {
        var input = MakeInput(killParticipation: 80, visionPerMin: 2.0, teamKills: 4, gameDurationSec: 1800);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8)),
            ("visionPerMin", Usual(0.9, 0.25)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Meters.Should().NotContain(m => m.Stat == "killParticipation");
        result.Meters.Should().ContainSingle(m => m.Stat == "visionPerMin");
    }

    // ───────────────────────── FR1, FR4: spread floor and the flipped deaths sign ─────────────────────────

    [Fact]
    public void Compute_UsesSpreadFloor_WhenStddevIsSmallerThanTheFloor()
    {
        // deathsBefore10 floor is 0.7; a near-zero recorded stddev must not blow the score up.
        var input = MakeInput(deathsBefore10: 3, gameDurationSec: 1800);
        var usuals = Usuals(("deathsBefore10", Usual(average: 0, stdDev: 0.05)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        var meter = result!.Meters.Should().ContainSingle().Subject;
        // (3 - 0) / max(0.05, 0.7) = 4.2857..., then flipped because lower is better → −4.29
        meter.Score.Should().Be(-4.29);
    }

    [Fact]
    public void Compute_FlipsSign_ForDeathsBefore10()
    {
        // Fewer deaths than usual must score positive (better), even though the raw value is below average.
        var input = MakeInput(deathsBefore10: 0, gameDurationSec: 1800);
        var usuals = Usuals(("deathsBefore10", Usual(average: 1.4, stdDev: 0.7)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        var meter = result!.Meters.Should().ContainSingle().Subject;
        meter.Score.Should().BeGreaterThan(0);
        meter.Score.Should().Be(2.0); // (0 - 1.4) / 0.7 = -2.0, flipped → +2.0
    }

    // ───────────────────────── FR5, FR6: choosing the deciding stat ─────────────────────────

    [Fact]
    public void Compute_None_WhenNoStatStandsOut()
    {
        // Every score stays under the 1.0 stand-out threshold
        var input = MakeInput(killParticipation: 60, visionPerMin: 1.0, teamKills: 20, gameDurationSec: 1800);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8)),   // (60-58)/8 = 0.25
            ("visionPerMin", Usual(0.9, 0.25)));   // (1.0-0.9)/0.25 = 0.4

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("none");
        result.Stat.Should().BeNull();
        result.Meters.Should().HaveCount(2);
    }

    [Fact]
    public void Compute_Win_PicksTheTopStrength()
    {
        var input = MakeInput(goldLeadAt10: 1240, killParticipation: 71, csAt10: 84, gameDurationSec: 1800, teamKills: 20, win: true);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),       // (1240-180)/400 = 2.65
            ("killParticipation", Usual(58, 8)),     // (71-58)/8 = 1.625
            ("csAt10", Usual(71, 8)));                // (84-71)/8 = 1.625 too — but gold is strictly higher

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("strength");
        result.Stat.Should().Be("goldLeadAt10");
    }

    [Fact]
    public void Compute_Loss_PicksTheWorstShortfall()
    {
        var input = MakeInput(deathsBefore10: 3, killParticipation: 20, gameDurationSec: 1800, teamKills: 20, win: false);
        var usuals = Usuals(
            ("deathsBefore10", Usual(0.4, 0.7)),      // (3-0.4)/0.7 = 3.71, flipped → -3.71
            ("killParticipation", Usual(58, 8)));     // (20-58)/8 = -4.75

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("shortfall");
        result.Stat.Should().Be("killParticipation");
    }

    [Fact]
    public void Compute_Win_FallsBackTo_WonDespite_WhenOnlyShortfallsStandOut()
    {
        var input = MakeInput(deathsBefore10: 3, killParticipation: 20, gameDurationSec: 1800, teamKills: 20, win: true);
        var usuals = Usuals(
            ("deathsBefore10", Usual(0.4, 0.7)),      // → -3.71
            ("killParticipation", Usual(58, 8)));     // -4.75 (worst)

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("shortfall");
        result.Stat.Should().Be("killParticipation"); // the lowest score, i.e. "won despite"
    }

    [Fact]
    public void Compute_Loss_FallsBackTo_YouDidYourPart_WhenOnlyStrengthsStandOut()
    {
        var input = MakeInput(goldLeadAt10: 1240, killParticipation: 71, gameDurationSec: 1800, teamKills: 20, win: false);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),      // 2.65 (highest)
            ("killParticipation", Usual(58, 8)));   // 1.625

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("strength");
        result.Stat.Should().Be("goldLeadAt10"); // the highest score, i.e. "you did your part"
    }

    [Fact]
    public void Compute_TieBreaksByTableOrder()
    {
        // goldLeadAt10 and killParticipation land on exactly the same score; gold comes first in FR1's table
        var input = MakeInput(goldLeadAt10: 580, killParticipation: 71, gameDurationSec: 1800, teamKills: 20, win: true);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),      // (580-180)/400 = 1.0
            ("killParticipation", Usual(58, 13)));  // (71-58)/13 = 1.0 — same score, later in FR1's table

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Stat.Should().Be("goldLeadAt10");
    }

    // ───────────────────────── FR7: meters ─────────────────────────

    [Fact]
    public void Compute_OrdersMeters_DecidingStatFirstThenByAbsoluteScore_CappedAtThree()
    {
        var input = MakeInput(
            goldLeadAt10: 1240, csAt10: 60, deathsBefore10: 0, killParticipation: 60, visionPerMin: 1.0,
            gameDurationSec: 1800, teamKills: 20, win: true);
        var usuals = Usuals(
            ("goldLeadAt10", Usual(180, 400)),        // (1240-180)/400 = 2.65 → deciding stat
            ("csAt10", Usual(71, 8)),                 // (60-71)/8 = -1.375
            ("deathsBefore10", Usual(0.4, 0.7)),      // (0-0.4)/0.7 = -0.571, flipped → 0.571
            ("killParticipation", Usual(58, 8)),      // (60-58)/8 = 0.25
            ("visionPerMin", Usual(0.9, 0.25)));      // (1.0-0.9)/0.25 = 0.4

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Meters.Should().HaveCount(3);
        result.Meters[0].Stat.Should().Be("goldLeadAt10");
        // Remaining stats ordered by |score| descending: csAt10 (1.375), deathsBefore10 (0.571) —
        // both beat killParticipation (0.25) and visionPerMin (0.4), so only the top two make the cut.
        result.Meters[1].Stat.Should().Be("csAt10");
        result.Meters[2].Stat.Should().Be("deathsBefore10");
    }

    [Fact]
    public void Compute_None_OrdersAllMeters_ByAbsoluteScore()
    {
        var input = MakeInput(killParticipation: 62, visionPerMin: 0.95, deathsBefore10: 1, gameDurationSec: 1800, teamKills: 20);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8)),      // 0.5
            ("visionPerMin", Usual(0.9, 0.25)),       // 0.2
            ("deathsBefore10", Usual(0.4, 0.7)));     // (1-0.4)/0.7=0.857, flipped → -0.857

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Outcome.Should().Be("none");
        result.Meters.Select(m => m.Stat).Should().ContainInOrder("deathsBefore10", "killParticipation", "visionPerMin");
    }

    // ───────────────────────── FR8: fix ─────────────────────────

    [Fact]
    public void Compute_Fix_IsNull_WhenNoStatFallsToOrBelowThreshold()
    {
        var input = MakeInput(killParticipation: 62, visionPerMin: 1.0, gameDurationSec: 1800, teamKills: 20);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8)),     // 0.5
            ("visionPerMin", Usual(0.9, 0.25)));     // 0.4

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Fix.Should().BeNull();
    }

    [Fact]
    public void Compute_Fix_IsSet_AtExactlyMinusPointFive()
    {
        var input = MakeInput(killParticipation: 54, visionPerMin: 1.0, gameDurationSec: 1800, teamKills: 20);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8)),     // (54-58)/8 = -0.5
            ("visionPerMin", Usual(0.9, 0.25)));     // 0.4

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Fix.Should().NotBeNull();
        result.Fix!.Stat.Should().Be("killParticipation");
        result.Fix.Score.Should().Be(-0.5);
    }

    [Fact]
    public void Compute_Fix_CanBeTheDecidingStat()
    {
        var input = MakeInput(deathsBefore10: 3, killParticipation: 20, gameDurationSec: 1800, teamKills: 20, win: false);
        var usuals = Usuals(
            ("deathsBefore10", Usual(0.4, 0.7)),
            ("killParticipation", Usual(58, 8)));    // worst score → deciding stat and fix

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.Stat.Should().Be("killParticipation");
        result.Fix.Should().NotBeNull();
        result.Fix!.Stat.Should().Be("killParticipation");
    }

    // ───────────────────────── usualMatches ─────────────────────────

    [Fact]
    public void Compute_UsualMatches_IsTheMaximumSampleAcrossStats()
    {
        var input = MakeInput(killParticipation: 62, visionPerMin: 1.0, gameDurationSec: 1800, teamKills: 20);
        var usuals = Usuals(
            ("killParticipation", Usual(58, 8, matches: 12)),
            ("visionPerMin", Usual(0.9, 0.25, matches: 20)));

        var result = DecidingStatCalculator.Compute(input, usuals);

        result.Should().NotBeNull();
        result!.UsualMatches.Should().Be(20);
    }
}
