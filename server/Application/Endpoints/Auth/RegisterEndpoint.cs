using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Email;
using static Mongoose.Api.Application.DTOs.RegisterDto;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Register Endpoint
/// Creates a new user account with username, email, and password.
/// Sets emailVerified=false and logs the user in with a session cookie.
/// Rate limited to 3 requests per hour per IP to prevent account creation spam.
/// </summary>
public sealed class RegisterEndpoint : IEndpoint
{
    public string Route { get; }
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    // Rate limiting configuration: 3 registrations per hour per IP
    private const int RateLimitRequests = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    /// <summary>
    /// Extracts the client IP address from the HTTP context.
    /// Checks X-Forwarded-For header first (for proxies/load balancers),
    /// then falls back to the direct connection IP.
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public RegisterEndpoint(string basePath)
    {
        Route = basePath + "/auth/register";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] RegisterRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] IEmailService emailService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<RegisterEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                // Check rate limit before processing (IP-based only, no user yet)
                // Can be disabled for E2E tests via RateLimiting:Enabled config flag
                var rateLimitingEnabled = config.GetValue<bool>("RateLimiting:Enabled", true);
                if (rateLimitingEnabled)
                {
                    var clientIp = GetClientIpAddress(httpContext);
                    var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                        "register",
                        clientIp,
                        null, // No user ID for registration
                        RateLimitRequests,
                        RateLimitWindow);

                    if (!rateLimitResult.IsAllowed)
                    {
                        logger.LogWarning(
                            "Rate limit exceeded for register endpoint. IP: {IP}",
                            LogSanitizer.Sanitize(clientIp) ?? "unknown");

                        httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                        if (rateLimitResult.RetryAfter.HasValue)
                        {
                            httpContext.Response.Headers["Retry-After"] =
                                ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                        }

                        return Results.Json(
                            new { error = "Too many registration attempts. Please try again later." },
                            statusCode: 429);
                    }
                }

                // Feature flag gate
                var enableMvpLogin = config.GetValue<bool>("Auth:EnableMvpLogin");
                if (!enableMvpLogin)
                {
                    logger.LogWarning("Registration attempt blocked: MVP login disabled by configuration");
                    return Results.Json(new { error = "Registration is currently disabled" }, statusCode: 503);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username))
                    return Results.BadRequest(new { error = "Username is required", code = "USERNAME_REQUIRED" });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Results.BadRequest(new { error = "Email is required", code = "EMAIL_REQUIRED" });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Password is required", code = "PASSWORD_REQUIRED" });

                // Validate username length (3-50 chars)
                if (request.Username.Length < 3)
                    return Results.BadRequest(new { error = "Username must be at least 3 characters", code = "USERNAME_TOO_SHORT" });

                if (request.Username.Length > 50)
                    return Results.BadRequest(new { error = "Username must be 50 characters or less", code = "USERNAME_TOO_LONG" });

                // Validate username format (alphanumeric, underscore, hyphen only)
                if (!UsernameRegex.IsMatch(request.Username))
                    return Results.BadRequest(new { error = "Username can only contain letters, numbers, underscores, and hyphens", code = "USERNAME_INVALID" });

                // Normalize username to lowercase to prevent case-variant duplicates
                var normalizedUsername = request.Username.ToLowerInvariant().Trim();

                // Validate password length
                if (request.Password.Length < 8)
                    return Results.BadRequest(new { error = "Password must be at least 8 characters", code = "PASSWORD_TOO_SHORT" });

                // Check if username already exists (case-insensitive)
                if (await usersRepo.UsernameExistsAsync(normalizedUsername))
                {
                    logger.LogWarning("Registration attempt with existing username: {Username}", LogSanitizer.Sanitize(request.Username));
                    return Results.Conflict(new { error = "This username is already taken", code = "USERNAME_TAKEN" });
                }

                // Check if email already exists
                if (await usersRepo.EmailExistsAsync(request.Email))
                {
                    logger.LogWarning("Registration attempt with existing email: {Email}", LogSanitizer.Sanitize(request.Email));
                    return Results.Conflict(new { error = "This email is already registered", code = "EMAIL_TAKEN" });
                }

                // Hash password (using BCrypt)
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Auto-verify email if configured (for E2E tests in CI)
                // This should only be enabled in controlled test environments, never in real production
                var autoVerifyEmail = config.GetValue<bool>("Auth:AutoVerifyEmail", false);

                // Create user with normalized username and email
                var newUser = new User
                {
                    Email = request.Email.ToLowerInvariant().Trim(),
                    Username = normalizedUsername,
                    PasswordHash = passwordHash,
                    EmailVerified = autoVerifyEmail,
                    IsActive = true,
                    Tier = "free",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var userId = await usersRepo.UpsertAsync(newUser);
                newUser.UserId = userId;

                // Only handle email verification in production
                if (!autoVerifyEmail)
                {
                    // Invalidate any existing verification tokens for this user
                    // This handles edge cases like race conditions or if UpsertAsync updated an existing user
                    await tokensRepo.InvalidateActiveTokensAsync(userId, TokenTypes.EmailVerification);

                    // Generate verification code and create token
                    var verificationCode = VerificationCodeGenerator.Generate();
                    var verificationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    await tokensRepo.CreateTokenAsync(userId, TokenTypes.EmailVerification, verificationCode, verificationExpiresAt);

                    // Send verification email (fire-and-forget to not block registration)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await emailService.SendVerificationEmailAsync(newUser.Email, newUser.Username, verificationCode);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to send verification email for user {UserId}", userId);
                        }
                    });
                }
                else
                {
                    logger.LogInformation("Auto-verified email for user {UserId} (Auth:AutoVerifyEmail enabled)", userId);
                }

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, newUser.Username),
                    new Claim(ClaimTypes.Email, newUser.Email),
                    new Claim("email_verified", autoVerifyEmail ? "true" : "false"),
                    new Claim("tier", newUser.Tier)
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

                logger.LogInformation("User {Username} (ID: {UserId}) registered successfully", LogSanitizer.Sanitize(newUser.Username), userId);

                return Results.Ok(new RegisterResponse(
                    userId,
                    newUser.Username,
                    newUser.Email,
                    autoVerifyEmail,
                    autoVerifyEmail
                        ? "Registration successful. Email verified automatically."
                        : "Registration successful. Please verify your email."
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RegisterEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}
