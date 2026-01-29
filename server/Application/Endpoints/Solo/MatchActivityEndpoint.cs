using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Solo;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Solo;

/// <summary>
/// Match Activity Endpoint
/// Returns daily match counts for the past 6 months for heatmap visualization.
/// Used to render a GitHub-style contribution graph.
/// </summary>
public sealed class MatchActivityEndpoint : IEndpoint
{
    public string Route { get; }

    public MatchActivityEndpoint(string basePath)
    {
        Route = basePath + "/solo/activity/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] SoloStatsRepository soloStatsRepo,
            [FromServices] ILogger<MatchActivityEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Match activity: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Match activity: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Match activity: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch daily match counts for past 6 months (182 days)
                const int daysBack = 182;
                var dailyCounts = await soloStatsRepo.GetDailyMatchCountsAsync(primaryPuuid, daysBack);
                
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-daysBack);
                var totalMatches = dailyCounts.Values.Sum();

                var response = new MatchActivityResponse(
                    DailyMatchCounts: dailyCounts,
                    StartDate: startDate.ToString("yyyy-MM-dd"),
                    EndDate: endDate.ToString("yyyy-MM-dd"),
                    TotalMatches: totalMatches
                );

                logger.LogInformation("Match activity: userId={UserId}, puuid={Puuid}, totalMatches={Total}", 
                    userIdInt, primaryPuuid, totalMatches);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match activity: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

