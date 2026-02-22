using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Tests;

public class VisionScoreTrendEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task VisionScoreTrend_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/trends/vision-score/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VisionScoreTrend_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // User 1 is logged in, trying to access user 999's data
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VisionScoreTrend_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VisionScoreTrend_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VisionScoreTrend_returns_empty_data_when_no_vision_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account but no vision score data
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("visionScoreTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(0);
        root.TryGetProperty("averageVisionPerMinute", out var avgVision).Should().BeTrue();
        avgVision.GetDouble().Should().Be(0);
        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("neutral");
        root.TryGetProperty("roleTarget", out var roleTarget).Should().BeTrue();
        roleTarget.GetDouble().Should().Be(1.0);
    }

    [Fact]
    public async Task VisionScoreTrend_returns_data_when_authenticated_and_data_exists()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add vision score data
        var dataPoints = new[]
        {
            new VisionScoreTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                VisionScore: 45,
                VisionScorePerMinute: 1.2,
                RollingAverage: 1.2,
                GameDurationMinutes: 37.5,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            ),
            new VisionScoreTrendPoint(
                MatchId: "NA1_12346",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-4),
                VisionScore: 52,
                VisionScorePerMinute: 1.5,
                RollingAverage: 1.35,
                GameDurationMinutes: 34.7,
                ChampionName: "Caitlyn",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetVisionScoreData("test-puuid-123", dataPoints, 1.35, 1.30, 1.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("visionScoreTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        var point1 = trendArray[0];
        point1.GetProperty("matchId").GetString().Should().Be("NA1_12345");
        point1.GetProperty("gameIndex").GetInt32().Should().Be(1);
        point1.GetProperty("visionScore").GetInt32().Should().Be(45);
        point1.GetProperty("visionScorePerMinute").GetDouble().Should().BeApproximately(1.2, 0.01);
        point1.GetProperty("rollingAverage").GetDouble().Should().BeApproximately(1.2, 0.01);
        point1.GetProperty("championName").GetString().Should().Be("Jinx");
        point1.GetProperty("role").GetString().Should().Be("BOTTOM");

        root.TryGetProperty("averageVisionPerMinute", out var avgVision).Should().BeTrue();
        avgVision.GetDouble().Should().BeApproximately(1.35, 0.01);

        root.TryGetProperty("overallAverage", out var overallAvg).Should().BeTrue();
        overallAvg.GetDouble().Should().BeApproximately(1.30, 0.01);

        root.TryGetProperty("roleTarget", out var roleTarget).Should().BeTrue();
        roleTarget.GetDouble().Should().Be(1.0);

        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("improving");
    }

    [Fact]
    public async Task VisionScoreTrend_returns_support_role_target_when_playing_support()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "SupportMain", "NA1", "SupportMain#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add vision score data with UTILITY role (support)
        var dataPoints = new[]
        {
            new VisionScoreTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                VisionScore: 90,
                VisionScorePerMinute: 2.5,
                RollingAverage: 2.5,
                GameDurationMinutes: 36.0,
                ChampionName: "Lulu",
                Role: "UTILITY"
            )
        };

        factory.TrendRepository.SetVisionScoreData("test-puuid-123", dataPoints, 2.5, 2.4, 2.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("roleTarget", out var roleTarget).Should().BeTrue();
        roleTarget.GetDouble().Should().Be(2.0);
    }

    [Fact]
    public async Task VisionScoreTrend_respects_queue_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new VisionScoreTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                VisionScore: 45,
                VisionScorePerMinute: 1.2,
                RollingAverage: 1.2,
                GameDurationMinutes: 37.5,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetVisionScoreData("test-puuid-123", dataPoints, 1.2, 1.2, 1.0, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VisionScoreTrend_respects_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new VisionScoreTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                VisionScore: 45,
                VisionScorePerMinute: 1.2,
                RollingAverage: 1.2,
                GameDurationMinutes: 37.5,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetVisionScoreData("test-puuid-123", dataPoints, 1.2, 1.2, 1.0, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VisionScoreTrend_respects_limit_parameter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new VisionScoreTrendPoint(
                MatchId: "NA1_12345",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                VisionScore: 45,
                VisionScorePerMinute: 1.2,
                RollingAverage: 1.2,
                GameDurationMinutes: 37.5,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetVisionScoreData("test-puuid-123", dataPoints, 1.2, 1.2, 1.0, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/vision-score/1?limit=10");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
