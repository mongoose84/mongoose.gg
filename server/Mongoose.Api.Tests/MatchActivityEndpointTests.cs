using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class MatchActivityEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task MatchActivity_returns_401_when_unauthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/activity/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MatchActivity_returns_403_when_accessing_another_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/activity/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MatchActivity_returns_404_when_no_riot_account_linked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/activity/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchActivity_returns_daily_match_counts_for_linked_account()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-activity", "ActivityPlayer", "NA1", "ActivityPlayer#NA1", 80, 10);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-activity", isPrimary: true);

        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        factory.TrendRepository.SetDailyMatchCounts("test-puuid-activity", new Dictionary<string, int>
        {
            [today] = 3
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/activity/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("dailyMatchCounts", out var counts).Should().BeTrue();
        root.TryGetProperty("startDate", out _).Should().BeTrue();
        root.TryGetProperty("endDate", out _).Should().BeTrue();
        root.TryGetProperty("totalMatches", out var total).Should().BeTrue();

        counts.TryGetProperty(today, out var todayCount).Should().BeTrue();
        todayCount.GetInt32().Should().Be(3);
        total.GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task MatchActivity_returns_empty_counts_when_no_matches_played()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-empty", "EmptyPlayer", "NA1", "EmptyPlayer#NA1", 10, 5);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-empty", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/activity/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("totalMatches").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("dailyMatchCounts").EnumerateObject().Should().BeEmpty();
    }

    [Fact]
    public async Task MatchActivity_date_range_spans_approximately_182_days()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-dates", "DatePlayer", "NA1", "DatePlayer#NA1", 20, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-dates", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/activity/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var startDate = DateTime.Parse(doc.RootElement.GetProperty("startDate").GetString()!);
        var endDate = DateTime.Parse(doc.RootElement.GetProperty("endDate").GetString()!);
        var span = (endDate - startDate).Days;

        span.Should().BeCloseTo(182, 2, "endpoint returns 182 days of activity data");
    }
}
