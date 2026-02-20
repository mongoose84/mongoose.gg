using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Auth;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Reset Password Endpoint
/// Validates a password reset code and updates the user's password.
/// Rate limited to 10 requests per 15 minutes per IP.
/// </summary>
public sealed class ResetPasswordEndpoint : IEndpoint
{
    public string Route { get; }

    private const int RateLimitRequests = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private static readonly Regex CodeRegex = new(@"^\d{6}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public ResetPasswordEndpoint(string basePath)
    {
        Route = basePath + "/auth/reset-password";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] ResetPasswordRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<ResetPasswordEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                var clientIp = GetClientIpAddress(httpContext);
                var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                    "reset-password",
                    clientIp,
                    null,
                    RateLimitRequests,
                    RateLimitWindow);

                if (!rateLimitResult.IsAllowed)
                {
                    logger.LogWarning(
                        "Rate limit exceeded for reset-password endpoint. IP: {IP}",
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

                // Validate email
                if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email.Trim()))
                {
                    return Results.BadRequest(new { error = "Please enter a valid email address.", code = "INVALID_EMAIL" });
                }

                // Validate new password length
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
                {
                    return Results.BadRequest(new { error = "Password must be at least 8 characters.", code = "PASSWORD_TOO_SHORT" });
                }

                // Validate code format
                if (string.IsNullOrWhiteSpace(request.Code) || !CodeRegex.IsMatch(request.Code))
                {
                    return Results.BadRequest(new { error = "Invalid or expired code. Please request a new one.", code = "INVALID_CODE" });
                }

                // Look up user by email
                var normalizedEmail = request.Email.ToLowerInvariant().Trim();
                var user = await usersRepo.GetByEmailAsync(normalizedEmail);
                if (user == null || !user.IsActive)
                {
                    // Return same error as invalid code to prevent enumeration
                    return Results.BadRequest(new { error = "Invalid or expired code. Please request a new one.", code = "INVALID_CODE" });
                }

                // Get the active password reset token
                var token = await tokensRepo.GetActiveTokenAsync(user.UserId, TokenTypes.PasswordReset);
                if (token == null)
                {
                    return Results.BadRequest(new { error = "Invalid or expired code. Please request a new one.", code = "INVALID_CODE" });
                }

                // Brute-force protection: check max attempts
                var maxAttempts = config.GetValue<int>("Auth:VerificationMaxAttempts", 5);
                if (token.Attempts >= maxAttempts)
                {
                    await tokensRepo.MarkTokenAsUsedAsync(token.Id);
                    logger.LogWarning("User {UserId} exceeded max password reset attempts. Token invalidated.", user.UserId);
                    return Results.BadRequest(new { error = "Invalid or expired code. Please request a new one.", code = "INVALID_CODE" });
                }

                // Validate code match
                if (!string.Equals(request.Code, token.Code, StringComparison.Ordinal))
                {
                    await tokensRepo.IncrementAttemptsAsync(token.Id);
                    logger.LogWarning("User {UserId} submitted incorrect reset code (attempt {Attempts}/{MaxAttempts})", user.UserId, token.Attempts + 1, maxAttempts);
                    return Results.BadRequest(new { error = "Invalid or expired code. Please request a new one.", code = "INVALID_CODE" });
                }

                // Hash new password and update
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await usersRepo.UpdatePasswordHashAsync(user.UserId, newPasswordHash);
                await tokensRepo.MarkTokenAsUsedAsync(token.Id);

                logger.LogInformation("Password reset successfully for user {UserId}", user.UserId);

                return Results.Ok(new ResetPasswordResponse(true, "Password has been reset. Please log in with your new password."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ResetPasswordEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}

