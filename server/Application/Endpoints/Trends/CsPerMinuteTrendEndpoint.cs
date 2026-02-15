using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// CS Per Minute Trend Endpoint
/// Returns CS per minute trend data for chart display showing farming efficiency over time.
/// Shared endpoint that can be used by solo, duo, and team dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// Filters out games shorter than 15 minutes for accuracy.
/// </summary>
public sealed class CsPerMinuteTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public CsPerMinuteTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/cs-per-minute/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromQuery] int? limit,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ITrendRepository trendRepo,
            [FromServices] ILogger<CsPerMinuteTrendEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("CS per minute trend: invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("CS per minute trend: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("CS per minute trend: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Validate limit if provided
                int? validatedLimit = null;
                if (limit.HasValue)
                {
                    validatedLimit = limit.Value;
                    if (validatedLimit < 1) validatedLimit = 20;
                    if (validatedLimit > 500) validatedLimit = 500;
                }

                // Fetch CS per minute trend data
                logger.LogInformation("CS per minute trend request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}, limit={Limit}",
                    userIdInt, primaryPuuid, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", validatedLimit?.ToString() ?? "all");

                var csPerMinuteTrend = await trendRepo.GetCsPerMinuteTrendAsync(primaryPuuid, queueType, timeRange, validatedLimit);

                return Results.Ok(new CsPerMinuteTrendResponse(csPerMinuteTrend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "CS per minute trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CS per minute trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
