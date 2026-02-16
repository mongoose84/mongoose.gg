using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// Vision Score Trend Endpoint
/// Returns vision score per minute with rolling average trend data for chart display.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// Includes role-appropriate targets (Support: 2.0/min, Others: 1.0/min).
/// </summary>
public sealed class VisionScoreTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public VisionScoreTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/vision-score/{userId}";
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
            [FromServices] ILogger<VisionScoreTrendEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Vision score trend: invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Vision score trend: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Vision score trend: no riot accounts found for userId {UserId}", userIdInt);
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

                // Fetch vision score trend data
                logger.LogInformation("Vision score trend request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}, limit={Limit}",
                    userIdInt, primaryPuuid, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", validatedLimit?.ToString() ?? "all");

                var (dataPoints, averageVisionPerMinute, overallAverage, roleTarget, trend) = await trendRepo.GetVisionScoreTrendAsync(primaryPuuid, queueType, timeRange, validatedLimit);

                return Results.Ok(new VisionScoreTrendResponse(dataPoints, averageVisionPerMinute, overallAverage, roleTarget, trend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Vision score trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vision score trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
