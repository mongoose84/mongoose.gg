using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.QueryModels;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloStatTrendsEndpointTests
{
    private const string Route = "/api/v2/solo/stat-trends";
    private const string Puuid = "test-puuid-primary";
    private const string AltPuuid = "test-puuid-alt";

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
    public async Task GetStatTrends_Returns401_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatTrends_Returns403_WhenAccessingOtherUsersData()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStatTrends_Returns404_WhenNoRiotAccountLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("RIOT_ACCOUNT_NOT_FOUND");
    }

    [Theory]
    [InlineData("?queueType=aram", "INVALID_QUEUE")]
    [InlineData("?range=last100", "INVALID_RANGE")]
    public async Task GetStatTrends_Returns400_ForInvalidParameters(string query, string code)
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be(code);
        factory.SoloTrendsRepository.RowQueries.Should().BeEmpty();
    }

    // ───────────────────────── Responses ─────────────────────────

    [Fact]
    public async Task GetStatTrends_Returns200WithZeroMatches_WhenNoMatchesInScope()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(0);
        root.GetProperty("queueType").GetString().Should().Be("all");
        root.GetProperty("range").GetString().Should().Be("last20");
        root.GetProperty("stats").GetArrayLength().Should().Be(6);
        root.GetProperty("stats")[0].GetProperty("verdict").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("focus").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetStatTrends_ReturnsTrendsAndFocus()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        // Vision drops from 1.2 to 0.6 and wins follow it; deaths drop from 6 to 4.
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(20, i => SoloRows.Make(
            i,
            win: i < 10 ? i < 6 : i < 14,
            deaths: i < 10 ? 6 : 4,
            visionPerMin: i < 10 ? 1.2 : 0.6)));

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=last20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(20);
        root.GetProperty("queueType").GetString().Should().Be("ranked_solo");

        var deaths = root.GetProperty("stats")[0];
        deaths.GetProperty("key").GetString().Should().Be("deaths");
        deaths.GetProperty("values").GetArrayLength().Should().Be(20);
        deaths.GetProperty("rolling")[0].GetProperty("index").GetInt32().Should().Be(9);
        deaths.GetProperty("was").GetDouble().Should().Be(6);
        deaths.GetProperty("now").GetDouble().Should().Be(4);
        deaths.GetProperty("count").GetInt32().Should().Be(20);
        deaths.GetProperty("verdict").GetString().Should().Be("improving");
        deaths.GetProperty("benchmark").GetProperty("kind").GetString().Should().Be("season");
        deaths.GetProperty("benchmark").GetProperty("value").GetDouble().Should().Be(5);

        var focus = root.GetProperty("focus");
        focus.GetProperty("stat").GetString().Should().Be("visionPerMin");
        focus.GetProperty("factor").GetString().Should().Be("vision");
        focus.GetProperty("mark").GetDouble().Should().Be(0.9);
        focus.GetProperty("hitWinRate").GetInt32().Should().Be(60);
        focus.GetProperty("missWinRate").GetInt32().Should().Be(40);
        focus.GetProperty("last20").GetArrayLength().Should().Be(20);
        focus.GetProperty("last20")[0].GetString().Should().Be("hit");
        focus.GetProperty("hits").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetStatTrends_OmitsValues_WhenSeasonHasMoreThan100Matches()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(120));

        var response = await GetAsync(factory, authCookie, $"{Route}/1?range=season");

        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(120);
        var deaths = root.GetProperty("stats")[0];
        deaths.GetProperty("values").ValueKind.Should().Be(JsonValueKind.Null);
        deaths.GetProperty("rolling").GetArrayLength().Should().Be(100);
    }

    // ───────────────────────── Scope ─────────────────────────

    [Fact]
    public async Task GetStatTrends_DefaultsToFlex_WhenNoSoloMatchThisSeason()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.Add(Puuid, SoloRows.Make(0, queueId: 420), inSeason: false);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(3, i => SoloRows.Make(i + 1, queueId: 440)));

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        var root = await ReadJsonAsync(response);
        root.GetProperty("queueType").GetString().Should().Be("ranked_flex");
        root.GetProperty("matches").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetStatTrends_ReadsSeasonForBenchmark_OnlyWhenRangeIsNotSeason()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=last50");
        await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=season");

        factory.SoloTrendsRepository.RowQueries.Select(q => q.Range)
            .Should().Equal(SoloRange.Last50, SoloRange.Season, SoloRange.Season);
    }

    // ───────────────────────── 5g: rank average ─────────────────────────

    /// <summary>Emerald mid matches from <paramref name="players"/> other players, 4 or 6 deaths each.</summary>
    private static void SeedRankPool(TestWebApplicationFactory factory, int matches, int players, string role = "MIDDLE")
    {
        factory.SoloTrendsRepository.RankPool.AddRange(Enumerable.Range(0, matches).Select(i => new SoloRankPoolRow(
            $"other-{i % players}",
            SoloRows.Make(1000 + i, role: role, tierAfter: "EMERALD", deaths: i % 2 == 0 ? 4 : 6))));
    }

    [Fact]
    public async Task GetStatTrends_UsesTheRankAverage_WhenThePoolIsLargeEnough()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(20, i => SoloRows.Make(i, tierAfter: "EMERALD", deaths: 9)));
        SeedRankPool(factory, matches: 200, players: 20);
        // The player's own pool rows are left out of their average
        factory.SoloTrendsRepository.RankPool.AddRange(SoloRows.Many(50, i => SoloRows.Make(2000 + i, tierAfter: "EMERALD", deaths: 20))
            .Select(r => new SoloRankPoolRow(Puuid, r)));

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=last20"));

        var deaths = root.GetProperty("stats").EnumerateArray().Single(s => s.GetProperty("key").GetString() == "deaths");
        var benchmark = deaths.GetProperty("benchmark");
        benchmark.GetProperty("kind").GetString().Should().Be("rank");
        benchmark.GetProperty("tier").GetString().Should().Be("EMERALD");
        benchmark.GetProperty("value").GetDouble().Should().Be(5);
        factory.SoloTrendsRepository.RankPoolQueries.Should().Equal((420, "EMERALD", "MIDDLE"));
    }

    [Fact]
    public async Task GetStatTrends_KeepsTheSeasonAverage_WhenThePoolIsTooSmall()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(20, i => SoloRows.Make(i, tierAfter: "EMERALD")));
        SeedRankPool(factory, matches: 400, players: 19);

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo&range=last20"));

        var benchmark = root.GetProperty("stats")[0].GetProperty("benchmark");
        benchmark.GetProperty("kind").GetString().Should().Be("season");
        benchmark.GetProperty("tier").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetStatTrends_DoesNotReadARankPool_ForAllQueues()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(20, i => SoloRows.Make(i, tierAfter: "EMERALD")));
        SeedRankPool(factory, matches: 200, players: 20);

        await GetAsync(factory, authCookie, $"{Route}/1?queueType=all&range=last20");

        factory.SoloTrendsRepository.RankPoolQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatTrends_ReadsEveryLinkedAccount_WhenAccountIdIsAll()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var user = await factory.UsersRepository.GetByIdAsync(1);
        user!.Tier = "pro";
        await factory.UsersRepository.UpsertAsync(user);

        LinkPrimaryAccount(factory);
        factory.RiotAccountsRepository.AddRiotAccount(1, AltPuuid, "AltPlayer", "EUW1", "AltPlayer#EUW", 101, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, AltPuuid, isPrimary: false);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(3));
        factory.SoloTrendsRepository.AddRange(AltPuuid, SoloRows.Many(2, i => SoloRows.Make(i + 10)));

        var primary = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1"));
        var all = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?accountId=all"));

        primary.GetProperty("matches").GetInt32().Should().Be(3);
        all.GetProperty("matches").GetInt32().Should().Be(5);
    }
}
