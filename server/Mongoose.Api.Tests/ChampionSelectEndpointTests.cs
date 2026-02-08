using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;
using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;
using static Mongoose.Api.Application.DTOs.MainChampionDto;

namespace Mongoose.Api.Tests;

/// <summary>
/// Tests for ChampionSelectEndpoint and SoloMatchupsEndpoint.
/// </summary>
public class ChampionSelectEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie");
        var cookie = cookies.First(c => c.Contains("mongoose-auth"));
        return cookie.Split(';', 2)[0]; // Extract name=value portion only
    }

    private static SoloPerformanceResponse CreateTestPerformanceData()
    {
        return new SoloPerformanceResponse(
            GamesPlayed: 100,
            Wins: 55,
            WinRate: 55.0,
            AvgKda: 3.2,
            AvgGameDurationMinutes: 28.5,
            SideStats: new SideWinDistribution(
                BlueWins: 30, RedWins: 25, BlueGames: 50, RedGames: 50,
                TotalGames: 100, BlueWinDistribution: 60.0, RedWinDistribution: 50.0),
            UniqueChampsPlayedCount: 15,
            MainChampion: new ChampionSummary(1, "Annie", 25, 60.0, 25.0),
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: new TrendMetric(10, 6, 60.0, 3.5),
            Last20Games: new TrendMetric(20, 11, 55.0, 3.2),
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(5, 10, 8, 3, 8.5, 5.2),
            QueueType: "ranked_solo",
            LpTrend: Array.Empty<LpTrendPoint>()
        );
    }

    private static ChampionMatchupsResponse CreateTestMatchupsData()
    {
        return new ChampionMatchupsResponse(
            Matchups: new[]
            {
                new ChampionMatchup(
                    ChampionId: 1,
                    ChampionName: "Annie",
                    Role: "MID",
                    TotalGames: 25,
                    Wins: 15,
                    WinRate: 60.0,
                    Opponents: new[]
                    {
                        new OpponentMatchup(238, "Zed", 3, 2, 2, 1),
                        new OpponentMatchup(7, "LeBlanc", 2, 3, 1, 2)
                    })
            },
            QueueType: "ranked_solo",
            TimeRange: "all"
        );
    }

    #region ChampionSelectEndpoint Tests

    [Fact]
    public async Task ChampionSelect_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/champion-select/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChampionSelect_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChampionSelect_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChampionSelect_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChampionSelect_returns_not_found_when_no_match_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChampionSelect_returns_performance_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.SoloPerformanceRepository.SetPerformanceData("puuid123", CreateTestPerformanceData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<SoloPerformanceResponse>();
        data.Should().NotBeNull();
        data!.GamesPlayed.Should().Be(100);
        data.Wins.Should().Be(55);
        data.WinRate.Should().Be(55.0);
        data.AvgKda.Should().Be(3.2);
        data.MainChampion.Should().NotBeNull();
        data.MainChampion!.ChampionName.Should().Be("Annie");
    }

    [Fact]
    public async Task ChampionSelect_accepts_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.SoloPerformanceRepository.SetPerformanceData("puuid123", CreateTestPerformanceData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChampionSelect_accepts_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.SoloPerformanceRepository.SetPerformanceData("puuid123", CreateTestPerformanceData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1?timeRange=1m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region SoloMatchupsEndpoint Tests

    [Fact]
    public async Task SoloMatchups_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/matchups/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SoloMatchups_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SoloMatchups_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SoloMatchups_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SoloMatchups_returns_empty_matchups_when_no_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<ChampionMatchupsResponse>();
        data.Should().NotBeNull();
        data!.Matchups.Should().BeEmpty();
    }

    [Fact]
    public async Task SoloMatchups_returns_matchup_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.MatchupRepository.SetMatchupData("puuid123", CreateTestMatchupsData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<ChampionMatchupsResponse>();
        data.Should().NotBeNull();
        data!.Matchups.Should().HaveCount(1);
        data.Matchups[0].ChampionName.Should().Be("Annie");
        data.Matchups[0].TotalGames.Should().Be(25);
        data.Matchups[0].Opponents.Should().HaveCount(2);
    }

    [Fact]
    public async Task SoloMatchups_accepts_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.MatchupRepository.SetMatchupData("puuid123", CreateTestMatchupsData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SoloMatchups_accepts_time_range_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.MatchupRepository.SetMatchupData("puuid123", CreateTestMatchupsData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1?timeRange=3m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}

