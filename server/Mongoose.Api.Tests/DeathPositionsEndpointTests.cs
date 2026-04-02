using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.QueryModels;
using Xunit;

namespace Mongoose.Api.Tests;

public class DeathPositionsEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task GetDeathPositions_Returns401_WhenNotAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/death-positions/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDeathPositions_Returns403_WhenAccessingOtherUsersData()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Try to access userId=2 while authenticated as userId=1
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/2");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDeathPositions_Returns404_WhenNoRiotAccountLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // No Riot account linked for userId=1
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("No riot accounts found");
    }

    [Fact]
    public async Task GetDeathPositions_ReturnsData_WhenAuthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add mock death positions data
        var testDeaths = new[]
        {
            new DeathPositionData(7234, 8456, 8, "early", 238, 1, "match-1"),
            new DeathPositionData(4561, 6789, 22, "late", 412, 3, "match-1"),
            new DeathPositionData(9100, 5200, 15, "mid", 64, 2, "match-2")
        };

        var testData = new DeathPositionsResult(
            Deaths: testDeaths,
            TotalDeaths: 143,
            MatchesAnalyzed: 32,
            PhaseSummary: new DeathPositionPhaseSummary(28, 45, 42, 28)
        );

        factory.DeathPositionsRepository.SetDeathPositionsData("test-puuid-123", testData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify structure
        root.TryGetProperty("deaths", out var deaths).Should().BeTrue();
        deaths.GetArrayLength().Should().Be(3);

        root.TryGetProperty("totalDeaths", out var totalDeaths).Should().BeTrue();
        totalDeaths.GetInt32().Should().Be(143);

        root.TryGetProperty("matchesAnalyzed", out var matches).Should().BeTrue();
        matches.GetInt32().Should().Be(32);

        root.TryGetProperty("phaseSummary", out var phaseSummary).Should().BeTrue();
        phaseSummary.GetProperty("early").GetInt32().Should().Be(28);
        phaseSummary.GetProperty("mid").GetInt32().Should().Be(45);
        phaseSummary.GetProperty("late").GetInt32().Should().Be(42);
        phaseSummary.GetProperty("veryLate").GetInt32().Should().Be(28);

        // Verify first death position
        var firstDeath = deaths[0];
        firstDeath.GetProperty("x").GetInt32().Should().Be(7234);
        firstDeath.GetProperty("y").GetInt32().Should().Be(8456);
        firstDeath.GetProperty("minuteMark").GetInt32().Should().Be(8);
        firstDeath.GetProperty("phase").GetString().Should().Be("early");
        firstDeath.GetProperty("killerChampionId").GetInt32().Should().Be(238);
        firstDeath.GetProperty("assistCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetDeathPositions_ReturnsEmptyArray_WhenNoDeathEventsStored()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // No death positions data set (returns null from repository)
        // Endpoint should return empty response with 200

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("deaths", out var deaths).Should().BeTrue();
        deaths.GetArrayLength().Should().Be(0);

        root.TryGetProperty("totalDeaths", out var totalDeaths).Should().BeTrue();
        totalDeaths.GetInt32().Should().Be(0);

        root.TryGetProperty("matchesAnalyzed", out var matches).Should().BeTrue();
        matches.GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetDeathPositions_SupportsQueueFilter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var testData = new DeathPositionsResult(
            Deaths: new[] { new DeathPositionData(7234, 8456, 8, "early", 238, 1, "match-1") },
            TotalDeaths: 50,
            MatchesAnalyzed: 15,
            PhaseSummary: new DeathPositionPhaseSummary(10, 15, 15, 10)
        );

        factory.DeathPositionsRepository.SetDeathPositionsData("test-puuid-123", testData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDeathPositions_SupportsTimeRangeFilter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var testData = new DeathPositionsResult(
            Deaths: new[] { new DeathPositionData(7234, 8456, 8, "early", 238, 1, "match-1") },
            TotalDeaths: 30,
            MatchesAnalyzed: 10,
            PhaseSummary: new DeathPositionPhaseSummary(8, 10, 8, 4)
        );

        factory.DeathPositionsRepository.SetDeathPositionsData("test-puuid-123", testData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDeathPositions_SupportsSideFilter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        var testData = new DeathPositionsResult(
            Deaths: new[] { new DeathPositionData(7234, 8456, 8, "early", 238, 1, "match-1") },
            TotalDeaths: 70,
            MatchesAnalyzed: 16,
            PhaseSummary: new DeathPositionPhaseSummary(15, 20, 20, 15)
        );

        factory.DeathPositionsRepository.SetDeathPositionsData("test-puuid-123", testData);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1?side=blue");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDeathPositions_Returns400_ForInvalidSide()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add Riot account
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/1?side=invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid side value");
    }

    [Fact]
    public async Task GetDeathPositions_Returns400_ForInvalidUserId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/death-positions/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid userId format");
    }
}
