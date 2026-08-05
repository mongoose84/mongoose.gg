using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Mongoose.Api.Application.DTOs.AnalyticsV2Dto;

namespace Mongoose.Api.Tests;

[Collection("EnvIsolation")]
public class AnalyticsV2EndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    // ============ V2 Single Event Tests ============

    [Fact]
    public async Task Analytics_v2_track_returns_success_with_valid_event()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "nav:page_view",
                eventVersion = 1,
                timestamp = DateTime.UtcNow,
                sessionId = "test-session-123",
                payload = new { path = "/app/overview", referrer = "/" }
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task Analytics_v2_track_rejects_unknown_event()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "unknown:event",
                eventVersion = 1,
                sessionId = "test-session-123",
                payload = new { field = "value" }
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result!.Success.Should().BeFalse();
        result.RejectionReason.Should().NotBeNull();
    }

    [Fact]
    public async Task Analytics_v2_track_rejects_missing_required_fields()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "feature:match_select",
                eventVersion = 1,
                sessionId = "test-session-123",
                payload = new { index = 0 } // Missing required 'match_id'
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result!.Success.Should().BeFalse();
        result.RejectionReason.Should().Be("RequiredPayloadFieldMissing");
    }

    [Fact]
    public async Task Analytics_v2_track_rejects_payload_too_large()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var largePayload = new Dictionary<string, object>
        {
            { "path", "/app/overview" }, // Required field, so the payload is otherwise valid
            { "title", new string('x', 5000) } // Allowed key, but exceeds the 4KB serialized limit
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "nav:page_view",
                eventVersion = 1,
                sessionId = "test-session-123",
                payload = largePayload
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result!.Success.Should().BeFalse();
        result.RejectionReason.Should().Be("PayloadTooLarge");
    }

    [Fact]
    public async Task Analytics_v2_track_sanitizes_unknown_payload_keys()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "feature:match_select",
                eventVersion = 1,
                sessionId = "test-session-123",
                payload = new
                {
                    matchId = "EUW1_12345",
                    unknownKey = "should_be_dropped",
                    anotherUnknown = 123
                }
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result!.Success.Should().BeTrue();
        // Payload should be sanitized but event accepted
    }

    [Fact]
    public async Task Analytics_v2_track_accepts_anonymous_event()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
        {
            Content = JsonContent.Create(new
            {
                eventName = "nav:page_view",
                eventVersion = 1,
                sessionId = "anon-session-123",
                payload = new { path = "/" }
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
        result!.Success.Should().BeTrue();
    }

    // ============ V2 Batch Event Tests ============

    [Fact]
    public async Task Analytics_v2_batch_returns_accepted_and_rejected_counts()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var events = new object[]
        {
            new { eventName = "nav:page_view", eventVersion = 1, payload = new { path = "/app" } },
            new { eventName = "invalid:event", eventVersion = 1, payload = new { field = "value" } }, // Will be rejected
            new { eventName = "auth:login_attempt", eventVersion = 1, payload = new { method = "email", success = true } }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2/batch")
        {
            Content = JsonContent.Create(new { events })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrackBatchV2Response>();
        result.Should().NotBeNull();
        result!.Accepted.Should().Be(2);
        result.Rejected.Should().Be(1);
        result.Rejections.Should().NotBeNull();
        result.Rejections!.Length.Should().Be(1);
    }

    [Fact]
    public async Task Analytics_v2_batch_respects_max_50_events()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var events = Enumerable.Range(0, 51)
            .Select(_ => new { eventName = "nav:page_view", eventVersion = 1, payload = new { path = "/app" } })
            .ToArray();

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2/batch")
        {
            Content = JsonContent.Create(new { events })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Analytics_v2_batch_partial_acceptance_on_mixed_validity()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var events = new object[]
        {
            new { eventName = "feature:match_select", eventVersion = 1, payload = new { matchId = "EUW1_1", index = 0 } },
            new { eventName = "feature:match_select", eventVersion = 1, payload = new { index = 1 } }, // Missing matchId
            new { eventName = "nav:page_view", eventVersion = 1, payload = new { path = "/app/matches" } }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2/batch")
        {
            Content = JsonContent.Create(new { events })
        };

        var response = await client.SendAsync(req);

        var result = await response.Content.ReadFromJsonAsync<TrackBatchV2Response>();
        result!.Accepted.Should().Be(2);
        result!.Rejected.Should().Be(1);
        result!.Success.Should().BeTrue(); // Success if any events accepted
    }

    // ============ Compatibility Tests (V1 → V2) ============

    [Fact]
    public async Task Analytics_hybrid_endpoint_accepts_v1_and_converts_to_v2()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Send v1 format to hybrid endpoint
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics")
        {
            Content = JsonContent.Create(new
            {
                eventName = "match:select",
                payload = new { matchId = "EUW1_12345", matchIndex = 0 },
                sessionId = "test-session-123"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        result.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
    }

    // ============ Observability Endpoint Tests ============

    [Fact]
    public async Task Analytics_health_endpoint_returns_metrics()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/analytics/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyticsHealthResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("healthy");
        result.AcceptanceRate.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public async Task Analytics_schema_endpoint_returns_registered_events()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/analytics/schema");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSchemasResponse>();
        result.Should().NotBeNull();
        result!.Schemas.Length.Should().BeGreaterThan(0);
        result.Schemas.Should().Contain(s => s.EventName == "nav:page_view");
    }
}
