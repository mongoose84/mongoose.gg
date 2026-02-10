using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.ChampionSelect;

/// <summary>
/// Champion Select Endpoint
/// Returns champion recommendations and statistics optimized for champion select.
/// Uses a focused repository that fetches only the data needed (main champions, games played, win rate)
/// instead of the full solo performance data.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class ChampionSelectEndpoint : IEndpoint
{
    public string Route { get; }

    public ChampionSelectEndpoint(string basePath)
    {
        Route = basePath + "/champion-select/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IChampionSelectRepository championSelectRepo,
            [FromServices] ILogger<ChampionSelectEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Champion select: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Champion select: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get linked riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Champion select: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch champion select data (only main champions, games played, win rate)
                logger.LogInformation("Champion select request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}",
                    userIdInt, primaryPuuid, queueType ?? "all", timeRange ?? "all");
                var championSelectData = await championSelectRepo.GetChampionSelectDataAsync(primaryPuuid, queueType, timeRange);

                if (championSelectData == null)
                {
                    logger.LogInformation("Champion select: no match data for puuid {Puuid} with queueType {Queue} and timeRange {TimeRange}",
                        primaryPuuid, queueType ?? "all", timeRange ?? "all");
                    return Results.NotFound(new { error = "No match data found for this player" });
                }

                return Results.Ok(championSelectData);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Champion select: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Champion select: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

