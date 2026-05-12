using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Application.Endpoints.Auth;

internal static class AuthSessionFactory
{
    public static ClaimsPrincipal CreatePrincipal(User user, bool? emailVerifiedOverride = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("email_verified", (emailVerifiedOverride ?? user.EmailVerified).ToString().ToLowerInvariant()),
            new("tier", user.Tier),
            new("security_stamp", user.SecurityStamp)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(claimsIdentity);
    }

    public static AuthenticationProperties CreatePersistentSlidingSession()
    {
        return new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };
    }
}
