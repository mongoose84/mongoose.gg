using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Mongoose.Api.Infrastructure.Jobs;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloDeathZonesEndpointTests
{
    private const string Route = "/api/v2/solo/death-zones";
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
    /// <paramref name="matches"/> Solo/Duo matches, each with <paramref name="deathsPerMatch"/> deaths in
    /// the enemy bot jungle at 20:00, followed by an enemy dragon in every other match.
    /// </summary>
    private static void Seed(TestWebApplicationFactory factory, int matches, int deathsPerMatch, bool detailed = true)
    {
        var anchor = MapRegions.Get(MapRegions.JungleEnemyBot);
        factory.SoloTrendsRepository.AddRange(Puuid, SoloRows.Many(matches));
        for (var i = 0; i < matches; i++)
        {
            var matchId = $"EUW1_{i}";
            for (var d = 0; d < deathsPerMatch; d++)
            {
                factory.SoloTrendsRepository.Deaths.Add((Puuid, new SoloDeathRow(
                    matchId, 100, "MIDDLE", 3,
                    (int)(anchor.U * MapRegions.MapSize), (int)(anchor.V * MapRegions.MapSize),
                    detailed ? 1200 + d : null, detailed ? 8 : null, [], detailed ? 1 : null)));
            }
            if (i % 2 == 0) factory.SoloTrendsRepository.Objectives.Add(new SoloObjectiveRow(matchId, 200, "dragon", 1230));
        }
    }

    // ───────────────────────── Auth and validation ─────────────────────────

    [Fact]
    public async Task GetDeathZones_Returns401_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDeathZones_Returns403_WhenAccessingOtherUsersData()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDeathZones_Returns404_WhenNoRiotAccountLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("?queueType=arena", "INVALID_QUEUE")]
    [InlineData("?range=last10", "INVALID_RANGE")]
    public async Task GetDeathZones_Returns400_ForInvalidParameters(string query, string code)
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
    public async Task GetDeathZones_Returns200WithZeroMatches_NotReady()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);

        var response = await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetProperty("matches").GetInt32().Should().Be(0);
        root.GetProperty("deaths").GetInt32().Should().Be(0);
        root.GetProperty("ready").GetBoolean().Should().BeFalse();
        root.GetProperty("zones").GetArrayLength().Should().Be(0);
        root.GetProperty("backfill").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetDeathZones_ReturnsZonesAndBreakdowns_WhenReady()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        Seed(factory, matches: 20, deathsPerMatch: 2);

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo"));

        root.GetProperty("deaths").GetInt32().Should().Be(40);
        root.GetProperty("ready").GetBoolean().Should().BeTrue();
        root.GetProperty("backfill").ValueKind.Should().Be(JsonValueKind.Null);

        var zone = root.GetProperty("zones").EnumerateArray().Single();
        zone.GetProperty("key").GetString().Should().Be("jungleEnemyBot");
        zone.GetProperty("deaths").GetInt32().Should().Be(40);
        // Both deaths (20:00 and 20:01) of each of the 10 dragon matches fall within 60 seconds of the dragon at 20:30
        zone.GetProperty("lostObjectives").GetInt32().Should().Be(20);
        zone.GetProperty("costly").GetBoolean().Should().BeTrue();
        zone.GetProperty("anchor").GetProperty("u").GetDouble().Should().Be(0.74);
        zone.GetProperty("timing").GetProperty("phase").GetString().Should().Be("mid");

        var all = root.GetProperty("breakdowns").GetProperty("all");
        all.GetProperty("phase").GetProperty("mid").GetInt32().Should().Be(40);
        all.GetProperty("cost").GetProperty("dragon").GetInt32().Should().Be(20);
        root.GetProperty("breakdowns").GetProperty("byZone").GetProperty("jungleEnemyBot").GetProperty("how")
            .EnumerateObject().Sum(p => p.Value.GetInt32()).Should().Be(40);
    }

    [Fact]
    public async Task GetDeathZones_SaysQueued_AndMovesTheAccountForward_WhileDetailIsMissing()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        Seed(factory, matches: 10, deathsPerMatch: 3, detailed: false);

        var root = await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo"));

        root.GetProperty("ready").GetBoolean().Should().BeFalse();
        root.GetProperty("backfill").GetProperty("status").GetString().Should().Be("queued");
        factory.Services.GetRequiredService<DeathDetailBackfillState>().Prioritized().Should().Equal(Puuid);
    }

    [Fact]
    public async Task GetDeathZones_ReportsTheJobsProgress()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        LinkPrimaryAccount(factory);
        Seed(factory, matches: 10, deathsPerMatch: 3, detailed: false);
        var retryAt = new DateTime(2026, 10, 1, 18, 4, 0, DateTimeKind.Utc);
        factory.Services.GetRequiredService<DeathDetailBackfillState>()
            .Set(Puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, 12, 50, retryAt));

        var backfill = (await ReadJsonAsync(await GetAsync(factory, authCookie, $"{Route}/1?queueType=ranked_solo"))).GetProperty("backfill");

        backfill.GetProperty("status").GetString().Should().Be("waiting");
        backfill.GetProperty("done").GetInt32().Should().Be(12);
        backfill.GetProperty("total").GetInt32().Should().Be(50);
        backfill.GetProperty("retryAt").GetDateTime().Should().Be(retryAt);
    }
}
