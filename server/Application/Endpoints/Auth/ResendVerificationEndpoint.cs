using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.Endpoints.Shared;
using RiotProxy.Core.Entities;
using RiotProxy.Core.Interfaces;
using RiotProxy.Infrastructure.Database.Repositories;
using RiotProxy.Infrastructure.Email;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// Resend Verification Endpoint
/// Generates a new verification code and sends it to the user's email.
/// Rate limited to 5 requests per hour per user to prevent email spam.
/// </summary>
public sealed class ResendVerificationEndpoint : IEndpoint
{
    public string Route { get; }

    // Rate limiting configuration: 5 resend attempts per hour per user
    private const int RateLimitRequests = 5;
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

    public ResendVerificationEndpoint(string basePath)
    {
        Route = basePath + "/auth/resend-verification";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, [Authorize] async (
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] IEmailService emailService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<ResendVerificationEndpoint> logger
        ) =>
        {
            try
            {
                // Get current user ID from claims for rate limiting
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                long? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Check rate limit before processing (user-based for authenticated users, IP fallback)
                var clientIp = GetClientIpAddress(httpContext);
                var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                    "resend-verification",
                    clientIp,
                    userId,
                    RateLimitRequests,
                    RateLimitWindow);

                if (!rateLimitResult.IsAllowed)
                {
                    logger.LogWarning(
                        "Rate limit exceeded for resend-verification endpoint. IP: {IP}, UserId: {UserId}",
                        clientIp ?? "unknown",
                        userId?.ToString() ?? "anonymous");

                    httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                    if (rateLimitResult.RetryAfter.HasValue)
                    {
                        httpContext.Response.Headers["Retry-After"] =
                            ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                    }

                    return Results.Json(
                        new { error = "Too many verification code requests. Please try again later." },
                        statusCode: 429);
                }

                // Validate user ID from claims (use the already parsed userId)
                if (!userId.HasValue)
                {
                    return AuthResults.InvalidSession();
                }

                // Get user from database
                var user = await usersRepo.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    logger.LogWarning("Resend verification attempt for non-existent user ID: {UserId}", userId);
                    return AuthResults.InvalidSession();
                }

                // Check if already verified
                if (user.EmailVerified)
                {
                    return Results.BadRequest(new { error = "Email is already verified", code = "ALREADY_VERIFIED" });
                }

                // Rate limiting: check if a token was created in the last 60 seconds
                var recentTokenCount = await tokensRepo.CountRecentTokensAsync(userId.Value, TokenTypes.EmailVerification, 60);
                if (recentTokenCount > 0)
                {
                    // Get the most recent token to calculate wait time
                    var existingToken = await tokensRepo.GetActiveTokenAsync(userId.Value, TokenTypes.EmailVerification);
                    if (existingToken != null)
                    {
                        var timeSinceCreated = DateTime.UtcNow - existingToken.CreatedAt;
                        var waitSeconds = Math.Max(1, (int)(60 - timeSinceCreated.TotalSeconds));
                        return Results.BadRequest(new {
                            error = $"Please wait {waitSeconds} seconds before requesting a new code",
                            code = "RATE_LIMITED",
                            waitSeconds
                        });
                    }
                }

                // Invalidate any existing active tokens
                await tokensRepo.InvalidateActiveTokensAsync(userId.Value, TokenTypes.EmailVerification);

                // Generate new verification code and create token
                var verificationCode = VerificationCodeGenerator.Generate();
                var verificationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                await tokensRepo.CreateTokenAsync(userId.Value, TokenTypes.EmailVerification, verificationCode, verificationExpiresAt);

                // Send verification email
                try
                {
                    await emailService.SendVerificationEmailAsync(user.Email, user.Username, verificationCode);
                    logger.LogInformation("Resent verification email to user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
                    return Results.Json(new { error = "Failed to send verification email. Please try again later." }, statusCode: 500);
                }

                return Results.Ok(new ResendVerificationResponse(true, "Verification code sent successfully"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ResendVerificationEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    public record ResendVerificationResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string Message
    );
}

