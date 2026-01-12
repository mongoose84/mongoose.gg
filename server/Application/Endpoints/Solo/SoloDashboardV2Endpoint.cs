using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// v2 Solo Dashboard Endpoint
/// Returns comprehensive solo player statistics optimized for dashboard rendering.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all).
/// </summary>
public sealed class SoloDashboardV2Endpoint : IEndpoint
{
    public string Route { get; }

    public SoloDashboardV2Endpoint(string basePath)
    {
        Route = basePath + "/solo/dashboard/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timePeriod,
            [FromServices] V2RiotAccountsRepository riotAccountsRepo,
            [FromServices] V2SoloStatsRepository v2SoloStatsRepo,
            [FromServices] ILogger<SoloDashboardV2Endpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!long.TryParse(userId, out var userIdLong))
                {
                    logger.LogWarning("Solo v2 dashboard: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Get riot accounts for this user from v2 schema
                var riotAccounts = await riotAccountsRepo.GetByUserIdAsync(userIdLong);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Solo v2 dashboard: no riot accounts found for userId {UserId}", userIdLong);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Get the primary account, or fall back to first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch dashboard data (always returns a response, even if empty)
                logger.LogInformation("Solo v2 dashboard request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timePeriod={TimePeriod}",
                    userIdLong, primaryPuuid, queueType ?? "all", timePeriod ?? "all");
                var dashboard = await v2SoloStatsRepo.GetSoloDashboardAsync(primaryPuuid, queueType, timePeriod);

                // Dashboard is never null - returns empty stats if no matches found
                return Results.Ok(dashboard);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo v2 dashboard: bad request");
                // Do not expose internal exception messages to clients
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo v2 dashboard: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
