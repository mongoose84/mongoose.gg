using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Solo;

	/// <summary>
	/// Solo Dashboard Endpoint
	/// Returns comprehensive solo player statistics optimized for dashboard rendering.
	/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
	/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
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
	            [FromQuery] string? timeRange,
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
                    logger.LogWarning("Solo dashboard: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Solo dashboard: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Solo dashboard: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

	                // Fetch dashboard data
	                logger.LogInformation("Solo dashboard request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}", userIdInt, primaryPuuid, queueType ?? "all", timeRange ?? "all");
	                var dashboard = await soloStatsRepo.GetSoloDashboardAsync(primaryPuuid, queueType, timeRange);

	                if (dashboard == null)
	                {
	                    logger.LogInformation("Solo dashboard: no match data for puuid {Puuid} with queueType {Queue} and timeRange {TimeRange}", primaryPuuid, queueType ?? "all", timeRange ?? "all");
	                    return Results.NotFound(new { error = "No match data found for this player" });
	                }

                return Results.Ok(dashboard);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo dashboard: bad request");
                // Do not expose internal exception messages to clients
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo dashboard: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
