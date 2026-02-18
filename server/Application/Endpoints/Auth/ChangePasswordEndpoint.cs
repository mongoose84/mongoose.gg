using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Auth;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Change Password Endpoint
/// Allows an authenticated user to change their password.
/// On success, the session is invalidated and the user must re-login.
/// </summary>
public sealed class ChangePasswordEndpoint : IEndpoint
{
    public string Route { get; }

    public ChangePasswordEndpoint(string basePath)
    {
        Route = basePath + "/auth/change-password";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, [Authorize] async (
            [FromBody] ChangePasswordRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] ILogger<ChangePasswordEndpoint> logger
        ) =>
        {
            try
            {
                // Extract user ID from session claims
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return AuthResults.InvalidSession();
                }

                // Validate new password length
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
                {
                    return Results.BadRequest(new { error = "New password must be at least 8 characters.", code = "PASSWORD_TOO_SHORT" });
                }

                // Validate inputs are present
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return Results.BadRequest(new { error = "Current password is required.", code = "INVALID_PASSWORD" });
                }

                // Fetch user
                var user = await usersRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    logger.LogWarning("Change-password attempt for non-existent user ID: {UserId}", userId);
                    return AuthResults.InvalidSession();
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    logger.LogWarning("Change-password: incorrect current password for user {UserId}", userId);
                    return Results.Json(
                        new { error = "Current password is incorrect.", code = "INVALID_PASSWORD" },
                        statusCode: 401);
                }

                // Check new password is different from current
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
                {
                    return Results.BadRequest(new { error = "New password must be different from your current password.", code = "SAME_PASSWORD" });
                }

                // Hash and store the new password
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await usersRepo.UpdatePasswordHashAsync(userId, newPasswordHash);

                // Sign out the user (invalidate session)
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Results.Ok(new ChangePasswordResponse(true, "Password changed. Please log in again."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ChangePasswordEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}

