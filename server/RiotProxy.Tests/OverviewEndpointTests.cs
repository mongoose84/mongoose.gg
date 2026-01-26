using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RiotProxy.Tests;

public class OverviewEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie");
        return cookies.First(c => c.Contains("mongoose-auth"));
    }

    [Fact]
    public async Task Overview_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/overview/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overview_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);
        
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Overview_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/invalid");
        req.Headers.Add("Cookie", authCookie);
        
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Overview_returns_player_header_with_linked_account()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        
        // Add a Riot account for the tester user (userId = 1)
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);
        
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.PlayerHeader.Should().NotBeNull();
        body.PlayerHeader.SummonerName.Should().Be("TestPlayer#NA1");
        body.PlayerHeader.Level.Should().Be(100);
        body.PlayerHeader.Region.Should().Be("NA1");
        body.PlayerHeader.ActiveContexts.Should().Contain("Solo");
    }

    [Fact]
    public async Task Overview_returns_rank_snapshot_with_default_queue()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        
        // Add a Riot account with solo rank
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
            soloLp: 75
        );
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);
        
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.RankSnapshot.Should().NotBeNull();
        body.RankSnapshot.PrimaryQueueLabel.Should().Be("Ranked Solo/Duo");
    }

    [Fact]
    public async Task Overview_returns_empty_goals_and_actions()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);
        
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.ActiveGoals.Should().BeEmpty();
        body.SuggestedActions.Should().BeEmpty();
    }

    // Response DTOs for deserialization
    private record OverviewResponse(
        PlayerHeader PlayerHeader,
        RankSnapshot RankSnapshot,
        LastMatch? LastMatch,
        GoalPreview[] ActiveGoals,
        SuggestedAction[] SuggestedActions
    );
    
    private record PlayerHeader(string SummonerName, int Level, string Region, string ProfileIconUrl, string[] ActiveContexts);
    private record RankSnapshot(string PrimaryQueueLabel, string? Rank, int? Lp, int LpDeltaLast20, int Last20Wins, int Last20Losses, int[] LpDeltasLast20, bool[] WlLast20);
    private record LastMatch(string MatchId, string ChampionIconUrl, string ChampionName, string Result, string Kda, long Timestamp);
    private record GoalPreview(string GoalId, string Title, string Context, double Progress);
    private record SuggestedAction(string ActionId, string Text, string DeepLink, int Priority);
}

