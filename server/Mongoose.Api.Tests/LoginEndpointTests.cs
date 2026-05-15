using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class LoginEndpointTests
{
    [Fact]
    public async Task Login_is_blocked_when_mvp_login_disabled()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:EnableMvpLogin"] = "false"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_rejects_invalid_password_and_sets_no_cookie()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_sets_secure_http_only_cookie_on_success()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:CookieName"] = "mongoose-auth-test"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Use the password that matches the BCrypt hash in FakeV2UsersRepository
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.Single(c => c.Contains("mongoose-auth-test"));
        cookie.Should().ContainEquivalentOf("httponly", "cookie must be httpOnly");
        cookie.Should().ContainEquivalentOf("secure", "cookie must require TLS");
        cookie.Should().ContainEquivalentOf("samesite=strict", "cookie should default to SameSite Strict");

        var expiresPart = cookie
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(p => p.Trim().StartsWith("expires=", StringComparison.OrdinalIgnoreCase));

        expiresPart.Should().NotBeNull("cookie must include Expires");
        var expiresValue = expiresPart!.Split('=', 2)[1].Trim();
        DateTimeOffset.TryParse(expiresValue, out var expiresUtc).Should().BeTrue("Expires must be parseable");

        var remaining = expiresUtc - DateTimeOffset.UtcNow;
        remaining.Should().BeGreaterThan(TimeSpan.FromDays(29));
        remaining.Should().BeLessThan(TimeSpan.FromDays(31));
    }

    [Fact]
    public async Task Login_sets_persistent_sliding_cookie_with_30_day_expiry()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new
        {
            username = "tester",
            password = "test-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticket = AuthCookieTestHelper.GetAuthenticationTicket(factory, response);
        ticket.Properties.IsPersistent.Should().BeTrue();
        ticket.Properties.AllowRefresh.Should().BeTrue("all sessions use sliding expiration");
        ticket.Properties.ExpiresUtc.Should().NotBeNull();

        var remaining = ticket.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow;
        remaining.Should().BeGreaterThan(TimeSpan.FromDays(29));
        remaining.Should().BeLessThan(TimeSpan.FromDays(31));
    }

    [Fact]
    public async Task Login_backfills_missing_security_stamp_for_legacy_user()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:CookieName"] = "mongoose-auth-test"
        });

        factory.UsersRepository.SetSecurityStamp("tester", string.Empty);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await factory.UsersRepository.GetByUsernameAsync("tester");
        updatedUser.Should().NotBeNull();
        updatedUser!.SecurityStamp.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_returns_INVALID_CREDENTIALS_code_for_wrong_password()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("INVALID_CREDENTIALS");
        json.RootElement.GetProperty("error").GetString().Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task Login_returns_INVALID_CREDENTIALS_code_for_nonexistent_user()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "nonexistent", password = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_returns_ACCOUNT_DEACTIVATED_code_for_inactive_user()
    {
        using var factory = new TestWebApplicationFactory();
        // Add an inactive user
        factory.UsersRepository.AddInactiveUser("inactive", "inactive@test.com", "test-password");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "inactive", password = "test-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("ACCOUNT_DEACTIVATED");
        json.RootElement.GetProperty("error").GetString().Should().Contain("deactivated");
    }

    [Fact]
    public async Task Protected_endpoint_returns_NOT_AUTHENTICATED_code_without_cookie()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Access protected endpoint without logging in
        var response = await client.GetAsync("/api/v2/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("NOT_AUTHENTICATED");
        json.RootElement.GetProperty("error").GetString().Should().Contain("Authentication required");
    }

    [Fact]
    public async Task Protected_endpoint_returns_SESSION_EXPIRED_code_with_invalid_cookie()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Manually set an invalid/expired auth cookie
        client.DefaultRequestHeaders.Add("Cookie", "mongoose-auth=invalid-expired-token");

        var response = await client.GetAsync("/api/v2/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("SESSION_EXPIRED");
        json.RootElement.GetProperty("error").GetString().Should().Contain("session has expired");
    }

    [Fact]
    public async Task Login_returns_400_with_COOKIE_CONSENT_REQUIRED_when_consent_is_rejected()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new
        {
            username = "tester",
            password = "test-password",
            consentLevel = "rejected"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse("no session cookie should be set when consent is rejected");

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("code").GetString().Should().Be("COOKIE_CONSENT_REQUIRED");
        json.RootElement.GetProperty("error").GetString().Should().Contain("Cookie consent is required");
    }

    [Fact]
    public async Task Login_succeeds_when_consent_level_is_omitted()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // consentLevel is intentionally absent — backward-compat path treats null as accepted
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new
        {
            username = "tester",
            password = "test-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeTrue("session cookie must be set on successful login");
    }

    // Note: FORBIDDEN (403) test is not included because the codebase doesn't currently
    // have role-based authorization policies. When such functionality is added,
    // a test should be created to verify that OnRedirectToAccessDenied returns
    // JSON with code "FORBIDDEN" and an appropriate error message.
}
