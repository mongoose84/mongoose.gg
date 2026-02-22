using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

[Collection("EnvIsolation")]
public class FeedbackEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    // Response DTOs for deserialization
    private record FeedbackResponse(bool success, string message);
    private record ErrorResponse(string error);

    [Fact]
    public async Task Feedback_bug_report_creates_github_issue_with_bug_label()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "bug",
                summary = "Button doesn't work",
                details = "When I click the submit button, nothing happens",
                route = "/app/solo",
                environment = "production",
                browser = "Chrome 120",
                os = "Windows 11"
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var result = await response.Content.ReadFromJsonAsync<FeedbackResponse>();
        result.Should().NotBeNull();
        result!.success.Should().BeTrue();
        result.message.Should().Contain("Thank you");

        // Verify GitHub issue was created with correct data
        factory.GitHubService.CreatedIssues.Should().HaveCount(1);
        var issue = factory.GitHubService.CreatedIssues[0];
        issue.Title.Should().StartWith("[Bug]");
        issue.Title.Should().Contain("Button doesn't work");
        issue.Labels.Should().Contain("bug");
        issue.Labels.Should().Contain("user-feedback");
        issue.Body.Should().Contain("When I click the submit button");
        issue.Body.Should().Contain("/app/solo");
        issue.Body.Should().Contain("production");
    }

    [Fact]
    public async Task Feedback_feature_request_creates_github_issue_with_enhancement_label()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode",
                details = "Would be nice to have a dark theme option"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        
        factory.GitHubService.CreatedIssues.Should().HaveCount(1);
        var issue = factory.GitHubService.CreatedIssues[0];
        issue.Title.Should().StartWith("[Feature Request]");
        issue.Title.Should().Contain("Add dark mode");
        issue.Labels.Should().Contain("enhancement");
        issue.Labels.Should().Contain("user-feedback");
    }

    [Fact]
    public async Task Feedback_returns_bad_request_when_type_missing()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                summary = "Some summary",
                details = "Some details"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("type");
    }

    [Fact]
    public async Task Feedback_returns_bad_request_when_type_invalid()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "invalid-type",
                summary = "Some summary"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("bug").And.Contain("feature");
    }

    [Fact]
    public async Task Feedback_returns_bad_request_when_summary_missing()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "bug",
                details = "Some details"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("summary");
    }

    [Fact]
    public async Task Feedback_bug_requires_details()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "bug",
                summary = "Something is broken"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("details");
    }

    [Fact]
    public async Task Feedback_feature_does_not_require_details()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.GitHubService.CreatedIssues.Should().HaveCount(1);
    }

    [Fact]
    public async Task Feedback_returns_service_unavailable_when_github_fails()
    {
        using var factory = new TestWebApplicationFactory();
        factory.GitHubService.SetupFailure("GitHub service unavailable");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("Unable to submit feedback");
        // Should NOT contain GitHub-specific details
        result.error.Should().NotContain("GitHub");
    }

    [Fact]
    public async Task Feedback_returns_bad_request_when_summary_too_long()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var longSummary = new string('x', 201);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = longSummary
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("200 characters");
    }

    [Fact]
    public async Task Feedback_captures_user_id_when_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode"
            })
        };
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var issue = factory.GitHubService.CreatedIssues[0];
        issue.Body.Should().Contain("User ID");
        issue.Body.Should().NotContain("_anonymous_");
    }

    [Fact]
    public async Task Feedback_works_without_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode"
            })
        };
        // No auth cookie

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var issue = factory.GitHubService.CreatedIssues[0];
        issue.Body.Should().Contain("_anonymous_");
    }

    [Fact]
    public async Task Feedback_returns_service_unavailable_when_github_not_configured()
    {
        using var factory = new TestWebApplicationFactory();
        factory.GitHubService.SetupNotConfigured();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Add dark mode"
            })
        };

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("Unable to submit feedback");
        // Should NOT leak configuration details
        result.error.Should().NotContain("configured");
        result.error.Should().NotContain("GitHub");
    }

    [Fact]
    public async Task Feedback_rate_limits_after_5_requests_per_hour()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Make 5 successful requests (at rate limit)
        for (int i = 0; i < 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
            {
                Content = JsonContent.Create(new
                {
                    type = "feature",
                    summary = $"Feature request {i + 1}"
                })
            };
            // Add X-Forwarded-For header to simulate a specific client IP
            req.Headers.Add("X-Forwarded-For", "192.168.1.100");

            var response = await client.SendAsync(req);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted, $"Request {i + 1} should succeed");
        }

        // 6th request should be rate limited
        using var rateLimitedReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "This should be rate limited"
            })
        };
        rateLimitedReq.Headers.Add("X-Forwarded-For", "192.168.1.100");

        var rateLimitedResponse = await client.SendAsync(rateLimitedReq);

        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var result = await rateLimitedResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        result!.error.Should().Contain("Too many feedback submissions");

        // Should include rate limit headers
        rateLimitedResponse.Headers.Should().ContainKey("X-RateLimit-Remaining");
        rateLimitedResponse.Headers.GetValues("X-RateLimit-Remaining").First().Should().Be("0");
        rateLimitedResponse.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task Feedback_rate_limit_tracks_by_user_id_when_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        // Make 5 successful requests (at rate limit)
        for (int i = 0; i < 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
            {
                Content = JsonContent.Create(new
                {
                    type = "feature",
                    summary = $"Feature request {i + 1}"
                })
            };
            req.Headers.Add("Cookie", authCookie);
            // Use different IPs to prove user ID is used, not IP
            req.Headers.Add("X-Forwarded-For", $"192.168.1.{i + 1}");

            var response = await client.SendAsync(req);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted, $"Request {i + 1} should succeed");
        }

        // 6th request should be rate limited (user ID tracking, not IP)
        using var rateLimitedReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "This should be rate limited"
            })
        };
        rateLimitedReq.Headers.Add("Cookie", authCookie);
        rateLimitedReq.Headers.Add("X-Forwarded-For", "192.168.1.200"); // Different IP

        var rateLimitedResponse = await client.SendAsync(rateLimitedReq);

        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Feedback_rate_limit_separate_for_different_ips()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // First IP makes 5 requests (reaches limit)
        for (int i = 0; i < 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
            {
                Content = JsonContent.Create(new
                {
                    type = "feature",
                    summary = $"Feature request {i + 1}"
                })
            };
            req.Headers.Add("X-Forwarded-For", "10.0.0.1");
            var response = await client.SendAsync(req);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        // Second IP should still be able to submit (separate rate limit)
        using var differentIpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/feedback")
        {
            Content = JsonContent.Create(new
            {
                type = "feature",
                summary = "Different IP request"
            })
        };
        differentIpReq.Headers.Add("X-Forwarded-For", "10.0.0.2");

        var differentIpResponse = await client.SendAsync(differentIpReq);

        differentIpResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}

