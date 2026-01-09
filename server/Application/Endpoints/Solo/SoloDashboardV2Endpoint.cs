using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using static RiotProxy.Application.DTOs.SoloSummaryDto;

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
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] V2SoloStatsRepository v2SoloStatsRepo
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                    return Results.BadRequest(new { error = "Invalid userId format" });

                // Get gamers for this user
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                if (distinctPuuIds.Length == 0)
                    return Results.NotFound(new { error = "No gamers found for this user" });

                // Aggregate stats from all gamers (team perspective)
                // For now, fetch for primary gamer; TODO: support team aggregation
                var primaryPuuid = distinctPuuIds[0];

                // Fetch dashboard data
                var dashboard = await v2SoloStatsRepo.GetSoloDashboardAsync(primaryPuuid, queueType);

                if (dashboard == null)
                    return Results.NotFound(new { error = "No match data found for this player" });

                return Results.Ok(dashboard);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SoloDashboardV2Endpoint: {ex.Message}");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
