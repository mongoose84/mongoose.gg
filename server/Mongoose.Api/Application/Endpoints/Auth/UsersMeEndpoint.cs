using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Auth;

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
            [FromServices] IUsersRepository usersRepo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<UsersMeEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and extract user ID
                var (authError, authenticatedUser) = AuthorizationHelper.GetAuthenticatedUser(httpContext, logger);
                if (authError != null)
                    return authError;

                // Get user from database
                var user = await usersRepo.GetByIdAsync(authenticatedUser!.UserId);
                if (user == null)
                {
                    logger.LogWarning("User not found for ID: {UserId}", LogSanitizer.Sanitize(authenticatedUser.UserId.ToString()));
                    return AuthResults.InvalidSession();
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    logger.LogWarning("Inactive user attempted to access /users/me: {UserId}", LogSanitizer.Sanitize(authenticatedUser.UserId.ToString()));
                    return AuthResults.AccountDeactivated();
                }

                // Get linked Riot accounts via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(authenticatedUser.UserId);
                var normalizedTier = user.Tier?.Trim().ToLowerInvariant() ?? "free";
                var visibleLinkedAccounts = linkedAccounts;
                if (normalizedTier == "free")
                {
                    var primaryOnly = linkedAccounts.Where(la => la.Link.IsPrimary).ToList();
                    visibleLinkedAccounts = primaryOnly.Count > 0
                        ? primaryOnly
                        : linkedAccounts.Take(1).ToList();
                }

                var riotAccountResponses = visibleLinkedAccounts.Select(la => new RiotAccountResponse(
                    la.Account.Puuid,
                    PuuidResolutionService.BuildAccountId(authenticatedUser.UserId, la.Account.Puuid),
                    la.Account.GameName,
                    la.Account.TagLine,
                    la.Account.SummonerName,
                    la.Account.Region,
                    la.Link.IsPrimary,
                    la.Account.SyncStatus,
                    la.Account.SyncProgress,
                    la.Account.SyncTotal,
                    la.Account.ProfileIconId,
                    la.Account.SummonerLevel,
                    la.Account.SoloTier,
                    la.Account.SoloRank,
                    la.Account.SoloLp,
                    la.Account.FlexTier,
                    la.Account.FlexRank,
                    la.Account.FlexLp,
                    la.Account.LastSyncAt,
                    la.Account.CreatedAt
                )).ToList();

                return Results.Ok(new UserMeResponse(
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.EmailVerified,
                    user.Tier ?? "free",
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
        [property: JsonPropertyName("accountId")] string AccountId,
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

