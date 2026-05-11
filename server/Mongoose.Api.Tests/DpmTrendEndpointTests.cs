using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

public class DpmTrendEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task DpmTrend_returns_401_when_not_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/trends/damage-per-minute/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DpmTrend_returns_403_when_accessing_another_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/2");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DpmTrend_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DpmTrend_returns_404_when_no_riot_account_linked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DpmTrend_returns_empty_array_when_no_data_exists()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dpmTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(0);
        root.TryGetProperty("averageDamagePerMinute", out var avg).Should().BeTrue();
        avg.GetDouble().Should().Be(0);
        root.TryGetProperty("overallAverage", out var overall).Should().BeTrue();
        overall.GetDouble().Should().Be(0);
        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("neutral");
    }

    [Fact]
    public async Task DpmTrend_returns_correct_shape_with_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var dataPoints = new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_10001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-5),
                TotalDamageDealt: 42000,
                DamagePerMinute: 1200.0,
                RollingAverage: 1200.0,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            ),
            new DpmTrendPoint(
                MatchId: "NA1_10002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-4),
                TotalDamageDealt: 48000,
                DamagePerMinute: 1371.4,
                RollingAverage: 1285.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Caitlyn",
                Role: "BOTTOM"
            )
        };

        factory.TrendRepository.SetDpmData("test-puuid-123", dataPoints, 1285.7, 1250.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dpmTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        var point1 = trendArray[0];
        point1.GetProperty("matchId").GetString().Should().Be("NA1_10001");
        point1.GetProperty("gameIndex").GetInt32().Should().Be(1);
        point1.GetProperty("totalDamageDealt").GetInt32().Should().Be(42000);
        point1.GetProperty("damagePerMinute").GetDouble().Should().BeApproximately(1200.0, 0.1);
        point1.GetProperty("rollingAverage").GetDouble().Should().BeApproximately(1200.0, 0.1);
        point1.GetProperty("gameDurationMinutes").GetDouble().Should().BeApproximately(35.0, 0.1);
        point1.GetProperty("championName").GetString().Should().Be("Jinx");
        point1.GetProperty("role").GetString().Should().Be("BOTTOM");

        root.TryGetProperty("averageDamagePerMinute", out var avgDpm).Should().BeTrue();
        avgDpm.GetDouble().Should().BeApproximately(1285.7, 0.1);

        root.TryGetProperty("overallAverage", out var overallAvg).Should().BeTrue();
        overallAvg.GetDouble().Should().BeApproximately(1250.0, 0.1);

        root.TryGetProperty("trend", out var trend).Should().BeTrue();
        trend.GetString().Should().Be("improving");
    }

    [Fact]
    public async Task DpmTrend_free_tier_returns_primary_account_only_when_accountId_all_requested()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // User is free tier (default)
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-primary", "MainPlayer", "NA1", "MainPlayer#NA1", 100, 42);
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-alt", "AltPlayer", "NA1", "AltPlayer#NA1", 101, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-alt", isPrimary: false);

        factory.TrendRepository.SetDpmData("test-puuid-primary", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_20001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                TotalDamageDealt: 45000,
                DamagePerMinute: 1285.7,
                RollingAverage: 1285.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1285.7, 1250.0, "neutral");

        factory.TrendRepository.SetDpmData("test-puuid-alt", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_20002",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                TotalDamageDealt: 52000,
                DamagePerMinute: 1485.7,
                RollingAverage: 1485.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Ezreal",
                Role: "BOTTOM"
            )
        }, 1485.7, 1400.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        // Free-tier user requesting all accounts gets only the primary account (single point)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dpmTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(1);
        trendArray[0].GetProperty("matchId").GetString().Should().Be("NA1_20001");
    }

    [Fact]
    public async Task DpmTrend_pro_tier_returns_all_accounts_when_accountId_all_requested()
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

        factory.TrendRepository.SetDpmData("test-puuid-primary", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_30001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                TotalDamageDealt: 45000,
                DamagePerMinute: 1285.7,
                RollingAverage: 1285.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1285.7, 1250.0, "neutral");

        factory.TrendRepository.SetDpmData("test-puuid-alt", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_30002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                TotalDamageDealt: 52000,
                DamagePerMinute: 1485.7,
                RollingAverage: 1485.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Ezreal",
                Role: "BOTTOM"
            )
        }, 1485.7, 1400.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dpmTrend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        var matchIds = trendArray.EnumerateArray().Select(p => p.GetProperty("matchId").GetString()).ToArray();
        matchIds.Should().Contain("NA1_30001");
        matchIds.Should().Contain("NA1_30002");
    }

    [Fact]
    public async Task DpmTrend_returns_account_game_name_in_multi_account_mode()
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

        factory.TrendRepository.SetDpmData("test-puuid-primary", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_40001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                TotalDamageDealt: 45000,
                DamagePerMinute: 1285.7,
                RollingAverage: 1285.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1285.7, 1250.0, "neutral");

        factory.TrendRepository.SetDpmData("test-puuid-alt", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_40002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                TotalDamageDealt: 52000,
                DamagePerMinute: 1485.7,
                RollingAverage: 1485.7,
                GameDurationMinutes: 35.0,
                ChampionName: "Ezreal",
                Role: "BOTTOM"
            )
        }, 1485.7, 1400.0, "improving");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dpmTrend", out var trendArray).Should().BeTrue();
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
    public async Task DpmTrend_accepts_valid_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        factory.TrendRepository.SetDpmData("test-puuid-123", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_50001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-1),
                TotalDamageDealt: 40000,
                DamagePerMinute: 1142.9,
                RollingAverage: 1142.9,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1142.9, 1142.9, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DpmTrend_accepts_valid_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        factory.TrendRepository.SetDpmData("test-puuid-123", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_60001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-10),
                TotalDamageDealt: 40000,
                DamagePerMinute: 1142.9,
                RollingAverage: 1142.9,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1142.9, 1142.9, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DpmTrend_accepts_limit_parameter_and_returns_200()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        factory.TrendRepository.SetDpmData("test-puuid-123", new[]
        {
            new DpmTrendPoint(
                MatchId: "NA1_70001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-1),
                TotalDamageDealt: 40000,
                DamagePerMinute: 1142.9,
                RollingAverage: 1142.9,
                GameDurationMinutes: 35.0,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        }, 1142.9, 1142.9, "neutral");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?limit=20");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DpmTrend_returns_403_when_requesting_unlinked_account_id()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/damage-per-minute/1?accountId=acc_notowned");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
