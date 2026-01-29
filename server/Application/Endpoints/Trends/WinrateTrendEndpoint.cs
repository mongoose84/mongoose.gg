using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Trends;

/// <summary>
/// Winrate Trend Endpoint
/// Returns rolling average winrate trend data for chart display.
/// Shared endpoint that can be used by solo, duo, and team dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class WinrateTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public WinrateTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/winrate/{userId}";
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
            [FromServices] ILogger<WinrateTrendEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Winrate trend: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Winrate trend: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Winrate trend: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch winrate trend data
                logger.LogInformation("Winrate trend request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}",
                    userIdInt, primaryPuuid, queueType ?? "all", timeRange ?? "all");
                
                var winrateTrend = await soloStatsRepo.GetWinrateTrendAsync(primaryPuuid, queueType, timeRange);

                return Results.Ok(new { winrateTrend });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Winrate trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Winrate trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

