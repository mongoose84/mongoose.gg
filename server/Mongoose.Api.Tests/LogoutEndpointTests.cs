using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class LogoutEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await loginClient.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        response.EnsureSuccessStatusCode();
        return AuthCookieTestHelper.GetAuthCookie(response);
    }

    [Fact]
    public async Task Logout_returns_ok_when_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Logout_returns_ok_when_unauthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/api/v2/auth/logout", null);

        // Logout is permissive — clearing a non-existent session is not an error
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_clears_auth_cookie()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // After logout the Set-Cookie header should expire the auth cookie
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var authSetCookie = cookies!.FirstOrDefault(c => c.Contains("mongoose-auth"));
        authSetCookie.Should().NotBeNull("logout must clear the auth cookie");

        // A cleared cookie is set with a past expiry date
        var expiresPart = authSetCookie!
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(p => p.Trim().StartsWith("expires=", StringComparison.OrdinalIgnoreCase));

        expiresPart.Should().NotBeNull("cleared cookie must carry an Expires attribute");
        var expiresValue = expiresPart!.Split('=', 2)[1].Trim();
        DateTimeOffset.TryParse(expiresValue, out var expiresUtc).Should().BeTrue();
        expiresUtc.Should().BeBefore(DateTimeOffset.UtcNow, "cleared cookie must have a past expiry");
    }

    [Fact]
    public async Task Logout_returns_success_message_in_response_body()
    {
        using var factory = new TestWebApplicationFactory();
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        req.Headers.Add("Cookie", authCookie);

        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString()!.ToLowerInvariant().Should().Contain("logged out");
    }
}
