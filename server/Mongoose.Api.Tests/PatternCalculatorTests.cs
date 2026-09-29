using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class PatternCalculatorTests
{
    // ───────────────────────── Helpers ─────────────────────────

    private const int DurationSec = 1800;
    private const long MinuteMs = 60 * 1000;
    private const long DurationMs = DurationSec * 1000L;

    /// <summary>
    /// Back-to-back sessions: each inner list is one session's results, matches 5 minutes apart,
    /// sessions a day apart.
    /// </summary>
    private static List<SoloMatchRow> Sessions(params bool[][] sessions)
    {
        var rows = new List<SoloMatchRow>();
        for (var s = 0; s < sessions.Length; s++)
        {
            var start = SoloRows.BaseStartMs + s * 24 * 60 * MinuteMs;
            foreach (var win in sessions[s])
            {
                rows.Add(SoloRows.Make(rows.Count, win: win, durationSec: DurationSec, startMs: start));
                start += DurationMs + 5 * MinuteMs;
            }
        }
        return rows;
    }

    private static List<SoloMatchRow> Lengths(params (int DurationSec, bool Win)[] matches)
        => matches.Select((m, i) => SoloRows.Make(i, win: m.Win, durationSec: m.DurationSec)).ToList();

    private static IEnumerable<(int, bool)> Repeat(int count, int durationSec, bool win)
        => Enumerable.Repeat((durationSec, win), count);

    // ───────────────────────── FR26: sessions ─────────────────────────

    [Theory]
    [InlineData(29, 1)]
    [InlineData(30, 2)]
    public void GroupSessions_MeasuresTheGapFromTheEndOfTheLastMatch(int gapMinutes, int expectedSessions)
    {
        var first = SoloRows.Make(0, durationSec: DurationSec, startMs: SoloRows.BaseStartMs);
        var second = SoloRows.Make(1, startMs: SoloRows.BaseStartMs + DurationMs + gapMinutes * MinuteMs);

        PatternCalculator.GroupSessions([second, first]).Should().HaveCount(expectedSessions);
    }

    [Fact]
    public void Session_FindsWeakSpot_AtFourthMatchAndLater()
    {
        bool[] session = [true, true, true, false];

        var pattern = PatternCalculator.Calculate(Sessions(session, session, session)).Session;

        pattern.Should().NotBeNull();
        pattern!.Groups.Should().Equal(
            new PatternGroup("1", 3, 100),
            new PatternGroup("2", 3, 100),
            new PatternGroup("3", 3, 100),
            new PatternGroup("4plus", 3, 0));
        pattern.Weak.Should().Be("4plus");
    }

    [Fact]
    public void Session_IsNull_WhenEveryMatchIsItsOwnSession()
    {
        PatternCalculator.Calculate(SoloRows.Many(20)).Session.Should().BeNull();
    }

    // ───────────────────────── FR27: after a loss ─────────────────────────

    [Fact]
    public void AfterLoss_PairsMatchesWithinSessionsOnly()
    {
        var pattern = PatternCalculator.Calculate(Sessions(
            [true, true], [true, true], [true, true], [true, false], [true, false],
            [false, true], [false, false], [false, false], [false, false], [false, false])).AfterLoss;

        pattern.Should().NotBeNull();
        pattern!.AfterWin.Should().Be(new AfterResultGroup(5, 60));
        pattern.AfterLoss.Should().Be(new AfterResultGroup(5, 20));
    }

    [Fact]
    public void AfterLoss_IsNull_WithFewerThanFivePairsOnASide()
    {
        var pattern = PatternCalculator.Calculate(Sessions(
            [true, true], [true, true], [true, true], [true, false], [true, false],
            [false, true], [false, false], [false, false], [false, false])).AfterLoss;

        pattern.Should().BeNull();
    }

    // ───────────────────────── FR28: match length ─────────────────────────

    [Fact]
    public void Length_BucketsAt25And35Minutes()
    {
        var rows = Lengths([
            .. Repeat(3, 1499, true),
            .. Repeat(3, 1500, true),
            .. Repeat(3, 2100, true),
            .. Repeat(3, 2101, true)]);

        var pattern = PatternCalculator.Calculate(rows).Length;

        pattern!.Groups.Select(g => (g.Key, g.Matches)).Should().Equal(("under25", 3), ("25to35", 6), ("over35", 3));
        pattern.Weak.Should().BeNull();
    }

    [Fact]
    public void Length_LeavesOutGroupsUnderThreeMatches_AndNeedsTwoGroups()
    {
        var rows = Lengths([.. Repeat(5, 1800, true), .. Repeat(2, 2400, false)]);

        PatternCalculator.Calculate(rows).Length.Should().BeNull();
    }

    [Fact]
    public void Length_WeakSpot_NeedsFifteenPointsBelowTheRest()
    {
        var weak = Lengths([.. Repeat(2, 1200, true), .. Repeat(1, 1200, false), .. Repeat(3, 1800, true)]);
        var close = Lengths([.. Repeat(8, 1200, true), .. Repeat(1, 1200, false), .. Repeat(3, 1800, true)]);

        PatternCalculator.Calculate(weak).Length!.Weak.Should().Be("under25");   // 67% vs 100%
        PatternCalculator.Calculate(close).Length!.Weak.Should().BeNull();       // 89% vs 100%
    }

    [Fact]
    public void Length_WeakSpot_IsNull_WhenTwoGroupsTieForLowest()
    {
        var rows = Lengths([.. Repeat(3, 1200, false), .. Repeat(6, 1800, true), .. Repeat(3, 2400, false)]);

        PatternCalculator.Calculate(rows).Length!.Weak.Should().BeNull();
    }
}
