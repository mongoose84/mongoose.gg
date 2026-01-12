using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using static RiotProxy.Application.DTOs.LoginDto;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Login Endpoint
/// Validates username/password and sets an httpOnly session cookie for subsequent requests.
/// Supports rememberMe for 7-day sessions.
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    public string Route { get; }

    public LoginEndpoint(string basePath)
    {
        Route = basePath + "/auth/login";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] LoginRequest request,
            HttpContext httpContext,
            [FromServices] V2UsersRepository usersRepo,
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
                    return Results.Json(new { error = "Login is currently disabled" }, statusCode: 503);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Username and password are required" });

                // Normalize input to lowercase to match storage format
                var normalizedInput = request.Username.ToLowerInvariant().Trim();

                // Fetch user by username first, then try email
                var user = await usersRepo.GetByUsernameAsync(normalizedInput);
                if (user == null)
                {
                    // Try email as fallback (user might be logging in with email)
                    user = await usersRepo.GetByEmailAsync(normalizedInput);
                }

                if (user == null)
                {
                    logger.LogWarning("Login attempt with non-existent username/email: {Input}", request.Username);
                    return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
                }

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    logger.LogWarning("Login attempt with invalid password for username: {Username}", user.Username);
                    return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    logger.LogWarning("Login attempt for inactive user: {Username}", user.Username);
                    return Results.Json(new { error = "This account has been deactivated" }, statusCode: 401);
                }

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
                    new Claim("tier", user.Tier)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Use 30 days for rememberMe, otherwise use configured session timeout
                var sessionTimeoutMinutes = request.RememberMe
                    ? 60 * 24 * 30 // 30 days in minutes
                    : config.GetValue<int>("Auth:SessionTimeout", 30);

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

                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                await usersRepo.UpsertAsync(user);

                logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully", user.Username, user.UserId);

                return Results.Ok(new LoginResponse(
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.EmailVerified,
                    user.Tier,
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
}
