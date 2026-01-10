using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Users/Me Endpoint
/// Returns the current authenticated user's information.
/// </summary>
public sealed class UsersMeEndpoint : IEndpoint
{
    public string Route { get; }

    public UsersMeEndpoint(string basePath)
    {
        Route = basePath + "/users/me";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, [Authorize] async (
            HttpContext httpContext,
            [FromServices] V2UsersRepository usersRepo,
            [FromServices] ILogger<UsersMeEndpoint> logger
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
                    logger.LogWarning("User not found for ID: {UserId}", userId);
                    return Results.Unauthorized();
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    logger.LogWarning("Inactive user attempted to access /users/me: {UserId}", userId);
                    return Results.Unauthorized();
                }

                return Results.Ok(new UserMeResponse(
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.EmailVerified,
                    user.Tier,
                    user.CreatedAt
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UsersMeEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    public record UserMeResponse(
        [property: JsonPropertyName("userId")] long UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("emailVerified")] bool EmailVerified,
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt
    );
}

