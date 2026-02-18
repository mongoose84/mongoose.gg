using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Auth;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Email;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Forgot Password Endpoint
/// Accepts an email and sends a password reset code if the account exists.
/// Always returns a 200 OK to prevent email enumeration.
/// Rate limited to 5 requests per hour per IP.
/// </summary>
public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public string Route { get; }

    private const int RateLimitRequests = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public ForgotPasswordEndpoint(string basePath)
    {
        Route = basePath + "/auth/forgot-password";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] ForgotPasswordRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] IEmailService emailService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<ForgotPasswordEndpoint> logger
        ) =>
        {
            try
            {
                var clientIp = GetClientIpAddress(httpContext);
                var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                    "forgot-password",
                    clientIp,
                    null,
                    RateLimitRequests,
                    RateLimitWindow);

                if (!rateLimitResult.IsAllowed)
                {
                    logger.LogWarning(
                        "Rate limit exceeded for forgot-password endpoint. IP: {IP}",
                        LogSanitizer.Sanitize(clientIp) ?? "unknown");

                    httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                    if (rateLimitResult.RetryAfter.HasValue)
                    {
                        httpContext.Response.Headers["Retry-After"] =
                            ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                    }

                    return Results.Json(
                        new { error = "Too many requests. Please try again later." },
                        statusCode: 429);
                }

                // Validate email input
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    // Still return 200 to prevent enumeration
                    return Results.Ok(new ForgotPasswordResponse(true, "If an account with that email exists, a reset code has been sent."));
                }

                // Normalize email
                var normalizedEmail = request.Email.ToLowerInvariant().Trim();

                // Look up user — do NOT reveal whether email exists
                var user = await usersRepo.GetByEmailAsync(normalizedEmail);

                if (user != null && user.IsActive)
                {
                    // Invalidate any existing password reset tokens
                    await tokensRepo.InvalidateActiveTokensAsync(user.UserId, TokenTypes.PasswordReset);

                    // Generate a new 6-digit reset code (15 min expiry)
                    var resetCode = VerificationCodeGenerator.Generate();
                    var expiresAt = DateTime.UtcNow.AddMinutes(15);
                    await tokensRepo.CreateTokenAsync(user.UserId, TokenTypes.PasswordReset, resetCode, expiresAt);

                    // Send the email — fire-and-forget logging on failure
                    try
                    {
                        await emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetCode);
                        logger.LogInformation("Password reset email sent for user {UserId}", user.UserId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.UserId);
                    }
                }
                else
                {
                    logger.LogDebug(
                        "Forgot-password request for unknown/inactive email: {Email}",
                        LogSanitizer.Sanitize(request.Email));
                }

                // Always return the same response to prevent email enumeration
                return Results.Ok(new ForgotPasswordResponse(true, "If an account with that email exists, a reset code has been sent."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ForgotPasswordEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}

