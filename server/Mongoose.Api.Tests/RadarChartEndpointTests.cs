using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

public class RadarChartEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task RadarChart_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/radar-chart/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RadarChart_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RadarChart_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RadarChart_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RadarChart_returns_empty_response_when_no_match_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("axes").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("gamesAnalyzed").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task RadarChart_returns_data_when_authenticated_and_data_exists()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var responseData = new RadarChartResponse(
            Axes:
            [
                new RadarAxis("laning", "Laning", 62.5, 500, "gold diff @15"),
                new RadarAxis("farming", "Farming", 58.0, 5.8, "CS/min"),
                new RadarAxis("combat", "Combat", 64.2, 64.2, "% KP"),
                new RadarAxis("vision", "Vision", 44.0, 1.1, "VS/min"),
                new RadarAxis("objectives", "Objectives", 55.3, 55.3, "% obj"),
                new RadarAxis("survivability", "Survivability", 56.0, 4.4, "deaths/game")
            ],
            GamesAnalyzed: 87
        );

        factory.RadarChartRepository.SetRadarData("test-puuid-123", responseData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var radar = JsonSerializer.Deserialize<RadarChartResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        radar.Should().NotBeNull();
        radar!.GamesAnalyzed.Should().Be(87);
        radar.Axes.Should().HaveCount(6);
        radar.Axes.Select(a => a.Key).Should().Contain(["laning", "farming", "combat", "vision", "objectives", "survivability"]);
    }

    [Fact]
    public async Task RadarChart_supports_queue_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        factory.RadarChartRepository.SetRadarData("test-puuid-123", new RadarChartResponse(Array.Empty<RadarAxis>(), 0));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RadarChart_supports_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        factory.RadarChartRepository.SetRadarData("test-puuid-123", new RadarChartResponse(Array.Empty<RadarAxis>(), 0));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RadarChart_returns_normalized_values_in_zero_to_hundred_range()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var responseData = new RadarChartResponse(
            [
                new RadarAxis("laning", "Laning", 0, -2000, "gold diff @15"),
                new RadarAxis("farming", "Farming", 100, 10, "CS/min"),
                new RadarAxis("combat", "Combat", 63.4, 63.4, "% KP"),
                new RadarAxis("vision", "Vision", 40, 1.0, "VS/min"),
                new RadarAxis("objectives", "Objectives", 75, 75, "% obj"),
                new RadarAxis("survivability", "Survivability", 85, 1.5, "deaths/game")
            ],
            120);

        factory.RadarChartRepository.SetRadarData("test-puuid-123", responseData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/radar-chart/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var radar = JsonSerializer.Deserialize<RadarChartResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        radar.Should().NotBeNull();
        radar!.Axes.Should().OnlyContain(axis => axis.Value >= 0 && axis.Value <= 100);
    }
}
