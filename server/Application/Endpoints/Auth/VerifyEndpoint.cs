using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Verify Endpoint
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

                // For MVP, accept any valid 6-digit code
                // TODO: Implement actual email verification with stored codes
                logger.LogDebug("User {UserId} submitted verification code", userId);

                // Update user as verified
                await usersRepo.UpdateEmailVerifiedAsync(userId, true);

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

