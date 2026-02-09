using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// LP Trend Endpoint
/// Returns LP progression trend data for chart display.
/// Shared endpoint that can be used by solo and duo dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex).
/// Note: LP data is only available for ranked queues.
/// </summary>
public sealed class LpTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public LpTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/lp/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] int? limit,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ITrendRepository trendRepo,
            [FromServices] ILogger<LpTrendEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("LP trend: invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("LP trend: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("LP trend: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Validate and default limit
                var dataLimit = limit ?? 100;
                if (dataLimit < 1) dataLimit = 20;
                if (dataLimit > 500) dataLimit = 500;

                // Fetch LP trend data
                logger.LogInformation("LP trend request: userId={UserId}, puuid={Puuid}, queueType={Queue}, limit={Limit}",
                    userIdInt, primaryPuuid, LogSanitizer.Sanitize(queueType) ?? "all", dataLimit);
                
                var lpTrend = await trendRepo.GetLpTrendAsync(primaryPuuid, queueType, dataLimit);

                return Results.Ok(new { lpTrend });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "LP trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LP trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

