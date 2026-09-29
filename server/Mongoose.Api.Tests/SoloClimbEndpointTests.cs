using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloClimbEndpointTests
{
    private const string Route = "/api/v2/solo/climb";
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

    // ───────────────────────── Auth and validation ─────────────────────────

    [Fact]
    public async Task GetClimb_Returns401_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClimb_Returns403_WhenAccessingOtherUsersData()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetClimb_Returns404_WhenNoRiotAccountLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("RIOT_ACCOUNT_NOT_FOUND");
    }

    [Theory]
    [InlineData("?queueType=aram", "INVALID_QUEUE")]
    [InlineData("?range=all", "INVALID_RANGE")]
    public async Task GetClimb_Returns400_ForInvalidParameters(string query, string code)
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
    public async Task GetClimb_Returns200WithZeroMatches_InWinRateMode()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(0);
        root.GetProperty("mode").GetString().Should().Be("winRate");
        root.GetProperty("lp").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("winRate").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("champions").GetArrayLength().Should().Be(0);
        root.GetProperty("rank").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetClimb_ReturnsTheLadder_InLpMode()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        // 12 ranked wins of +20 from Emerald III 70: promoted at the 2nd, 7th and 12th match
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(12, i =>
        {
            var rank = LpLadder.FromScore(2170 + (i + 1) * 20);
            return SoloRows.Make(i, win: true, tierAfter: rank.Tier, rankAfter: rank.Division, lpAfter: rank.Lp, lpChange: 20);
        }));

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("mode").GetString().Should().Be("lp");
        root.GetProperty("wins").GetInt32().Should().Be(12);
        root.GetProperty("winRate").ValueKind.Should().Be(JsonValueKind.Null);

        var lp = root.GetProperty("lp");
        lp.GetProperty("net").GetInt32().Should().Be(240);
        lp.GetProperty("start").GetProperty("division").GetString().Should().Be("III");
        lp.GetProperty("end").GetProperty("tier").GetString().Should().Be("DIAMOND");
        lp.GetProperty("end").GetProperty("division").GetString().Should().Be("IV");
        lp.GetProperty("points").GetArrayLength().Should().Be(12);
        lp.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("index").GetInt32()).Should().Equal(1, 6, 11);
        lp.GetProperty("events")[0].GetProperty("kind").GetString().Should().Be("promotion");
        lp.GetProperty("biggestDrop").ValueKind.Should().Be(JsonValueKind.Null);

        var champion = root.GetProperty("champions")[0];
        champion.GetProperty("championName").GetString().Should().Be("Ahri");
        champion.GetProperty("value").GetInt32().Should().Be(240);
        root.GetProperty("rank").GetProperty("lp").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetClimb_FallsBackToWinRate_ForAllQueues()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(20, i => SoloRows.Make(i, win: i % 2 == 0, lpChange: i % 2 == 0 ? 20 : -20)));

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=all"));

        root.GetProperty("mode").GetString().Should().Be("winRate");
        root.GetProperty("lp").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("winRate").GetProperty("was").GetInt32().Should().Be(50);
        root.GetProperty("winRate").GetProperty("points").GetArrayLength().Should().Be(11);
        root.GetProperty("champions")[0].GetProperty("value").GetInt32().Should().Be(0);
        root.GetProperty("rank").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
