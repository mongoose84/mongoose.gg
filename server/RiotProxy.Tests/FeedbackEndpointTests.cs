using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RiotProxy.Tests;

[Collection("EnvIsolation")]
public class FeedbackEndpointTests
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
}

