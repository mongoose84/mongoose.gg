using FluentAssertions;
using Mongoose.Api.Infrastructure.Helpers;
using Xunit;

namespace Mongoose.Api.Tests;

public class LeagueDataHelperTests
{
    // ─────────────── GetQueueLabel ───────────────

    [Theory]
    [InlineData(420, "Ranked Solo/Duo")]
    [InlineData(440, "Ranked Flex")]
    [InlineData(400, "Normal Draft")]
    [InlineData(430, "Normal Blind")]
    [InlineData(450, "ARAM")]
    [InlineData(900, "ARURF")]
    [InlineData(1700, "Arena")]
    [InlineData(830, "Co-op vs AI")]
    [InlineData(700, "Clash")]
    public void GetQueueLabel_ReturnsExpectedLabel(int queueId, string expected)
    {
        LeagueDataHelper.GetQueueLabel(queueId).Should().Be(expected);
    }

    [Fact]
    public void GetQueueLabel_ReturnsFallback_ForUnknownQueueId()
    {
        LeagueDataHelper.GetQueueLabel(0).Should().Be("Queue 0");
    }

    [Fact]
    public void GetQueueLabel_ReturnsFallback_WithQueueIdInText()
    {
        LeagueDataHelper.GetQueueLabel(9999).Should().Be("Queue 9999");
    }

    // ─────────────── GetQueueLabelShort ───────────────

    [Fact]
    public void GetQueueLabelShort_Returns_RankedSolo_For420()
    {
        LeagueDataHelper.GetQueueLabelShort(420).Should().Be("Ranked Solo");
    }

    [Fact]
    public void GetQueueLabelShort_Returns_RankedFlex_For440()
    {
        LeagueDataHelper.GetQueueLabelShort(440).Should().Be("Ranked Flex");
    }

    [Fact]
    public void GetQueueLabelShort_FallsBackToGetQueueLabel_ForAram()
    {
        LeagueDataHelper.GetQueueLabelShort(450).Should().Be(LeagueDataHelper.GetQueueLabel(450));
    }

    [Fact]
    public void GetQueueLabelShort_FallsBackToGetQueueLabel_ForUnknownId()
    {
        LeagueDataHelper.GetQueueLabelShort(0).Should().Be("Queue 0");
    }

    // ─────────────── NormalizeChampionName ───────────────

    [Theory]
    [InlineData("Cho'Gath", "ChoGath")]
    [InlineData("Lee Sin", "LeeSin")]
    [InlineData("Wukong", "Wukong")]
    [InlineData("Nunu & Willump", "NunuWillump")]
    [InlineData("Dr. Mundo", "DrMundo")]
    [InlineData("Kai'Sa", "KaiSa")]
    public void NormalizeChampionName_RemovesNonAlphanumericCharacters(string input, string expected)
    {
        LeagueDataHelper.NormalizeChampionName(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeChampionName_ReturnsEmpty_ForEmptyString()
    {
        LeagueDataHelper.NormalizeChampionName("").Should().BeEmpty();
    }

    [Fact]
    public void NormalizeChampionName_ReturnsEmpty_ForNull()
    {
        LeagueDataHelper.NormalizeChampionName(null!).Should().BeEmpty();
    }

    // ─────────────── GetChampionIconUrl ───────────────

    [Fact]
    public void GetChampionIconUrl_ReturnsUrlContainingChampionName()
    {
        var url = LeagueDataHelper.GetChampionIconUrl("Wukong");

        url.Should().Contain("Wukong");
    }

    [Fact]
    public void GetChampionIconUrl_StartsWithDataDragonBaseUrl()
    {
        var url = LeagueDataHelper.GetChampionIconUrl("Wukong");

        url.Should().StartWith("https://ddragon.leagueoflegends.com/cdn/");
    }

    [Fact]
    public void GetChampionIconUrl_ContainsDataDragonVersion()
    {
        var url = LeagueDataHelper.GetChampionIconUrl("Wukong");

        url.Should().Contain(LeagueDataHelper.DataDragonVersion);
    }

    [Fact]
    public void GetChampionIconUrl_NormalizesChampionName_ForChoGath()
    {
        var url = LeagueDataHelper.GetChampionIconUrl("Cho'Gath");

        url.Should().Contain("ChoGath");
        url.Should().NotContain("'");
    }

    [Fact]
    public void GetChampionIconUrl_EndsWithPngExtension()
    {
        var url = LeagueDataHelper.GetChampionIconUrl("Lux");

        url.Should().EndWith(".png");
    }
}
