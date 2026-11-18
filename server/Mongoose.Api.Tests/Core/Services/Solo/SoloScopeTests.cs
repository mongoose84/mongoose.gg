using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloScopeTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("ranked_solo", "ranked_solo")]
    [InlineData(" Ranked_Flex ", "ranked_flex")]
    [InlineData("ALL", "all")]
    public void TryParseQueue_AcceptsSoloQueues_AndMissingValue(string? raw, string? expected)
    {
        SoloScope.TryParseQueue(raw, out var queue).Should().BeTrue();
        queue.Should().Be(expected);
    }

    [Theory]
    [InlineData("aram")]
    [InlineData("normal")]
    [InlineData("ranked_only")]
    public void TryParseQueue_RejectsOtherQueues(string raw)
    {
        SoloScope.TryParseQueue(raw, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, SoloRange.Last20)]
    [InlineData("last20", SoloRange.Last20)]
    [InlineData("LAST50", SoloRange.Last50)]
    [InlineData("season", SoloRange.Season)]
    public void TryParseRange_AcceptsRanges_DefaultingToLast20(string? raw, SoloRange expected)
    {
        SoloScope.TryParseRange(raw, out var range).Should().BeTrue();
        range.Should().Be(expected);
    }

    [Theory]
    [InlineData("last100")]
    [InlineData("30d")]
    public void TryParseRange_RejectsOtherValues(string raw)
    {
        SoloScope.TryParseRange(raw, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(3, 7, "ranked_solo")]
    [InlineData(0, 7, "ranked_flex")]
    [InlineData(0, 0, "all")]
    public void DefaultQueue_PrefersSoloThenFlexThenAll(int solo, int flex, string expected)
    {
        SoloScope.DefaultQueue(new SoloQueueCounts(solo, flex)).Should().Be(expected);
    }

    [Theory]
    [InlineData(SoloRange.Last20, 20)]
    [InlineData(SoloRange.Last50, 50)]
    [InlineData(SoloRange.Season, null)]
    public void RangeLimit_CountsMatches(SoloRange range, int? expected)
    {
        SoloScope.RangeLimit(range).Should().Be(expected);
    }
}
