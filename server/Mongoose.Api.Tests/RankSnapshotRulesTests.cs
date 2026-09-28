using FluentAssertions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class RankSnapshotRulesTests
{
    private static RankSnapshotRecord Previous(int wins, int losses, int lp = 50, string tier = "GOLD", string division = "II") =>
        new() { Tier = tier, Division = division, Lp = lp, Wins = wins, Losses = losses };

    private static LeagueRankReading Reading(int wins, int losses, int lp = 50, string tier = "GOLD", string division = "II") =>
        new(420, tier, division, lp, wins, losses);

    [Fact]
    public void Classify_IsBaseline_WithoutAPreviousSnapshot()
    {
        RankSnapshotRules.Classify(null, Reading(10, 10)).Should().Be(RankReadingOutcome.Baseline);
    }

    [Theory]
    [InlineData(11, 10)]
    [InlineData(10, 11)]
    public void Classify_IsPending_WhenExactlyOneMatchWasPlayed(int wins, int losses)
    {
        RankSnapshotRules.Classify(Previous(10, 10), Reading(wins, losses, lp: 70)).Should().Be(RankReadingOutcome.Pending);
    }

    [Theory]
    [InlineData(12, 10)]   // two matches
    [InlineData(11, 11)]   // two matches
    [InlineData(0, 0)]     // season reset
    public void Classify_IsBaseline_WhenMoreThanOneMatchOrAReset(int wins, int losses)
    {
        RankSnapshotRules.Classify(Previous(10, 10), Reading(wins, losses)).Should().Be(RankReadingOutcome.Baseline);
    }

    [Fact]
    public void Classify_IsUnchanged_WhenNothingMoved()
    {
        RankSnapshotRules.Classify(Previous(10, 10), Reading(10, 10)).Should().Be(RankReadingOutcome.Unchanged);
    }

    [Theory]
    [InlineData(45, "GOLD", "II")]   // dodge or decay
    [InlineData(50, "GOLD", "III")]
    [InlineData(50, "SILVER", "II")]
    public void Classify_IsNoMatch_WhenTheRankMovedWithoutAMatch(int lp, string tier, string division)
    {
        RankSnapshotRules.Classify(Previous(10, 10), Reading(10, 10, lp, tier, division)).Should().Be(RankReadingOutcome.NoMatch);
    }

    [Fact]
    public void AttributionWindow_SpansBothReadings_WidenedByTheClockSkew()
    {
        var previous = new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc);
        var current = previous.AddMinutes(20);

        var (fromMs, toMs) = RankSnapshotRules.AttributionWindow(previous, current, TimeSpan.FromMinutes(2));

        fromMs.Should().Be(new DateTimeOffset(previous.AddMinutes(-2)).ToUnixTimeMilliseconds());
        toMs.Should().Be(new DateTimeOffset(current.AddMinutes(2)).ToUnixTimeMilliseconds());
    }

    [Fact]
    public void ChooseMatch_PicksOnlyASingleCandidate()
    {
        RankSnapshotRules.ChooseMatch(Array.Empty<string>()).Should().Be((MatchAttributionOutcome.NoCandidate, (string?)null));
        RankSnapshotRules.ChooseMatch(new[] { "EUW1_1" }).Should().Be((MatchAttributionOutcome.Single, "EUW1_1"));
        RankSnapshotRules.ChooseMatch(new[] { "EUW1_1", "EUW1_2" }).Should().Be((MatchAttributionOutcome.Ambiguous, (string?)null));
    }

    [Theory]
    [InlineData("RANKED_SOLO_5x5", 420)]
    [InlineData("RANKED_FLEX_SR", 440)]
    [InlineData("CHERRY", null)]
    [InlineData(null, null)]
    public void QueueIdFor_MapsOnlyRankedQueues(string? queueType, int? expected)
    {
        RankSnapshotRules.QueueIdFor(queueType).Should().Be(expected);
    }
}
