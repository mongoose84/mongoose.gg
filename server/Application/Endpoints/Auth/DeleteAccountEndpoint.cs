using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Infrastructure.Database.Repositories;
using static Mongoose.Api.Application.DTOs.DeleteAccountDto;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Delete Account Endpoint
/// Permanently deletes the user's account and all associated data.
/// Requires password confirmation for security.
/// </summary>
public sealed class DeleteAccountEndpoint : IEndpoint
{
    public string Route { get; }

    public DeleteAccountEndpoint(string basePath)
    {
        Route = basePath + "/auth/account";
    }

    public void Configure(WebApplication app)
    {
        app.MapDelete(Route, [Authorize] async (
            [FromBody] DeleteAccountRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] ILogger<DeleteAccountEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and extract user ID
                var (authError, authenticatedUser) = AuthorizationHelper.GetAuthenticatedUser(httpContext, logger);
                if (authError != null)
                    return authError;

                // Validate password is provided
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest(new { error = "Password is required", code = "INVALID_PASSWORD" });
                }

                // Get user from database
                var user = await usersRepo.GetByIdAsync(authenticatedUser!.UserId);
                if (user == null)
                {
                    logger.LogWarning("Delete account requested for non-existent user ID: {UserId}", authenticatedUser.UserId);
                    return AuthResults.InvalidSession();
                }

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    logger.LogWarning("Delete account attempt with invalid password for user: {UserId}", authenticatedUser.UserId);
                    return Results.Json(new { error = "Invalid password", code = "INVALID_PASSWORD" }, statusCode: 401);
                }

                // Perform the deletion
                var deleted = await usersRepo.DeleteUserAsync(authenticatedUser.UserId);
                
                if (!deleted)
                {
                    logger.LogError("Failed to delete user {UserId} - user not found during deletion", authenticatedUser.UserId);
                    return Results.Json(new { error = "Account deletion failed" }, statusCode: 500);
                }

                logger.LogInformation("User {UserId} ({Username}) account deleted successfully", authenticatedUser.UserId, user.Username);

                // Sign out the user (clear the auth cookie)
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.Ok(new DeleteAccountResponse(
                    true,
                    "Your account has been permanently deleted"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DeleteAccountEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}

