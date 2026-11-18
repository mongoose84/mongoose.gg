using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
}