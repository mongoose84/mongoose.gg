using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// erify Endpoint
/// Verifies user email with a 6-digit code.
/// For MVP, accepts any valid 6-digit code.
/// </summary>
public sealed class VerifyEndpoint : IEndpoint
{
    public string Route { get; }
    private static readonly Regex CodeRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    public VerifyEndpoint(string basePath)
    {
        Route = basePath + "/auth/verify";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, [Authorize] async (
            [FromBody] VerifyRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] ILogger<VerifyEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Validate code format
                if (string.IsNullOrWhiteSpace(request.Code) || !CodeRegex.IsMatch(request.Code))
                {
                    return Results.BadRequest(new { error = "Invalid verification code format", code = "INVALID_CODE_FORMAT" });
                }

                // Get user from database
                var user = await usersRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    logger.LogWarning("Verify attempt for non-existent user ID: {UserId}", userId);
                    return Results.Unauthorized();
                }

                // Check if already verified
                if (user.EmailVerified)
                {
                    return Results.Ok(new VerifyResponse(true, "Email already verified"));
                }

                // Get active verification token
                var token = await tokensRepo.GetActiveTokenAsync(userId, TokenTypes.EmailVerification);
                if (token == null)
                {
                    logger.LogWarning("User {UserId} has no active verification token", userId);
                    return Results.BadRequest(new { error = "No verification code found. Please request a new code.", code = "NO_CODE_STORED" });
                }

                // Check if max attempts exceeded (brute-force protection)
                var maxAttempts = config.GetValue<int>("Auth:VerificationMaxAttempts", 5);
                if (token.Attempts >= maxAttempts)
                {
                    // Invalidate the token to prevent further attempts
                    await tokensRepo.MarkTokenAsUsedAsync(token.Id);
                    logger.LogWarning("User {UserId} exceeded max verification attempts ({MaxAttempts}). Token invalidated.", userId, maxAttempts);
                    return Results.BadRequest(new {
                        error = "Too many failed attempts. Please request a new verification code.",
                        code = "MAX_ATTEMPTS_EXCEEDED"
                    });
                }

                // Validate the code matches
                if (!string.Equals(request.Code, token.Code, StringComparison.Ordinal))
                {
                    // Increment attempt counter
                    await tokensRepo.IncrementAttemptsAsync(token.Id);
                    logger.LogWarning("User {UserId} submitted incorrect verification code (attempt {Attempts}/{MaxAttempts})", userId, token.Attempts + 1, maxAttempts);
                    return Results.BadRequest(new { error = "Invalid verification code. Please try again.", code = "INVALID_CODE" });
                }

                logger.LogDebug("User {UserId} submitted correct verification code", userId);

                // Update user as verified first, then mark token as used
                // Order matters: if user update fails, token remains valid for retry
                // If token marking fails, user is verified (acceptable - token can't be reused anyway)
                await usersRepo.UpdateEmailVerifiedAsync(userId, true);
                await tokensRepo.MarkTokenAsUsedAsync(token.Id);

                // Update the session claims to reflect verified status
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("email_verified", "true"),
                    new Claim("tier", user.Tier)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var sessionTimeoutMinutes = config.GetValue<int>("Auth:SessionTimeout", 30);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(sessionTimeoutMinutes)
                };

                // Re-sign in with updated claims
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) email verified successfully", user.Username, userId);

                return Results.Ok(new VerifyResponse(true, "Email verified successfully"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in VerifyEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    public record VerifyRequest(
        [property: JsonPropertyName("code")] string Code
    );

    public record VerifyResponse(
        [property: JsonPropertyName("verified")] bool Verified,
        [property: JsonPropertyName("message")] string Message
    );
}

