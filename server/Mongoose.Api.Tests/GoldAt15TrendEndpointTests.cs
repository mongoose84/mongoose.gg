using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

public class GoldAt15TrendEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task GoldAt15Trend_returns_multi_account_data_with_account_game_name_in_overall_mode()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var user = await factory.UsersRepository.GetByIdAsync(1);
        user.Should().NotBeNull();
        user!.Tier = "pro";
        await factory.UsersRepository.UpsertAsync(user);

        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-primary", "MainPlayer", "NA1", "MainPlayer#NA1", 100, 42);
        factory.RiotAccountsRepository.AddRiotAccount(1, "test-puuid-alt", "AltPlayer", "NA1", "AltPlayer#NA1", 101, 42);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "test-puuid-alt", isPrimary: false);

        factory.TrendRepository.SetGoldAt15Data("test-puuid-primary", new[]
        {
            new GoldAt15TrendPoint(
                MatchId: "NA1_50001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                PlayerGold: 5200,
                OpponentGold: 5000,
                GoldDifferential: 200,
                ChampionName: "Jinx",
                Role: "BOTTOM",
                OpponentChampion: "Caitlyn"
            )
        });

        factory.TrendRepository.SetGoldAt15Data("test-puuid-alt", new[]
        {
            new GoldAt15TrendPoint(
                MatchId: "NA1_50002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                PlayerGold: 5100,
                OpponentGold: 5300,
                GoldDifferential: -200,
                ChampionName: "Ezreal",
                Role: "BOTTOM",
                OpponentChampion: "Lucian"
            )
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/gold-at-15/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("goldAt15Trend", out var trendArray).Should().BeTrue();
        trendArray.GetArrayLength().Should().Be(2);

        var accountNames = trendArray
            .EnumerateArray()
            .Select(point =>
            {
                point.TryGetProperty("accountGameName", out var accountGameName).Should().BeTrue();
                return accountGameName.GetString();
            })
            .ToArray();

        accountNames.Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
        accountNames.Should().Contain("MainPlayer#NA1");
        accountNames.Should().Contain("AltPlayer#NA1");
    }

    [Fact]
    public async Task GoldAt15Trend_returns_401_when_not_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/trends/gold-at-15/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoldAt15Trend_returns_403_when_accessing_another_users_data()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/gold-at-15/2");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GoldAt15Trend_returns_404_when_no_riot_account_linked()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/gold-at-15/1");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
