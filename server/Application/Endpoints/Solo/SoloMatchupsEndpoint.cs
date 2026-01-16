using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// Solo Matchups Endpoint
/// Returns champion matchup data showing top 5 most-played champions with opponent details.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class SoloMatchupsEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloMatchupsEndpoint(string basePath)
    {
        Route = basePath + "/solo/matchups/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] SoloStatsRepository soloStatsRepo,
            [FromServices] ILogger<SoloMatchupsEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Solo matchups: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Solo matchups: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Solo matchups: no gamers found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No gamers found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch matchups data
                logger.LogInformation("Solo matchups request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}",
                    userIdInt, primaryPuuid, queueType ?? "all", timeRange ?? "all");
                var matchups = await soloStatsRepo.GetChampionMatchupsAsync(primaryPuuid, queueType, timeRange);

                return Results.Ok(matchups);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo matchups: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo matchups: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

