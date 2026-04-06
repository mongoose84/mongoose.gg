using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class RegisterEndpointTests
{
    // Rate limiting disabled for all tests to avoid interference
    private static TestWebApplicationFactory CreateFactory(IDictionary<string, string?>? extra = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "false"
        };

        if (extra != null)
            foreach (var (k, v) in extra)
                config[k] = v;

        return new TestWebApplicationFactory(config);
    }

    private record RegisterResponse(long userId, string username, string email, bool emailVerified, string message);
    private record ErrorResponse(string error, string? code);

    [Fact]
    public async Task Register_returns_ok_and_sets_auth_cookie_for_new_user()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "newuser123",
            email = "newuser@example.com",
            password = "securePassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Any(c => c.Contains("mongoose-auth=")).Should().BeTrue("auth cookie must be set after registration");

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body.Should().NotBeNull();
        body!.username.Should().Be("newuser123");
        body.email.Should().Be("newuser@example.com");
        body.emailVerified.Should().BeFalse("email is unverified by default");
        body.message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_sends_verification_email_when_not_auto_verified()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "emailuser",
            email = "emailuser@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Give the fire-and-forget task a moment to complete
        await Task.Delay(100);

        factory.EmailService.SentEmails.Should().HaveCount(1);
        factory.EmailService.SentEmails[0].ToEmail.Should().Be("emailuser@example.com");
        factory.EmailService.SentEmails[0].Username.Should().Be("emailuser");
    }

    [Fact]
    public async Task Register_auto_verifies_email_when_config_flag_is_set()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Auth:AutoVerifyEmail"] = "true"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "autoverify",
            email = "autoverify@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.emailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Register_returns_503_when_mvp_login_disabled()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Auth:EnableMvpLogin"] = "false"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "blocked",
            email = "blocked@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("", "email@example.com", "password123", "USERNAME_REQUIRED")]
    [InlineData("validname", "", "password123", "EMAIL_REQUIRED")]
    [InlineData("validname", "email@example.com", "", "PASSWORD_REQUIRED")]
    public async Task Register_returns_bad_request_for_missing_required_fields(
        string username, string email, string password, string expectedCode)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new { username, email, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task Register_returns_bad_request_when_username_too_short()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "ab",
            email = "short@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("USERNAME_TOO_SHORT");
    }

    [Fact]
    public async Task Register_returns_bad_request_when_username_too_long()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = new string('a', 51),
            email = "long@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("USERNAME_TOO_LONG");
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user!name")]
    public async Task Register_returns_bad_request_when_username_has_invalid_characters(string username)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username,
            email = "invalid@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("USERNAME_INVALID");
    }

    [Fact]
    public async Task Register_returns_bad_request_when_password_too_short()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "validuser",
            email = "pass@example.com",
            password = "1234567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("PASSWORD_TOO_SHORT");
    }

    [Fact]
    public async Task Register_returns_conflict_when_username_already_taken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // The pre-seeded user "tester" already exists
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "tester",
            email = "unique@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("USERNAME_TAKEN");
    }

    [Fact]
    public async Task Register_returns_conflict_when_email_already_taken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // The pre-seeded user uses "tester@test.com"
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "uniqueuser",
            email = "tester@test.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.code.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task Register_normalizes_username_to_lowercase()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "MixedCaseUser",
            email = "mixedcase@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.username.Should().Be("mixedcaseuser");
    }

    [Fact]
    public async Task Register_returns_429_when_rate_limit_is_exceeded()
    {
        // Rate limiting enabled for this specific test
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "true"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Exhaust the 3-per-hour limit (the first calls may or may not succeed, depending on IP key)
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/v2/auth/register", new
            {
                username = $"ratelimituser{i}",
                email = $"ratelimit{i}@example.com",
                password = "12345678"
            });
        }

        // The 4th request should be rate limited
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", new
        {
            username = "blocked",
            email = "blocked@example.com",
            password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("X-RateLimit-Remaining").Should().BeTrue();
    }
}
