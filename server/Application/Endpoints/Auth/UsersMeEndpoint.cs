using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// Users/Me Endpoint
/// Returns the current authenticated user's information including linked Riot accounts.
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
            [FromServices] UsersRepository usersRepo,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
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

                // Get linked Riot accounts
                var riotAccounts = await riotAccountsRepo.GetByUserIdAsync(userId);
                var riotAccountResponses = riotAccounts.Select(ra => new RiotAccountResponse(
                    ra.Puuid,
                    ra.GameName,
                    ra.TagLine,
                    ra.SummonerName,
                    ra.Region,
                    ra.IsPrimary,
                    ra.SyncStatus,
                    ra.SyncProgress,
                    ra.SyncTotal,
                    ra.ProfileIconId,
                    ra.SummonerLevel,
                    ra.SoloTier,
                    ra.SoloRank,
                    ra.SoloLp,
                    ra.FlexTier,
                    ra.FlexRank,
                    ra.FlexLp,
                    ra.LastSyncAt,
                    ra.CreatedAt
                )).ToList();

                return Results.Ok(new UserMeResponse(
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.EmailVerified,
                    user.Tier,
                    user.CreatedAt,
                    riotAccountResponses
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
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
        [property: JsonPropertyName("riotAccounts")] List<RiotAccountResponse> RiotAccounts
    );

    /// <summary>
    /// Riot account response. Includes summonerName as a convenience field
    /// containing the pre-formatted display name (gameName#tagLine).
    /// </summary>
    public record RiotAccountResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine,
        [property: JsonPropertyName("summonerName")] string SummonerName,
        [property: JsonPropertyName("region")] string Region,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary,
        [property: JsonPropertyName("syncStatus")] string SyncStatus,
        [property: JsonPropertyName("syncProgress")] int SyncProgress,
        [property: JsonPropertyName("syncTotal")] int SyncTotal,
        [property: JsonPropertyName("profileIconId")] int? ProfileIconId,
        [property: JsonPropertyName("summonerLevel")] int? SummonerLevel,
        [property: JsonPropertyName("soloTier")] string? SoloTier,
        [property: JsonPropertyName("soloRank")] string? SoloRank,
        [property: JsonPropertyName("soloLp")] int? SoloLp,
        [property: JsonPropertyName("flexTier")] string? FlexTier,
        [property: JsonPropertyName("flexRank")] string? FlexRank,
        [property: JsonPropertyName("flexLp")] int? FlexLp,
        [property: JsonPropertyName("lastSyncAt")] DateTime? LastSyncAt,
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt
    );
}

