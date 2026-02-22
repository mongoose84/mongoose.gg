using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;
using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;
using static Mongoose.Api.Application.DTOs.MainChampionDto;
using static Mongoose.Api.Application.DTOs.TrendDto;
using static Mongoose.Api.Application.DTOs.ChampionSelectDto;

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
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    private static ChampionSelectResponse CreateTestChampionSelectData()
    {
        return new ChampionSelectResponse(
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            GamesPlayed: 100,
            WinRate: 55.0
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/invalid");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/999");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChampionSelect_returns_champion_select_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.ChampionSelectRepository.SetChampionSelectData("puuid123", CreateTestChampionSelectData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<ChampionSelectResponse>();
        data.Should().NotBeNull();
        data!.GamesPlayed.Should().Be(100);
        data.WinRate.Should().Be(55.0);
        data.MainChampions.Should().NotBeNull();
    }

    [Fact]
    public async Task ChampionSelect_accepts_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid123", "Tester", "EUW", "TesterSum", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid123", isPrimary: true);
        factory.ChampionSelectRepository.SetChampionSelectData("puuid123", CreateTestChampionSelectData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1?queueType=ranked_solo");
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
        factory.ChampionSelectRepository.SetChampionSelectData("puuid123", CreateTestChampionSelectData());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/champion-select/1?timeRange=1m");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/invalid");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/999");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1?queueType=ranked_solo");
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
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/matchups/1?timeRange=3m");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}

