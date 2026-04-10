using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.LoginDto;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Login Endpoint
/// Validates username/password and sets an httpOnly session cookie for subsequent requests.
/// Supports rememberMe for 7-day sessions.
/// Rate limited to 10 requests per 15 minutes per IP to prevent brute force attacks.
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    public string Route { get; }

    // Rate limiting configuration: 10 login attempts per 15 minutes per IP
    private const int RateLimitRequests = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public LoginEndpoint(string basePath)
    {
        Route = basePath + "/auth/login";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] LoginRequest request,
            HttpContext httpContext,
            [FromServices] IUsersRepository usersRepo,
            [FromServices] LoginSyncService loginSyncService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<LoginEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                // Check rate limit before processing (IP-based only)
                // Can be disabled for E2E tests via RateLimiting:Enabled config flag
                var rateLimitingEnabled = config.GetValue<bool>("RateLimiting:Enabled", true);
                if (rateLimitingEnabled)
                {
                    var clientIp = ClientIpAddressResolver.GetClientIpAddress(httpContext);
                    var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                        "login",
                        clientIp,
                        null, // No user ID for login attempts
                        RateLimitRequests,
                        RateLimitWindow);

                    if (!rateLimitResult.IsAllowed)
                    {
                        logger.LogWarning(
                            "Rate limit exceeded for login endpoint. IP: {IP}",
                            LogSanitizer.Sanitize(clientIp) ?? "unknown");

                        httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                        if (rateLimitResult.RetryAfter.HasValue)
                        {
                            httpContext.Response.Headers["Retry-After"] =
                                ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                        }

                        return Results.Json(
                            new { error = "Too many login attempts. Please try again later." },
                            statusCode: 429);
                    }
                }

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
                    logger.LogWarning("Login attempt with non-existent username/email: {Input}", LogSanitizer.HashForLog(request.Username));
                    return AuthResults.InvalidCredentials();
                }

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    logger.LogWarning("Login attempt with invalid password for username: {Username}", LogSanitizer.HashForLog(user.Username));
                    return AuthResults.InvalidCredentials();
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    logger.LogWarning("Login attempt for inactive user: {Username}", LogSanitizer.HashForLog(user.Username));
                    return AuthResults.AccountDeactivated();
                }

                if (string.IsNullOrWhiteSpace(user.SecurityStamp))
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                }

                user.LastLoginAt = DateTime.UtcNow;
                await usersRepo.UpsertAsync(user);

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
                    new Claim("tier", user.Tier),
                    new Claim("security_stamp", user.SecurityStamp)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // rememberMe: persistent 30-day cookie, refreshed via sliding expiration on activity.
                // not rememberMe: session cookie (browser-close clears it), idle timeout from config.
                AuthenticationProperties authProperties;
                if (request.RememberMe)
                {
                    authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };
                }
                else
                {
                    var sessionTimeoutMinutes = config.GetValue<int>("Auth:SessionTimeout", 30);
                    authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(sessionTimeoutMinutes)
                    };
                }

                // Sign in user with cookie
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully", LogSanitizer.Sanitize(user.Username), LogSanitizer.Sanitize(user.UserId.ToString()));

                // Check linked Riot accounts for new matches and update profile data
                // Run in background (fire-and-forget) to avoid slowing down login response
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await loginSyncService.CheckAccountsOnLoginAsync(user.UserId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Background sync check failed for user {UserId}", LogSanitizer.Sanitize(user.UserId.ToString()));
                    }
                });

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
