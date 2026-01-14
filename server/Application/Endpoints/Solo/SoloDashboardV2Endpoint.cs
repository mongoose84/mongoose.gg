using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// Solo Dashboard Endpoint
/// Returns comprehensive solo player statistics optimized for dashboard rendering.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all).
/// </summary>
public sealed class SoloDashboardEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloDashboardEndpoint(string basePath)
    {
        Route = basePath + "/solo/dashboard/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] SoloStatsRepository soloStatsRepo,
            [FromServices] ILogger<SoloDashboardEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Solo v2 dashboard: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Solo v2 dashboard: no gamers found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No gamers found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch dashboard data
                logger.LogInformation("Solo v2 dashboard request: userId={UserId}, puuid={Puuid}, queueType={Queue}", userIdInt, primaryPuuid, queueType ?? "all");
                var dashboard = await soloStatsRepo.GetSoloDashboardAsync(primaryPuuid, queueType);

                if (dashboard == null)
                {
                    logger.LogInformation("Solo v2 dashboard: no match data for puuid {Puuid} with queueType {Queue}", primaryPuuid, queueType ?? "all");
                    return Results.NotFound(new { error = "No match data found for this player" });
                }

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
