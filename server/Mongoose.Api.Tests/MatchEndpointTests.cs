using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Tests.TestWebApplicationFactory;

namespace Mongoose.Api.Tests;

/// <summary>
/// Tests for Match endpoints: MatchList, MatchDetails, MatchNarrative.
/// </summary>
public class MatchEndpointTests
{
    private static string BuildAccountId(long userId, string puuid) =>
        Mongoose.Api.Application.Services.PuuidResolutionService.BuildAccountId(userId, puuid);

    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    // ============================================================================
    // MatchListEndpoint Tests
    // ============================================================================

    [Fact]
    public async Task MatchList_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/matches/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MatchList_returns_bad_request_for_invalid_userId()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/invalid");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MatchList_returns_forbidden_when_accessing_other_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // User 1 is logged in, trying to access user 999's data
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/999");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MatchList_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchList_returns_empty_matches_with_linked_account_but_no_matches()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Add a Riot account and link it
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().BeEmpty();
        body.TotalMatches.Should().Be(0);
        body.QueueType.Should().Be("all");
    }

    [Fact]
    public async Task MatchList_accepts_queue_type_filter()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1?queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.QueueType.Should().Be("ranked_solo");
    }

    [Fact]
    public async Task MatchList_returns_matches_with_correct_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        // Add a match with participant data
        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "test-puuid-123",
            ChampionId: 1,
            ChampionName: "Annie",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 10,
            Deaths: 2,
            Assists: 5,
            CreepScore: 200,
            GoldEarned: 12000,
            TeamId: 100
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(1);
        body.TotalMatches.Should().Be(1);
        body.Matches[0].MatchId.Should().Be("NA1_12345");
        body.Matches[0].ChampionName.Should().Be("Annie");
        body.Matches[0].Role.Should().Be("MIDDLE");
        body.Matches[0].Win.Should().BeTrue();
        body.Matches[0].Kills.Should().Be(10);
        body.Matches[0].Deaths.Should().Be(2);
        body.Matches[0].Assists.Should().Be(5);
    }

    // ============================================================================
    // MatchDetailsEndpoint Tests
    // ============================================================================

    [Fact]
    public async Task MatchDetails_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/matches/NA1_12345/details");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MatchDetails_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/NA1_12345/details");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchDetails_returns_forbidden_when_account_not_owned()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    // Trying to access with an unowned accountId
    using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/NA1_12345/details?accountId=acc_unowned");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MatchDetails_returns_forbidden_when_free_tier_uses_non_primary_linked_puuid()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "free");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-primary", "Main", "NA1", "Main#NA1", 100, 42);
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-secondary", "Alt", "NA1", "Alt#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-secondary", isPrimary: false);
        var secondaryAccountId = BuildAccountId(1, "test-puuid-secondary");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/details?accountId={secondaryAccountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MatchDetails_returns_ok_when_pro_tier_uses_non_primary_linked_puuid_and_match_exists()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-primary", "Main", "NA1", "Main#NA1", 100, 42);
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-secondary", "Alt", "NA1", "Alt#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-secondary", isPrimary: false);
        var secondaryAccountId = BuildAccountId(1, "test-puuid-secondary");

        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "test-puuid-secondary",
            ChampionId: 103,
            ChampionName: "Ahri",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 9,
            Deaths: 2,
            Assists: 11,
            CreepScore: 225,
            GoldEarned: 13500,
            TeamId: 100,
            DamageDealt: 31000,
            DamageTaken: 12000,
            VisionScore: 24
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/details?accountId={secondaryAccountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchDetailsResponse>();
        body.Should().NotBeNull();
        body!.Match.Should().NotBeNull();
        body.Match.MatchId.Should().Be("NA1_12345");
        body.Match.ChampionName.Should().Be("Ahri");
    }

    [Fact]
    public async Task MatchDetails_returns_not_found_when_match_not_found()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NONEXISTENT/details?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchDetails_returns_match_with_details()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "test-puuid-123",
            ChampionId: 1,
            ChampionName: "Annie",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 10,
            Deaths: 2,
            Assists: 5,
            CreepScore: 200,
            GoldEarned: 12000,
            TeamId: 100,
            DamageDealt: 25000,
            DamageTaken: 15000,
            VisionScore: 25
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/details?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchDetailsResponse>();
        body.Should().NotBeNull();
        body!.Match.Should().NotBeNull();
        body.Match.MatchId.Should().Be("NA1_12345");
        body.Match.ChampionName.Should().Be("Annie");
        body.Match.DamageDealt.Should().Be(25000);
        body.Match.VisionScore.Should().Be(25);
        body.Match.DragonsParticipated.Should().Be(0);
    }

    [Fact]
    public async Task MatchDetails_returns_dragonsParticipated_field_in_response()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "test-puuid-123",
            ChampionId: 1,
            ChampionName: "Annie",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 5,
            Deaths: 2,
            Assists: 3,
            CreepScore: 150,
            GoldEarned: 10000,
            TeamId: 100
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/details?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchDetailsResponse>();
        body.Should().NotBeNull();
        body!.Match.Should().NotBeNull();
        body.Match.DragonsParticipated.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task MatchDetails_returns_dragonsParticipated_as_zero_when_no_participant_objectives()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "test-puuid-123",
            ChampionId: 1,
            ChampionName: "Annie",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 5,
            Deaths: 2,
            Assists: 3,
            CreepScore: 150,
            GoldEarned: 10000,
            TeamId: 100
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/details?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchDetailsResponse>();
        body.Should().NotBeNull();
        body!.Match.DragonsParticipated.Should().Be(0);
    }

    // ============================================================================
    // MatchNarrativeEndpoint Tests
    // ============================================================================

    [Fact]
    public async Task MatchNarrative_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/matches/NA1_12345/narrative");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MatchNarrative_returns_not_found_when_no_riot_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/NA1_12345/narrative");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchNarrative_returns_forbidden_when_account_not_owned()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/NA1_12345/narrative?accountId=acc_unowned");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MatchNarrative_returns_not_found_when_match_not_found()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NONEXISTENT/narrative?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchNarrative_returns_not_found_when_user_not_in_match()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        // Add match but without the user's puuid
        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345",
            Puuid: "other-puuid",
            ChampionId: 1,
            ChampionName: "Annie",
            Role: "MIDDLE",
            Lane: "MIDDLE",
            Win: true,
            Kills: 5,
            Deaths: 2,
            Assists: 3,
            CreepScore: 150,
            GoldEarned: 10000,
            TeamId: 100
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/narrative?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MatchNarrative_returns_lane_matchups_for_standard_game()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        factory.MatchesRepository.AddMatch("NA1_12345", queueId: 420);

        // Add ally team (Team 100)
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345", Puuid: "test-puuid-123", ChampionId: 1, ChampionName: "Annie",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true, Kills: 10, Deaths: 2, Assists: 5,
            CreepScore: 200, GoldEarned: 12000, TeamId: 100, GoldDiffAt10: 500
        ));
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345", Puuid: "ally-top", ChampionId: 2, ChampionName: "Garen",
            Role: "TOP", Lane: "TOP", Win: true, Kills: 5, Deaths: 3, Assists: 8,
            CreepScore: 180, GoldEarned: 11000, TeamId: 100, GoldDiffAt10: 200
        ));

        // Add enemy team (Team 200)
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345", Puuid: "enemy-mid", ChampionId: 3, ChampionName: "Ahri",
            Role: "MIDDLE", Lane: "MIDDLE", Win: false, Kills: 4, Deaths: 6, Assists: 3,
            CreepScore: 170, GoldEarned: 10000, TeamId: 200, GoldDiffAt10: -500
        ));
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_12345", Puuid: "enemy-top", ChampionId: 4, ChampionName: "Darius",
            Role: "TOP", Lane: "TOP", Win: false, Kills: 3, Deaths: 5, Assists: 2,
            CreepScore: 160, GoldEarned: 9500, TeamId: 200, GoldDiffAt10: -200
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_12345/narrative?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("\"puuid\"");
        rawJson.Should().Contain("\"isUserParticipant\"");

        var body = await response.Content.ReadFromJsonAsync<MatchNarrativeResponse>();
        body.Should().NotBeNull();
        body!.MatchId.Should().Be("NA1_12345");
        body.UserRole.Should().Be("MIDDLE");
        body.IsAram.Should().BeFalse();
        body.LaneMatchups.Should().HaveCount(2); // TOP and MIDDLE
    }

    [Fact]
    public async Task MatchNarrative_detects_aram_games()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-123", "TestPlayer", "NA1", "TestPlayer#NA1", 100, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-123", isPrimary: true);
        var accountId = BuildAccountId(1, "test-puuid-123");

        factory.MatchesRepository.AddMatch("NA1_ARAM123", queueId: 450);

        // ARAM - all roles are UNKNOWN
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_ARAM123", Puuid: "test-puuid-123", ChampionId: 1, ChampionName: "Annie",
            Role: "UNKNOWN", Lane: null, Win: true, Kills: 8, Deaths: 5, Assists: 12,
            CreepScore: 50, GoldEarned: 10000, TeamId: 100, DamageShare: 25
        ));
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_ARAM123", Puuid: "enemy-1", ChampionId: 2, ChampionName: "Brand",
            Role: "UNKNOWN", Lane: null, Win: false, Kills: 6, Deaths: 8, Assists: 10,
            CreepScore: 45, GoldEarned: 9000, TeamId: 200, DamageShare: 28
        ));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/matches/NA1_ARAM123/narrative?accountId={accountId}");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchNarrativeResponse>();
        body.Should().NotBeNull();
        body!.IsAram.Should().BeTrue();
    }

    // ============================================================================
    // MA-07: Overall Mode — Interleaved Match History Tests
    // ============================================================================

    [Fact]
    public async Task MatchList_with_account_all_returns_matches_from_multiple_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-main", "MainAcc", "NA1", "MainAcc#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-alt", "AltAcc", "EUW", "AltAcc#EUW", 50, 2);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-alt", isPrimary: false);

        factory.MatchesRepository.AddMatch("NA1_001", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_001", Puuid: "puuid-main", ChampionId: 1, ChampionName: "Annie",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true,
            Kills: 5, Deaths: 2, Assists: 3, CreepScore: 150, GoldEarned: 10000, TeamId: 100));

        factory.MatchesRepository.AddMatch("EUW_002", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "EUW_002", Puuid: "puuid-alt", ChampionId: 99, ChampionName: "Lux",
            Role: "MIDDLE", Lane: "MIDDLE", Win: false,
            Kills: 3, Deaths: 5, Assists: 7, CreepScore: 130, GoldEarned: 9000, TeamId: 100));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(2);
        body.Matches.Should().Contain(m => m.MatchId == "NA1_001");
        body.Matches.Should().Contain(m => m.MatchId == "EUW_002");
    }

    [Fact]
    public async Task MatchList_with_account_all_is_sorted_by_game_start_time_descending()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-main", "MainAcc", "NA1", "MainAcc#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-alt", "AltAcc", "EUW", "AltAcc#EUW", 50, 2);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-alt", isPrimary: false);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        factory.MatchesRepository.AddMatch("EUW_OLDER", queueId: 420, gameStartTime: now - 3_600_000); // 1h ago
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "EUW_OLDER", Puuid: "puuid-alt", ChampionId: 99, ChampionName: "Lux",
            Role: "MIDDLE", Lane: "MIDDLE", Win: false,
            Kills: 2, Deaths: 4, Assists: 6, CreepScore: 110, GoldEarned: 8000, TeamId: 200));

        factory.MatchesRepository.AddMatch("NA1_NEWER", queueId: 420, gameStartTime: now - 1_800_000); // 30min ago
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_NEWER", Puuid: "puuid-main", ChampionId: 1, ChampionName: "Annie",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true,
            Kills: 8, Deaths: 1, Assists: 4, CreepScore: 160, GoldEarned: 11000, TeamId: 100));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(2);
        body.Matches[0].MatchId.Should().Be("NA1_NEWER");
        body.Matches[1].MatchId.Should().Be("EUW_OLDER");
    }

    [Fact]
    public async Task MatchList_with_account_all_includes_account_info_on_each_match()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-main", "FakerMain", "EUW", "FakerMain#EUW", 200, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-alt", "SmurfAcc", "NA1", "SmurfAcc#NA1", 80, 2);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-alt", isPrimary: false);

        factory.MatchesRepository.AddMatch("EUW_001", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "EUW_001", Puuid: "puuid-main", ChampionId: 238, ChampionName: "Zed",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true,
            Kills: 12, Deaths: 2, Assists: 4, CreepScore: 220, GoldEarned: 14000, TeamId: 100));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(1);
        var match = body.Matches[0];
        match.AccountGameName.Should().Be("FakerMain");
        match.AccountRegion.Should().Be("EUW");
    }

    [Fact]
    public async Task MatchList_single_account_does_not_include_account_tag()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-main", "MainAcc", "NA1", "MainAcc#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);

        factory.MatchesRepository.AddMatch("NA1_001", queueId: 420);
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_001", Puuid: "puuid-main", ChampionId: 1, ChampionName: "Annie",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true,
            Kills: 5, Deaths: 2, Assists: 3, CreepScore: 150, GoldEarned: 10000, TeamId: 100));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(1);
        body.Matches[0].AccountGameName.Should().BeNull();
        body.Matches[0].AccountRegion.Should().BeNull();
    }

    [Fact]
    public async Task MatchList_with_account_all_queue_filter_applies_across_all_accounts()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-main", "MainAcc", "NA1", "MainAcc#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-alt", "AltAcc", "EUW", "AltAcc#EUW", 50, 2);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-main", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-alt", isPrimary: false);

        // Ranked solo match on main
        factory.MatchesRepository.AddMatch("NA1_RANKED", queueId: 420); // ranked solo
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "NA1_RANKED", Puuid: "puuid-main", ChampionId: 1, ChampionName: "Annie",
            Role: "MIDDLE", Lane: "MIDDLE", Win: true,
            Kills: 5, Deaths: 2, Assists: 3, CreepScore: 150, GoldEarned: 10000, TeamId: 100));

        // ARAM match on alt — should be excluded by ranked_solo filter
        factory.MatchesRepository.AddMatch("EUW_ARAM", queueId: 450); // ARAM
        factory.MatchesRepository.AddParticipant(new FakeParticipantData(
            MatchId: "EUW_ARAM", Puuid: "puuid-alt", ChampionId: 99, ChampionName: "Lux",
            Role: "UTILITY", Lane: "NONE", Win: false,
            Kills: 4, Deaths: 6, Assists: 9, CreepScore: 20, GoldEarned: 7500, TeamId: 200));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/matches/1?accountId=all&queueType=ranked_solo");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchListResponse>();
        body.Should().NotBeNull();
        body!.Matches.Should().HaveCount(1);
        body.Matches[0].MatchId.Should().Be("NA1_RANKED");
    }

    // Response DTOs for deserialization
    private record MatchListResponse(MatchListSummaryItem[] Matches, Dictionary<string, RoleBaseline> BaselinesByRole, string QueueType, int TotalMatches);
    private record MatchListSummaryItem(string MatchId, int QueueId, string QueueType, int ChampionId, string ChampionName, string Role, bool Win, int Kills, int Deaths, int Assists, string? AccountGameName = null, string? AccountTagLine = null, string? AccountRegion = null);
    private record RoleBaseline(string Role, int GamesCount, double AvgKills, double AvgDeaths, double AvgAssists);
    private record MatchDetailsResponse(MatchDetailsItem Match, RoleBaseline? Baseline);
    private record MatchDetailsItem(string MatchId, int QueueId, string QueueType, int ChampionId, string ChampionName, string Role, bool Win, int Kills, int Deaths, int Assists, int DamageDealt, int VisionScore, int DragonsParticipated);
    private record MatchNarrativeResponse(string MatchId, string UserRole, LaneMatchup[] LaneMatchups, bool IsAram);
    private record LaneMatchup(string Role, MatchupParticipant AllyParticipant, MatchupParticipant EnemyParticipant, string LaneWinner);
    private record MatchupParticipant(string Puuid, string ChampionName, int Kills, int Deaths, int Assists);
}

