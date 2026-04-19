using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class OverviewEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
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
    public async Task Overview_returns_403_when_accessing_another_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/2");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        // Link the account to the user (M:M relationship)
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

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
        // Link the account to the user (M:M relationship)
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

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
        // Link the account to the user (M:M relationship)
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

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

    [Fact]
    public async Task Overview_returns_most_played_champion_when_available()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        factory.OverviewStatsRepository.SetMostPlayedChampion("test-puuid-123", "Ahri", 28);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.MostPlayedChampion.Should().NotBeNull();
        body.MostPlayedChampion!.ChampionName.Should().Be("Ahri");
        body.MostPlayedChampion.GamesPlayed.Should().Be(28);
        body.MostPlayedChampion.Source.Should().Be("current_season");
    }

    [Fact]
    public async Task Overview_returns_null_most_played_champion_when_unavailable()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.MostPlayedChampion.Should().BeNull();
    }

    [Fact]
    public async Task Overview_returns_session_stats_non_null()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.SessionStats.Should().NotBeNull();
        body.SessionStats!.GamesToday.Should().Be(0);
        body.SessionStats.GamesThisWeek.Should().Be(0);
    }

    [Fact]
    public async Task Overview_returns_session_stats_with_configured_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        factory.OverviewStatsRepository.AddSessionData("test-puuid-123",
            gamesToday: 3, winsToday: 2, lossesToday: 1,
            gamesThisWeek: 10, winsThisWeek: 6, lossesThisWeek: 4);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.SessionStats.Should().NotBeNull();
        body.SessionStats!.GamesToday.Should().Be(3);
        body.SessionStats.WinsToday.Should().Be(2);
        body.SessionStats.LossesToday.Should().Be(1);
        body.SessionStats.GamesThisWeek.Should().Be(10);
        body.SessionStats.WinsThisWeek.Should().Be(6);
        body.SessionStats.LossesThisWeek.Should().Be(4);
    }

    [Fact]
    public async Task Overview_returns_survival_stats_non_null()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.SurvivalStats.Should().NotBeNull();
        body.SurvivalStats!.TotalGames.Should().Be(0);
        body.SurvivalStats.AvgDeathsPerGame.Should().Be(0);
    }

    [Fact]
    public async Task Overview_returns_survival_stats_with_configured_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        factory.OverviewStatsRepository.SetSurvivalStats(new Mongoose.Api.Core.QueryModels.SurvivalStatsData(
            AvgDeathsPerGame: 4.5,
            WinRateLowDeaths: 0.65,
            WinRateHighDeaths: 0.2,
            GamesLowDeaths: 8,
            GamesHighDeaths: 5,
            TotalGames: 20
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.SurvivalStats.Should().NotBeNull();
        body.SurvivalStats!.AvgDeathsPerGame.Should().Be(4.5);
        body.SurvivalStats.WinRateLowDeaths.Should().Be(0.65);
        body.SurvivalStats.WinRateHighDeaths.Should().Be(0.2);
        body.SurvivalStats.GamesLowDeaths.Should().Be(8);
        body.SurvivalStats.GamesHighDeaths.Should().Be(5);
        body.SurvivalStats.TotalGames.Should().Be(20);
    }

    [Fact]
    public async Task Overview_all_mode_returns_account_summaries_with_game_counts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-a", "PlayerA", "EUW1", "PlayerA#EUW", 150, 10);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-a", isPrimary: true);
        factory.OverviewStatsRepository.AddSessionData("puuid-a",
            gamesToday: 2, winsToday: 1, lossesToday: 1,
            gamesThisWeek: 7, winsThisWeek: 4, lossesThisWeek: 3);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body.Should().NotBeNull();
        body!.AccountSummaries.Should().NotBeNull();
        body.AccountSummaries!.Should().NotBeEmpty();

        var accountA = body.AccountSummaries!.First(a => a.GameName == "PlayerA");
        accountA.GamesToday.Should().Be(2);
        accountA.GamesThisWeek.Should().Be(7);
    }

    [Fact]
    public async Task Overview_rank_snapshot_does_not_include_wl_fields()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1, puuid: "test-puuid-123", gameName: "TestPlayer", region: "NA1",
            summonerName: "TestPlayer#NA1", summonerLevel: 100, profileIconId: 42,
            soloTier: "PLATINUM", soloRank: "I", soloLp: 50);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("wlLast20");
        raw.Should().NotContain("last20Wins");
        raw.Should().NotContain("last20Losses");
        var body = await System.Text.Json.JsonSerializer.DeserializeAsync<OverviewResponse>(
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(raw)),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body!.RankSnapshot.Rank.Should().Be("PLATINUM I");
    }

    [Fact]
    public async Task Overview_survival_stats_uses_gold_thresholds_for_gold_rank()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccountWithRank(
            userId: 1, puuid: "test-puuid-123", gameName: "TestPlayer", region: "NA1",
            summonerName: "TestPlayer#NA1", summonerLevel: 100, profileIconId: 42,
            soloTier: "GOLD", soloRank: "II", soloLp: 75);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body!.SurvivalStats.Should().NotBeNull();
        body.SurvivalStats!.LowDeathThreshold.Should().Be(4);
        body.SurvivalStats.HighDeathThreshold.Should().Be(6);
    }

    [Fact]
    public async Task Overview_survival_stats_uses_default_thresholds_for_unranked_player()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        body!.SurvivalStats.Should().NotBeNull();
        body.SurvivalStats!.LowDeathThreshold.Should().Be(4);
        body.SurvivalStats.HighDeathThreshold.Should().Be(7);
    }

    [Fact]
    public async Task Overview_survival_stats_response_does_not_contain_removed_fields()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/overview/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("deathsBefore10Pct");
        raw.Should().NotContain("winRateAtOrBelow3Deaths");
        raw.Should().NotContain("winRateAbove5Deaths");
        raw.Should().NotContain("gamesAtOrBelow3Deaths");
        raw.Should().NotContain("gamesAbove5Deaths");
        raw.Should().Contain("winRateLowDeaths");
        raw.Should().Contain("winRateHighDeaths");
        raw.Should().Contain("lowDeathThreshold");
        raw.Should().Contain("highDeathThreshold");
    }

    // Response DTOs for deserialization
    private record OverviewResponse(
        PlayerHeader PlayerHeader,
        RankSnapshot RankSnapshot,
        LastMatch? LastMatch,
        MostPlayedChampion? MostPlayedChampion,
        GoalPreview[] ActiveGoals,
        SuggestedAction[] SuggestedActions,
        AccountSummary[]? AccountSummaries = null,
        CombinedStats? CombinedStats = null,
        SessionStats? SessionStats = null,
        SurvivalStats? SurvivalStats = null
    );

    private record PlayerHeader(string SummonerName, int Level, string Region, string ProfileIconUrl, string[] ActiveContexts);
    private record RankSnapshot(string PrimaryQueueLabel, string? Rank, int? Lp);
    private record LastMatch(string MatchId, string ChampionIconUrl, string ChampionName, string Result, string Kda, long Timestamp);
    private record MostPlayedChampion(string ChampionName, int GamesPlayed, string Source);
    private record GoalPreview(string GoalId, string Title, string Context, double Progress);
    private record SuggestedAction(string ActionId, string Text, string DeepLink, int Priority);
    private record AccountSummary(string AccountId, string GameName, string TagLine, string Region, string? Rank, int? Lp, int GamesToday, int GamesThisWeek);
    private record CombinedStats(int TotalGames, double WinRate, double AvgKda);
    private record SessionStats(int GamesToday, int WinsToday, int LossesToday, double? AvgKdaToday, SessionChampion? BestChampionToday, int GamesThisWeek, int WinsThisWeek, int LossesThisWeek, double? AvgKdaThisWeek);
    private record SessionChampion(string ChampionName, int Wins, int Losses, double AvgKda);
    private record SurvivalStats(double AvgDeathsPerGame, double? WinRateLowDeaths, double? WinRateHighDeaths, int GamesLowDeaths, int GamesHighDeaths, int LowDeathThreshold, int HighDeathThreshold, int TotalGames);
}

