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
}

