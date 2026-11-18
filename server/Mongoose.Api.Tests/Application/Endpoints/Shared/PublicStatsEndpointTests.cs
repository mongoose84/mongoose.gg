using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class PublicStatsEndpointTests
{
    [Fact]
    public async Task PublicStats_returns_ok_without_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/public/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicStats_returns_total_matches_and_active_players()
    {
        using var factory = new TestWebApplicationFactory();

        // Add a match so totalMatches > 0
        factory.MatchesRepository.AddMatch("EUW1_001");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/public/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("totalMatches", out var totalMatches).Should().BeTrue();
        doc.RootElement.TryGetProperty("activePlayers", out var activePlayers).Should().BeTrue();
        totalMatches.GetInt64().Should().BeGreaterThanOrEqualTo(1);
        activePlayers.GetInt64().Should().BeGreaterThanOrEqualTo(1, "pre-seeded tester user is active");
    }

    [Fact]
    public async Task PublicStats_total_matches_reflects_match_repository_count()
    {
        using var factory = new TestWebApplicationFactory();

        factory.MatchesRepository.AddMatch("NA1_001");
        factory.MatchesRepository.AddMatch("NA1_002");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/v2/public/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("totalMatches").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task PublicStats_active_players_reflects_user_repository_count()
    {
        using var factory = new TestWebApplicationFactory();

        // The factory starts with one active user ("tester"); inactive users should not be counted
        factory.UsersRepository.AddInactiveUser("inactive1", "inactive1@test.com", "pass1234");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/v2/public/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("activePlayers").GetInt64().Should().Be(1);
    }

    [Fact]
    public async Task PublicStats_returns_429_when_rate_limit_exceeded()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Exhaust the 60-per-minute limit
        for (var i = 0; i < 60; i++)
        {
            await client.GetAsync("/api/v2/public/stats");
        }

        var response = await client.GetAsync("/api/v2/public/stats");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("X-RateLimit-Remaining").Should().BeTrue();
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
