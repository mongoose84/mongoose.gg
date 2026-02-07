using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RiotProxy.Tests;

[Collection("EnvIsolation")]
public class AnalyticsEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.First();
        var authCookie = cookie.Split(';', 2)[0];
        return authCookie;
    }

    // Response DTOs for deserialization
    private record TrackEventResponse(bool success);
    private record TrackBatchResponse(bool success, int count);
    private record ErrorResponse(string error);

    [Fact]
    public async Task Analytics_track_returns_success_when_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics")
        {
            Content = JsonContent.Create(new
            {
                eventName = "match:select",
                payload = new { matchId = "EUW1_12345", matchIndex = 0 },
                sessionId = "test-session-123"
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackEventResponse>();
        result.Should().NotBeNull();
        result!.success.Should().BeTrue();
    }

    [Fact]
    public async Task Analytics_track_returns_bad_request_when_eventName_missing()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics")
        {
            Content = JsonContent.Create(new
            {
                eventName = "",
                sessionId = "test-session"
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("eventName");
    }

    [Fact]
    public async Task Analytics_track_returns_bad_request_when_eventName_too_long()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var longEventName = new string('x', 101);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics")
        {
            Content = JsonContent.Create(new
            {
                eventName = longEventName,
                sessionId = "test-session"
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("100 characters");
    }

    [Fact]
    public async Task Analytics_batch_returns_success_with_count()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var events = new List<object>
        {
            new { eventName = "match:select", payload = new Dictionary<string, object> { ["matchId"] = "EUW1_1" }, sessionId = "sess1" },
            new { eventName = "match:details_view", payload = new Dictionary<string, object> { ["matchId"] = "EUW1_1" }, sessionId = "sess1" },
            new { eventName = "match:section_toggle", payload = new Dictionary<string, object> { ["section"] = "personal_stats" }, sessionId = "sess1" }
        };
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/batch")
        {
            Content = JsonContent.Create(new { events })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackBatchResponse>();
        result.Should().NotBeNull();
        result!.success.Should().BeTrue();
        result.count.Should().Be(3);
    }

    [Fact]
    public async Task Analytics_batch_returns_bad_request_when_events_empty()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/batch")
        {
            Content = JsonContent.Create(new { events = Array.Empty<object>() })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("events");
    }
}

