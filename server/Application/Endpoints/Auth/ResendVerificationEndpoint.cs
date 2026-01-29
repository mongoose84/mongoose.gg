using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;
using RiotProxy.Infrastructure.Email;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// Resend Verification Endpoint
/// Generates a new verification code and sends it to the user's email.
/// </summary>
public sealed class ResendVerificationEndpoint : IEndpoint
{
    public string Route { get; }

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
            [FromServices] ILogger<ResendVerificationEndpoint> logger
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

                // Get user from database
                var user = await usersRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    logger.LogWarning("Resend verification attempt for non-existent user ID: {UserId}", userId);
                    return Results.Unauthorized();
                }

                // Check if already verified
                if (user.EmailVerified)
                {
                    return Results.BadRequest(new { error = "Email is already verified", code = "ALREADY_VERIFIED" });
                }

                // Rate limiting: check if a token was created in the last 60 seconds
                var recentTokenCount = await tokensRepo.CountRecentTokensAsync(userId, TokenTypes.EmailVerification, 60);
                if (recentTokenCount > 0)
                {
                    // Get the most recent token to calculate wait time
                    var existingToken = await tokensRepo.GetActiveTokenAsync(userId, TokenTypes.EmailVerification);
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
                await tokensRepo.InvalidateActiveTokensAsync(userId, TokenTypes.EmailVerification);

                // Generate new verification code and create token
                var verificationCode = VerificationCodeGenerator.Generate();
                var verificationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                await tokensRepo.CreateTokenAsync(userId, TokenTypes.EmailVerification, verificationCode, verificationExpiresAt);

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

