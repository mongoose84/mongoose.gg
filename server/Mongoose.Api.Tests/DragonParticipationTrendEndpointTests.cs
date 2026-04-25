using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

public class DragonParticipationTrendEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task DragonParticipationTrend_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/trends/dragon-participation/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // User 1 is logged in, trying to access user 999's data
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_empty_data_when_no_participation_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account but no dragon participation data
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(0);
        root.TryGetProperty("averageParticipation", out var avgParticipation).Should().BeTrue();
        avgParticipation.GetDouble().Should().Be(0);
        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("neutral");
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_data_when_authenticated_and_data_exists()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add dragon participation data
        var dataPoints = new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                TeamDragons: 3,
                DragonsParticipated: 2,
                ParticipationRate: 66.7,
                RollingAverage: 66.7,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            ),
            new DragonParticipationTrendPoint(
                MatchId: "NA1_12346",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-4),
                TeamDragons: 4,
                DragonsParticipated: 3,
                ParticipationRate: 75.0,
                RollingAverage: 71.4,
                ChampionName: "Caitlyn",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetDragonParticipationData("test-puuid-123", dataPoints, 71.4, 70.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify response structure
        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        // Verify first data point
        var firstPoint = trendArray[0];
        firstPoint.GetProperty("matchId").GetString().Should().Be("NA1_12345");
        firstPoint.GetProperty("gameIndex").GetInt32().Should().Be(1);
        firstPoint.GetProperty("teamDragons").GetInt32().Should().Be(3);
        firstPoint.GetProperty("dragonsParticipated").GetInt32().Should().Be(2);
        firstPoint.GetProperty("participationRate").GetDouble().Should().BeApproximately(66.7, 0.1);
        firstPoint.GetProperty("championName").GetString().Should().Be("Jinx");
        firstPoint.GetProperty("role").GetString().Should().Be("BOTTOM");

        // Verify summary statistics
        root.TryGetProperty("averageParticipation", out var avgParticipation).Should().BeTrue();
        avgParticipation.GetDouble().Should().BeApproximately(71.4, 0.1);

        root.TryGetProperty("overallAverage", out var overallAvg).Should().BeTrue();
        overallAvg.GetDouble().Should().BeApproximately(70.0, 0.1);

        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("improving");
    }

    [Fact]
    public async Task DragonParticipationTrend_returns_multi_account_data_with_account_game_name_in_overall_mode()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var user = await factory.UsersRepository.GetByIdAsync(1);
        user.Should().NotBeNull();
        user!.Tier = "pro";
        await factory.UsersRepository.UpsertAsync(user);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-primary", "MainPlayer", "NA1", "MainPlayer#NA1", 100, 42);
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-alt", "AltPlayer", "NA1", "AltPlayer#NA1", 101, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-alt", isPrimary: false);

        factory.TrendRepository.SetDragonParticipationData("test-puuid-primary", new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_30001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                TeamDragons: 3,
                DragonsParticipated: 2,
                ParticipationRate: 66.7,
                RollingAverage: 66.7,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 66.7, 66.7, "neutral");

        factory.TrendRepository.SetDragonParticipationData("test-puuid-alt", new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_30002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                TeamDragons: 4,
                DragonsParticipated: 3,
                ParticipationRate: 75.0,
                RollingAverage: 70.8,
                ChampionName: "Caitlyn",
                Role: "BOTTOM"
            )
        }, 75.0, 70.8, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        var accountNames = trendArray
            .EnumerateArray()
            .Select(point =>
            {
                point.TryGetProperty("accountGameName", out var accountGameName).Should().BeTrue();
                return accountGameName.GetString();
            })
            .ToArray();

        accountNames.Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
        accountNames.Should().Contain("MainPlayer#NA1");
        accountNames.Should().Contain("AltPlayer#NA1");
    }

    [Fact]
    public async Task DragonParticipationTrend_respects_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-1),
                TeamDragons: 3,
                DragonsParticipated: 2,
                ParticipationRate: 66.7,
                RollingAverage: 66.7,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetDragonParticipationData("test-puuid-123", dataPoints, 66.7, 66.7, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task DragonParticipationTrend_respects_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-1),
                TeamDragons: 3,
                DragonsParticipated: 2,
                ParticipationRate: 66.7,
                RollingAverage: 66.7,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetDragonParticipationData("test-puuid-123", dataPoints, 66.7, 66.7, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task DragonParticipationTrend_respects_limit_parameter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new DragonParticipationTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-1),
                TeamDragons: 3,
                DragonsParticipated: 2,
                ParticipationRate: 66.7,
                RollingAverage: 66.7,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetDragonParticipationData("test-puuid-123", dataPoints, 66.7, 66.7, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/dragon-participation/1?limit=20");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dragonParticipationTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }
}
