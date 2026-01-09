using System;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RiotProxy.Tests;

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

        var response = await client.PostAsJsonAsync("/api/v2/login", new { username = "tester", password = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_is_blocked_when_dev_password_missing()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:DevPassword"] = string.Empty,
            ["Auth:EnableMvpLogin"] = "true"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/login", new { username = "tester", password = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_rejects_invalid_password_and_sets_no_cookie()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:DevPassword"] = "dev-secret"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/login", new { username = "tester", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_sets_secure_http_only_cookie_on_success()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Auth:DevPassword"] = "dev-secret",
            ["Auth:CookieName"] = "pulse-auth-test",
            ["Auth:SessionTimeout"] = "45"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/login", new { username = "tester", password = "dev-secret" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.Single(c => c.Contains("pulse-auth-test"));
        cookie.Should().ContainEquivalentOf("httponly", "cookie must be httpOnly");
        cookie.Should().ContainEquivalentOf("secure", "cookie must require TLS");
        cookie.Should().ContainEquivalentOf("samesite=lax", "cookie should default to SameSite Lax");

        var expiresPart = cookie
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(p => p.Trim().StartsWith("expires=", StringComparison.OrdinalIgnoreCase));

        expiresPart.Should().NotBeNull("cookie must include Expires");
        var expiresValue = expiresPart!.Split('=', 2)[1].Trim();
        DateTimeOffset.TryParse(expiresValue, out var expiresUtc).Should().BeTrue("Expires must be parseable");

        var remaining = expiresUtc - DateTimeOffset.UtcNow;
        remaining.Should().BeGreaterThan(TimeSpan.FromMinutes(40));
        remaining.Should().BeLessThan(TimeSpan.FromMinutes(50));
    }
}
