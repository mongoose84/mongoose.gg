using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.LoginDto;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Login Endpoint
/// Validates username/password and sets an httpOnly session cookie for subsequent requests.
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    public string Route { get; }

    public LoginEndpoint(string basePath)
    {
        Route = basePath + "/login";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] LoginRequest request,
            HttpContext httpContext,
            [FromServices] UserRepository userRepo,
            [FromServices] ILogger<LoginEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                // Feature flag gate: disable MVP login unless explicitly enabled
                var enableMvpLogin = config.GetValue<bool>("Auth:EnableMvpLogin");
                if (!enableMvpLogin)
                {
                    logger.LogWarning("Login attempt blocked: MVP login disabled by configuration");
                    return Results.StatusCode(503); // Service Unavailable while auth is not ready
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Username and password are required" });

                // Fetch user by username
                var user = await userRepo.GetByUserNameAsync(request.Username);
                if (user == null)
                {
                    logger.LogWarning("Login attempt with non-existent username: {Username}", request.Username);
                    return Results.Unauthorized();
                }

                // Temporary dev-only password check: require configured DevPassword secret
                var devPassword = config.GetValue<string>("Auth:DevPassword");
                if (string.IsNullOrEmpty(devPassword))
                {
                    logger.LogWarning("Login blocked: DevPassword not configured");
                    return Results.StatusCode(503);
                }

                if (!ValidatePassword(request.Password, devPassword))
                {
                    logger.LogWarning("Login attempt with invalid password for username: {Username}", request.Username);
                    return Results.Unauthorized();
                }

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var sessionTimeoutMinutes = config.GetValue<int>("Auth:SessionTimeout", 30);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(sessionTimeoutMinutes)
                };

                // Sign in user with cookie
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully", user.UserName, user.UserId);

                return Results.Ok(new LoginResponse(
                    user.UserId,
                    user.UserName,
                    "Login successful"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoginEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    /// <summary>
    /// Validates password using a configured dev secret. Replace with bcrypt/argon2 against stored hash.
    /// </summary>
    private static bool ValidatePassword(string password, string configuredSecret)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(configuredSecret))
            return false;
        // Constant-time comparison to mitigate timing attacks
        return ConstantTimeEquals(password, configuredSecret);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;
        if (a.Length != b.Length)
            return false;
        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
