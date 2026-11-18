using FluentAssertions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for SeasonHelper — verifies season code resolution, start date calculation,
/// and database upsert behavior used during match ingestion.
/// </summary>
public class SeasonHelperTests
{
    #region GetSeasonCodeFromPatch

    [Theory]
    [InlineData("15.3", "S15")]
    [InlineData("16.1", "S16")]
    [InlineData("14.24", "S14")]
    [InlineData("13.1", "S13")]
    public void GetSeasonCodeFromPatch_ReturnsCorrectCode_ForValidPatch(string patch, string expected)
    {
        SeasonHelper.GetSeasonCodeFromPatch(patch).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData(".5")]
    public void GetSeasonCodeFromPatch_ReturnsNull_ForInvalidPatch(string? patch)
    {
        SeasonHelper.GetSeasonCodeFromPatch(patch).Should().BeNull();
    }

    [Fact]
    public void GetSeasonCodeFromPatch_ExtractsMajorVersionOnly()
    {
        // Minor version should not appear in the season code
        SeasonHelper.GetSeasonCodeFromPatch("15.99").Should().Be("S15");
    }

    #endregion

    #region GetSeasonStartDate

    [Theory]
    [InlineData(14, 2024)]
    [InlineData(15, 2025)]
    [InlineData(16, 2026)]
    public void GetSeasonStartDate_ReturnsJanuaryDate_ForSeasonNumber(int majorVersion, int expectedYear)
    {
        var date = SeasonHelper.GetSeasonStartDate(majorVersion);

        date.Year.Should().Be(expectedYear);
        date.Month.Should().Be(1); // seasons start in January
    }

    [Fact]
    public void GetSeasonStartDate_ReturnsDayInEarlyJanuary()
    {
        // All seasons start in early January (historical pattern)
        var date = SeasonHelper.GetSeasonStartDate(15);

        date.Day.Should().BeGreaterThanOrEqualTo(1);
        date.Day.Should().BeLessThanOrEqualTo(15);
    }

    #endregion

    #region EnsureSeasonExistsAsync

    [Fact]
    public async Task EnsureSeasonExistsAsync_ReusesExistingSeason_WithoutInsert()
    {
        // Arrange — season S15 already exists
        var repo = new FakeSeasonsRepository();
        repo.Seed(new Season
        {
            SeasonCode = "S15",
            PatchVersion = "15.1",
            StartDate = new DateOnly(2025, 1, 8),
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.3", gameStartTimestamp: 0);

        // Assert
        result.Should().Be("S15");
        repo.UpsertCallCount.Should().Be(0); // no new row should be inserted
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_InsertsNewSeason_WhenMissing()
    {
        // Arrange — empty seasons table
        var repo = new FakeSeasonsRepository();

        // Act
        var result = await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.3", gameStartTimestamp: 0);

        // Assert
        result.Should().Be("S15");
        repo.UpsertCallCount.Should().Be(1);
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_ReturnsNull_ForNullPatch()
    {
        var repo = new FakeSeasonsRepository();

        var result = await SeasonHelper.EnsureSeasonExistsAsync(repo, null, gameStartTimestamp: 0);

        result.Should().BeNull();
        repo.UpsertCallCount.Should().Be(0);
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_ReturnsNull_ForEmptyPatch()
    {
        var repo = new FakeSeasonsRepository();

        var result = await SeasonHelper.EnsureSeasonExistsAsync(repo, string.Empty, gameStartTimestamp: 0);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_InsertedSeason_HasUtcCreatedAt()
    {
        // Arrange
        var repo = new FakeSeasonsRepository();
        var before = DateTime.UtcNow;

        // Act
        await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.1", gameStartTimestamp: 0);

        // Assert
        var after = DateTime.UtcNow;
        var inserted = repo.LastUpserted;
        inserted.Should().NotBeNull();
        inserted!.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        inserted.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_InsertedSeason_HasCorrectStartDate()
    {
        var repo = new FakeSeasonsRepository();

        await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.1", gameStartTimestamp: 0);

        repo.LastUpserted!.StartDate.Year.Should().Be(2025);
        repo.LastUpserted.StartDate.Month.Should().Be(1);
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_InsertedSeason_HasNoEndDate()
    {
        // Current season has no end date
        var repo = new FakeSeasonsRepository();

        await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.1", gameStartTimestamp: 0);

        repo.LastUpserted!.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task EnsureSeasonExistsAsync_InsertedSeason_StoresPatchVersion()
    {
        var repo = new FakeSeasonsRepository();

        await SeasonHelper.EnsureSeasonExistsAsync(repo, "15.3", gameStartTimestamp: 0);

        repo.LastUpserted!.PatchVersion.Should().Be("15.3");
    }

    #endregion

    // ---- in-test fake ----

    internal sealed class FakeSeasonsRepository : ISeasonsRepository
    {
        private readonly Dictionary<string, Season> _seasons = new(StringComparer.OrdinalIgnoreCase);

        public int UpsertCallCount { get; private set; }
        public Season? LastUpserted { get; private set; }

        public void Seed(Season season) => _seasons[season.SeasonCode] = season;

        public Task UpsertAsync(Season season)
        {
            UpsertCallCount++;
            LastUpserted = season;
            _seasons[season.SeasonCode] = season;
            return Task.CompletedTask;
        }

        public Task<Season?> GetByCodeAsync(string seasonCode)
        {
            _seasons.TryGetValue(seasonCode, out var season);
            return Task.FromResult(season);
        }
    }
}
