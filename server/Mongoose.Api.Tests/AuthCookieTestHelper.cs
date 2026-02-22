using System.Net.Http;
using FluentAssertions;

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
}