using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.Endpoints.Shared;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// Solo Performance Endpoint
/// Returns comprehensive solo player statistics optimized for dashboard rendering.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class SoloPerformanceEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloPerformanceEndpoint(string basePath)
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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ISoloPerformanceRepository soloPerformanceRepo,
            [FromServices] ILogger<SoloPerformanceEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Solo performance: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Solo performance: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Solo performance: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch performance data
                logger.LogInformation("Solo performance request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}", userIdInt, primaryPuuid, queueType ?? "all", timeRange ?? "all");
                var performance = await soloPerformanceRepo.GetSoloPerformanceAsync(primaryPuuid, queueType, timeRange);

                if (performance == null)
                {
                    logger.LogInformation("Solo performance: no match data for puuid {Puuid} with queueType {Queue} and timeRange {TimeRange}", primaryPuuid, queueType ?? "all", timeRange ?? "all");
                    return Results.NotFound(new { error = "No match data found for this player" });
                }

                return Results.Ok(performance);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo performance: bad request");
                // Do not expose internal exception messages to clients
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo performance: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

