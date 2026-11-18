using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloWinFactorsEndpointTests
{
    private const string Route = "/api/v2/solo/win-factors";
    private const string Puuid = "test-puuid-primary";

    // ───────────────────────── Helpers ─────────────────────────

    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    private static void LinkPrimaryAccount(TestWebApplicationFactory factory)
    {
        factory.RiotAccountsRepository.AddRiotAccount(1, Puuid, "MainPlayer", "EUW1", "MainPlayer#EUW", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, Puuid, isPrimary: true);
    }

    private static async Task<HttpResponseMessage> GetAsync(TestWebApplicationFactory factory, string authCookie, string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Add("Cookie", authCookie);
        return await client.SendAsync(req);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// 10 matches. Low deaths: hit 80%, miss 20%. Vision: hit 40%, miss 60%. Every match is its own session.
    /// </summary>
    private static List<Core.QueryModels.SoloMatchRow> FactorRows(string secondRole = "MIDDLE")
        => SoloRows.Many(10, i => SoloRows.Make(
            i,
            win: i is 0 or 1 or 2 or 3 or 5,
            deaths: i < 5 ? 2 : 8,
            role: i == 9 ? secondRole : "MIDDLE",
            visionPerMin: i % 2 == 0 ? 1.0 : 0.5));

    // ───────────────────────── Auth and validation ─────────────────────────

    [Fact]
    public async Task GetWinFactors_Returns401_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWinFactors_Returns403_WhenAccessingOtherUsersData()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetWinFactors_Returns404_WhenNoRiotAccountLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("RIOT_ACCOUNT_NOT_FOUND");
    }

    [Theory]
    [InlineData("?queueType=normal", "INVALID_QUEUE")]
    [InlineData("?range=7d", "INVALID_RANGE")]
    public async Task GetWinFactors_Returns400_ForInvalidParameters(string query, string code)
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be(code);
    }

    // ───────────────────────── Responses ─────────────────────────

    [Fact]
    public async Task GetWinFactors_Returns200WithZeroMatches_WhenNoMatchesInScope()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(0);
        root.GetProperty("queueType").GetString().Should().Be("ranked_solo");
        root.GetProperty("factors").GetArrayLength().Should().Be(0);
        var patterns = root.GetProperty("patterns");
        patterns.GetProperty("session").ValueKind.Should().Be(JsonValueKind.Null);
        patterns.GetProperty("afterLoss").ValueKind.Should().Be(JsonValueKind.Null);
        patterns.GetProperty("length").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetWinFactors_ReturnsFactorsSortedByGap_WithRoleMark()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, FactorRows());

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=last20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(10);
        root.GetProperty("range").GetString().Should().Be("last20");

        var factors = root.GetProperty("factors");
        factors.GetArrayLength().Should().Be(2);
        factors[0].GetProperty("key").GetString().Should().Be("lowDeaths");
        factors[0].GetProperty("hitWinRate").GetInt32().Should().Be(80);
        factors[0].GetProperty("missWinRate").GetInt32().Should().Be(20);
        factors[0].GetProperty("hitMatches").GetInt32().Should().Be(5);
        factors[0].GetProperty("missMatches").GetInt32().Should().Be(5);
        factors[0].GetProperty("gap").GetInt32().Should().Be(60);
        factors[0].GetProperty("mark").ValueKind.Should().Be(JsonValueKind.Null);
        factors[1].GetProperty("key").GetString().Should().Be("vision");
        factors[1].GetProperty("gap").GetInt32().Should().Be(-20);
        factors[1].GetProperty("mark").GetDouble().Should().Be(0.9);
    }

    [Fact]
    public async Task GetWinFactors_LeavesOutRoleMark_WhenRangeHasMoreThanOneRole()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, FactorRows(secondRole: "TOP"));

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo"));

        var vision = root.GetProperty("factors").EnumerateArray().Single(f => f.GetProperty("key").GetString() == "vision");
        vision.GetProperty("mark").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetWinFactors_ReturnsPatterns()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        // Three sessions of four back-to-back matches; the fourth match of each is lost.
        const long matchGapMs = (30 + 5) * 60 * 1000;
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(12, i => SoloRows.Make(
            i,
            win: i % 4 != 3,
            startMs: SoloRows.BaseStartMs + i / 4 * SoloRows.TwoHoursMs * 12 + i % 4 * matchGapMs)));

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo"));

        var patterns = root.GetProperty("patterns");
        var session = patterns.GetProperty("session");
        session.GetProperty("groups").EnumerateArray().Select(g => g.GetProperty("key").GetString())
            .Should().Equal("1", "2", "3", "4plus");
        session.GetProperty("groups")[3].GetProperty("matches").GetInt32().Should().Be(3);
        session.GetProperty("groups")[3].GetProperty("winRate").GetInt32().Should().Be(0);
        session.GetProperty("weak").GetString().Should().Be("4plus");

        // Nine pairs follow a win and none a loss, so there is no after-a-loss card; every match is 30 minutes.
        patterns.GetProperty("afterLoss").ValueKind.Should().Be(JsonValueKind.Null);
        patterns.GetProperty("length").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
