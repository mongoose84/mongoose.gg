using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Application.DTOs;
using Xunit;
using static Mongoose.Api.Application.DTOs.MainChampionDto;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;

namespace Mongoose.Api.Tests;

public class SoloPerformanceEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task Solo_performance_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/dashboard/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Solo_performance_returns_403_when_accessing_another_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/2");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Solo_performance_returns_404_when_no_riot_account_linked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Solo_performance_returns_rank_info_with_solo_and_flex_ranks()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account with both solo and flex ranks
        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1,
            puuid: "test-puuid-123",
            gameName: "TestPlayer",
            region: "NA1",
            summonerName: "TestPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42,
            soloTier: "GOLD",
            soloRank: "II",
            soloLp: 78,
            flexTier: "SILVER",
            flexRank: "I",
            flexLp: 45
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add mock performance data
        factory.SoloPerformanceRepository.SetPerformanceData("test-puuid-123", new SoloPerformanceResponse(
            GamesPlayed: 42,
            Wins: 28,
            WinRate: 66.7,
            AvgKda: 3.5,
            AvgGameDurationMinutes: 28.5,
            AvgKills: 6.5,
            AvgDeaths: 3.2,
            AvgAssists: 8.1,
            OverallWinRate: 55.0,
            OverallAvgKills: 5.5,
            OverallAvgDeaths: 3.5,
            OverallAvgAssists: 7.0,
            OverallAvgKda: 3.0,
            SideStats: new SideWinDistribution(10, 18, 20, 22, 42, 50.0, 81.8),
            UniqueChampsPlayedCount: 15,
            MainChampion: null,
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: null,
            Last20Games: null,
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(5, 10, 8, 2, 5.5, 3.2),
            QueueType: "all"
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify rank info is present
        root.TryGetProperty("rankInfo", out var rankInfo).Should().BeTrue();
        
        // Verify Solo/Duo rank
        rankInfo.TryGetProperty("soloDuoRank", out var soloDuoRank).Should().BeTrue();
        soloDuoRank.GetProperty("tier").GetString().Should().Be("GOLD");
        soloDuoRank.GetProperty("division").GetString().Should().Be("II");
        soloDuoRank.GetProperty("lp").GetInt32().Should().Be(78);
        soloDuoRank.GetProperty("hasRank").GetBoolean().Should().BeTrue();

        // Verify Flex rank
        rankInfo.TryGetProperty("flexRank", out var flexRank).Should().BeTrue();
        flexRank.GetProperty("tier").GetString().Should().Be("SILVER");
        flexRank.GetProperty("division").GetString().Should().Be("I");
        flexRank.GetProperty("lp").GetInt32().Should().Be(45);
        flexRank.GetProperty("hasRank").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Solo_performance_returns_rank_info_with_unranked_player()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account without ranks
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

        // Add mock performance data
        factory.SoloPerformanceRepository.SetPerformanceData("test-puuid-123", new SoloPerformanceResponse(
            GamesPlayed: 10,
            Wins: 5,
            WinRate: 50.0,
            AvgKda: 2.5,
            AvgGameDurationMinutes: 25.0,
            AvgKills: 4.5,
            AvgDeaths: 4.0,
            AvgAssists: 6.0,
            OverallWinRate: 50.0,
            OverallAvgKills: 4.5,
            OverallAvgDeaths: 4.0,
            OverallAvgAssists: 6.0,
            OverallAvgKda: 2.5,
            SideStats: new SideWinDistribution(3, 2, 5, 5, 10, 60.0, 40.0),
            UniqueChampsPlayedCount: 8,
            MainChampion: null,
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: null,
            Last20Games: null,
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(3, 5, 2, 0, 6.0, 4.5),
            QueueType: "all"
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verify rank info is present
        root.TryGetProperty("rankInfo", out var rankInfo).Should().BeTrue();
        
        // Verify Solo/Duo rank is unranked
        rankInfo.TryGetProperty("soloDuoRank", out var soloDuoRank).Should().BeTrue();
        soloDuoRank.GetProperty("hasRank").GetBoolean().Should().BeFalse();

        // Verify Flex rank is unranked
        rankInfo.TryGetProperty("flexRank", out var flexRank).Should().BeTrue();
        flexRank.GetProperty("hasRank").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Solo_performance_falls_back_to_first_linked_account_when_no_primary_is_set()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add two linked accounts where neither is marked as primary
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-first",
            gameName: "FirstPlayer",
            region: "NA1",
            summonerName: "FirstPlayer#NA1",
            summonerLevel: 100,
            profileIconId: 42
        );
        factory.RiotAccountsRepository.AddRiotAccount(
            userId: 1,
            puuid: "test-puuid-second",
            gameName: "SecondPlayer",
            region: "NA1",
            summonerName: "SecondPlayer#NA1",
            summonerLevel: 101,
            profileIconId: 43
        );

        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-first", isPrimary: false);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-second", isPrimary: false);

        // Seed performance data for first linked account (fallback target)
        factory.SoloPerformanceRepository.SetPerformanceData("test-puuid-first", new SoloPerformanceResponse(
            GamesPlayed: 12,
            Wins: 7,
            WinRate: 58.3,
            AvgKda: 3.1,
            AvgGameDurationMinutes: 27.4,
            AvgKills: 6.1,
            AvgDeaths: 3.8,
            AvgAssists: 5.7,
            OverallWinRate: 54.0,
            OverallAvgKills: 5.4,
            OverallAvgDeaths: 4.0,
            OverallAvgAssists: 6.2,
            OverallAvgKda: 2.9,
            SideStats: new SideWinDistribution(4, 3, 6, 6, 12, 57.1, 50.0),
            UniqueChampsPlayedCount: 6,
            MainChampion: null,
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: null,
            Last20Games: null,
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(3, 4, 5, 0, 5.8, 3.9),
            QueueType: "all"
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.GetProperty("gamesPlayed").GetInt32().Should().Be(12);
        root.GetProperty("winRate").GetDouble().Should().Be(58.3);
    }

    [Fact]
    public async Task Solo_performance_single_account_returns_accountCount_1_and_allAccountRanks()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1,
            puuid: "puuid-main",
            gameName: "MainPlayer",
            region: "EUW1",
            summonerName: "MainPlayer#EUW",
            summonerLevel: 200,
            profileIconId: 1,
            soloTier: "DIAMOND",
            soloRank: "IV",
            soloLp: 10,
            flexTier: null,
            flexRank: null,
            flexLp: null
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);

        factory.SoloPerformanceRepository.SetPerformanceData("puuid-main", new SoloPerformanceResponse(
            GamesPlayed: 20, Wins: 12, WinRate: 60.0, AvgKda: 3.0, AvgGameDurationMinutes: 27.0,
            AvgKills: 5.0, AvgDeaths: 3.0, AvgAssists: 7.0,
            OverallWinRate: 55.0, OverallAvgKills: 4.8, OverallAvgDeaths: 3.2, OverallAvgAssists: 6.5,
            OverallAvgKda: 2.8,
            SideStats: new SideWinDistribution(6, 6, 10, 10, 20, 60.0, 60.0),
            UniqueChampsPlayedCount: 5, MainChampion: null,
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: null, Last20Games: null,
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(2, 5, 3, 0, 6.0, 4.0),
            QueueType: "all"
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Single account → accountCount = 1
        root.GetProperty("accountCount").GetInt32().Should().Be(1);

        // allAccountRanks contains exactly 1 entry
        var allRanks = root.GetProperty("allAccountRanks");
        allRanks.GetArrayLength().Should().Be(1);
        allRanks[0].GetProperty("gameName").GetString().Should().Be("MainPlayer");
        allRanks[0].GetProperty("soloDuoRank").GetProperty("tier").GetString().Should().Be("DIAMOND");
        allRanks[0].GetProperty("soloDuoRank").GetProperty("hasRank").GetBoolean().Should().BeTrue();
        allRanks[0].GetProperty("flexRank").GetProperty("hasRank").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Solo_performance_overall_mode_returns_accountCount_and_all_ranks_for_each_account()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Set up two linked Riot accounts
        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1,
            puuid: "puuid-main",
            gameName: "FakerMain",
            region: "KR",
            summonerName: "FakerMain#KR1",
            summonerLevel: 500,
            profileIconId: 1,
            soloTier: "DIAMOND",
            soloRank: "II",
            soloLp: 75,
            flexTier: null,
            flexRank: null,
            flexLp: null
        );
        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1,
            puuid: "puuid-smurf",
            gameName: "FakerSmurf",
            region: "KR",
            summonerName: "FakerSmurf#KR2",
            summonerLevel: 80,
            profileIconId: 2,
            soloTier: "PLATINUM",
            soloRank: "I",
            soloLp: 30,
            flexTier: null,
            flexRank: null,
            flexLp: null
        );
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-smurf", isPrimary: false);

        // Upgrade user to pro so multi-account visibility is enabled
        factory.UsersRepository.SetTier("tester", "pro");

        // Seed performance data for primary account (fake repo returns first match)
        factory.SoloPerformanceRepository.SetPerformanceData("puuid-main", new SoloPerformanceResponse(
            GamesPlayed: 100, Wins: 58, WinRate: 58.0, AvgKda: 4.1, AvgGameDurationMinutes: 26.5,
            AvgKills: 7.0, AvgDeaths: 3.0, AvgAssists: 9.0,
            OverallWinRate: 54.0, OverallAvgKills: 6.5, OverallAvgDeaths: 3.2, OverallAvgAssists: 8.5,
            OverallAvgKda: 3.8,
            SideStats: new SideWinDistribution(30, 28, 50, 50, 100, 60.0, 56.0),
            UniqueChampsPlayedCount: 12, MainChampion: null,
            MainChampions: Array.Empty<MainChampionRoleGroup>(),
            Last10Games: null, Last20Games: null,
            PerformanceByPhase: Array.Empty<PerformancePhase>(),
            RoleBreakdown: Array.Empty<RolePerformance>(),
            DeathEfficiency: new DeathEfficiency(8, 20, 15, 5, 4.5, 2.0),
            QueueType: "all"
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/solo/dashboard/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Overall mode → accountCount = 2
        root.GetProperty("accountCount").GetInt32().Should().Be(2);

        // allAccountRanks carries rank info for all accounts
        var allRanks = root.GetProperty("allAccountRanks");
        allRanks.GetArrayLength().Should().Be(2);

        var mainRank = allRanks.EnumerateArray().First(e => e.GetProperty("gameName").GetString() == "FakerMain");
        mainRank.GetProperty("soloDuoRank").GetProperty("tier").GetString().Should().Be("DIAMOND");
        mainRank.GetProperty("soloDuoRank").GetProperty("division").GetString().Should().Be("II");
        mainRank.GetProperty("soloDuoRank").GetProperty("lp").GetInt32().Should().Be(75);

        var smurfRank = allRanks.EnumerateArray().First(e => e.GetProperty("gameName").GetString() == "FakerSmurf");
        smurfRank.GetProperty("soloDuoRank").GetProperty("tier").GetString().Should().Be("PLATINUM");
        smurfRank.GetProperty("soloDuoRank").GetProperty("division").GetString().Should().Be("I");
        smurfRank.GetProperty("soloDuoRank").GetProperty("lp").GetInt32().Should().Be(30);
    }
}

