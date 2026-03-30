using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Tests;

public class CsPerMinuteTrendEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task CsPerMinuteTrend_returns_multi_account_data_with_account_game_name_in_overall_mode()
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

        factory.TrendRepository.SetCsPerMinuteData("test-puuid-primary", new[]
        {
            new CsPerMinuteTrendPoint(
                MatchId: "NA1_60001",
                GameIndex: 1,
                Timestamp: DateTime.UtcNow.AddDays(-3),
                TotalCs: 280,
                CsPerMinute: 7.8,
                GameDurationMinutes: 35.9,
                ChampionName: "Jinx",
                Role: "BOTTOM"
            )
        });

        factory.TrendRepository.SetCsPerMinuteData("test-puuid-alt", new[]
        {
            new CsPerMinuteTrendPoint(
                MatchId: "NA1_60002",
                GameIndex: 2,
                Timestamp: DateTime.UtcNow.AddDays(-2),
                TotalCs: 240,
                CsPerMinute: 6.9,
                GameDurationMinutes: 34.7,
                ChampionName: "Ezreal",
                Role: "BOTTOM"
            )
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/trends/cs-per-minute/1?accountId=all");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.TryGetProperty("csPerMinuteTrend", out var trendArray).Should().BeTrue();
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
}
