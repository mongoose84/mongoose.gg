using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database;
using MySqlConnector;
using Xunit;

namespace Mongoose.Api.Tests;

public class QueryFilterBuilderTests
{
    // Minimal stub — the pure methods under test never call the DB.
    private sealed class StubDbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection() =>
            throw new NotImplementedException("Not used in pure-method tests.");

        public Task<MySqlConnection> CreateOpenConnectionAsync() =>
            throw new NotImplementedException("Not used in pure-method tests.");
    }

    private static QueryFilterBuilder CreateSut() =>
        new(new StubDbConnectionFactory(),
            NullLogger<QueryFilterBuilder>.Instance);

    // ─────────────── ValidateQueueType ───────────────

    [Fact]
    public void ValidateQueueType_ReturnsAll_WhenNull()
    {
        var sut = CreateSut();
        sut.ValidateQueueType(null).Should().Be("all");
    }

    [Fact]
    public void ValidateQueueType_ReturnsAll_WhenExplicitlyAll()
    {
        var sut = CreateSut();
        sut.ValidateQueueType("all").Should().Be("all");
    }

    [Theory]
    [InlineData("ranked_solo")]
    [InlineData("ranked_flex")]
    [InlineData("normal")]
    [InlineData("aram")]
    public void ValidateQueueType_ReturnsNormalizedValue_ForValidInputs(string input)
    {
        var sut = CreateSut();
        sut.ValidateQueueType(input).Should().Be(input);
    }

    [Fact]
    public void ValidateQueueType_ReturnsAll_ForUnknownValue()
    {
        var sut = CreateSut();
        sut.ValidateQueueType("unknown_queue").Should().Be("all");
    }

    [Theory]
    [InlineData("RANKED_SOLO", "ranked_solo")]
    [InlineData("RANKED_FLEX", "ranked_flex")]
    [InlineData("ARAM", "aram")]
    [InlineData("ALL", "all")]
    public void ValidateQueueType_IsCaseInsensitive(string input, string expected)
    {
        var sut = CreateSut();
        sut.ValidateQueueType(input).Should().Be(expected);
    }

    // ─────────────── BuildQueueFilter ───────────────

    [Fact]
    public void BuildQueueFilter_ReturnsRankedSoloFilter()
    {
        var sut = CreateSut();
        sut.BuildQueueFilter("ranked_solo").Should().Be("AND m.queue_id = 420");
    }

    [Fact]
    public void BuildQueueFilter_ReturnsRankedFlexFilter()
    {
        var sut = CreateSut();
        sut.BuildQueueFilter("ranked_flex").Should().Be("AND m.queue_id = 440");
    }

    [Fact]
    public void BuildQueueFilter_ReturnsNormalFilter()
    {
        var sut = CreateSut();
        sut.BuildQueueFilter("normal").Should().Be("AND m.queue_id IN (430, 400)");
    }

    [Fact]
    public void BuildQueueFilter_ReturnsAramFilter()
    {
        var sut = CreateSut();
        sut.BuildQueueFilter("aram").Should().Be("AND m.queue_id IN (450, 1700)");
    }

    [Fact]
    public void BuildQueueFilter_ReturnsEmptyString_WhenAll()
    {
        var sut = CreateSut();
        sut.BuildQueueFilter("all").Should().BeEmpty();
    }

    // ─────────────── BuildTimeRangeFilter ───────────────

    [Fact]
    public void BuildTimeRangeFilter_ReturnsSeasonCodeFilter_WhenCurrentSeasonWithSeasonCode()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, "S2025", "current_season");

        sut.BuildTimeRangeFilter(filter).Should().Be("AND m.season_code = @season");
    }

    [Fact]
    public void BuildTimeRangeFilter_ReturnsSeasonCodeFilter_WhenLastSeasonWithSeasonCode()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, "S2024", "last_season");

        sut.BuildTimeRangeFilter(filter).Should().Be("AND m.season_code = @season");
    }

    [Fact]
    public void BuildTimeRangeFilter_ReturnsImpossibleFilter_WhenCurrentSeasonButNoSeasonCode()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, null, "current_season");

        sut.BuildTimeRangeFilter(filter).Should().Be("AND 1=0");
    }

    [Fact]
    public void BuildTimeRangeFilter_ReturnsImpossibleFilter_WhenCurrentSeasonButEmptySeasonCode()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, "", "current_season");

        sut.BuildTimeRangeFilter(filter).Should().Be("AND 1=0");
    }

    [Fact]
    public void BuildTimeRangeFilter_ReturnsStartTimeFilter_WhenTimeRangeStartIsSet()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(DateTime.UtcNow.AddDays(-7), null, "1w");

        sut.BuildTimeRangeFilter(filter).Should().Be("AND m.game_start_time >= @startTime");
    }

    [Fact]
    public void BuildTimeRangeFilter_ReturnsEmptyString_WhenAllTimeAndNoStartTime()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, null, "all");

        sut.BuildTimeRangeFilter(filter).Should().BeEmpty();
    }

    // ─────────────── AddTimeRangeParameters ───────────────

    [Fact]
    public void AddTimeRangeParameters_DoesNotThrow_WhenBothTimeRangeStartAndSeasonCodeAreSet()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(DateTime.UtcNow.AddDays(-7), "S2025", "1w");
        using var cmd = new MySqlCommand();

        var act = () => sut.AddTimeRangeParameters(cmd, filter);

        act.Should().NotThrow();
        cmd.Parameters.Contains("@startTime").Should().BeTrue();
        cmd.Parameters.Contains("@season").Should().BeTrue();
    }

    [Fact]
    public void AddTimeRangeParameters_AddsOnlyStartTime_WhenOnlyTimeRangeStartIsSet()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(DateTime.UtcNow.AddDays(-30), null, "1m");
        using var cmd = new MySqlCommand();

        sut.AddTimeRangeParameters(cmd, filter);

        cmd.Parameters.Contains("@startTime").Should().BeTrue();
        cmd.Parameters.Contains("@season").Should().BeFalse();
    }

    [Fact]
    public void AddTimeRangeParameters_AddsOnlySeason_WhenOnlySeasonCodeIsSet()
    {
        var sut = CreateSut();
        var filter = new TimeRangeFilter(null, "S2025", "current_season");
        using var cmd = new MySqlCommand();

        sut.AddTimeRangeParameters(cmd, filter);

        cmd.Parameters.Contains("@startTime").Should().BeFalse();
        cmd.Parameters.Contains("@season").Should().BeTrue();
    }
}
