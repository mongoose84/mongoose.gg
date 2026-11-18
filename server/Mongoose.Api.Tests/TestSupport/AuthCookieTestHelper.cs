using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mongoose.Api.Tests;

internal static class AuthCookieTestHelper
{
    private const string SetCookieHeader = "Set-Cookie";
    private const string DefaultCookieName = "mongoose-auth";

    public static string GetAuthCookie(HttpResponseMessage response, string cookieName = DefaultCookieName)
    {
        response.Headers.TryGetValues(SetCookieHeader, out var cookies).Should().BeTrue();

        var authCookie = cookies!.FirstOrDefault(c => c.Contains($"{cookieName}=", StringComparison.Ordinal));
        authCookie.Should().NotBeNullOrEmpty($"Expected auth cookie '{cookieName}' in Set-Cookie headers.");

        return authCookie!.Split(';', 2)[0];
    }

    public static AuthenticationTicket GetAuthenticationTicket(
        TestWebApplicationFactory factory,
        HttpResponseMessage response,
        string cookieName = DefaultCookieName)
    {
        var cookiePair = GetAuthCookie(response, cookieName);
        var cookieValue = Uri.UnescapeDataString(cookiePair.Split('=', 2)[1]);

        var optionsMonitor = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var options = optionsMonitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookieValue);

        ticket.Should().NotBeNull("auth cookie should contain a valid authentication ticket");
        return ticket!;
    }

    public const string DefaultUsername = "tester";
    public const string DefaultPassword = "test-password";

    /// <summary>
    /// Logs in through the real login endpoint and returns the auth cookie pair (<c>name=value</c>).
    /// Defaults to the verified user seeded by <see cref="TestWebApplicationFactory"/>.
    /// </summary>
    public static async Task<string> LoginAsync(
        TestWebApplicationFactory factory,
        string username = DefaultUsername,
        string password = DefaultPassword)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return GetAuthCookie(response);
    }

    /// <summary>
    /// Seeds an unverified user, logs in as them, and returns the auth cookie with their user id.
    /// </summary>
    public static async Task<(string cookie, long userId)> LoginUnverifiedUserAsync(TestWebApplicationFactory factory)
    {
        factory.UsersRepository.AddUnverifiedUser("unverified", "unverified@test.com", DefaultPassword);
        var cookie = await LoginAsync(factory, "unverified");
        var user = await factory.UsersRepository.GetByUsernameAsync("unverified");
        return (cookie, user!.UserId);
    }
}
