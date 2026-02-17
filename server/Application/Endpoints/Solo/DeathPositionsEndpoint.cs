using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Interfaces;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Death Positions Endpoint (Danger Zones Heatmap)
/// Returns death coordinates and phase summary for spatial heatmap visualization.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all),
/// optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m),
/// and optional side filtering (blue, red, all).
/// Phase filtering (early/mid/late/veryLate) is handled client-side for instant UX.
/// </summary>
public sealed class DeathPositionsEndpoint : IEndpoint
{
    public string Route { get; }

    public DeathPositionsEndpoint(string basePath)
    {
        Route = basePath + "/solo/death-positions/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromQuery] string? side,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IDeathPositionsRepository deathPositionsRepo,
            [FromServices] ILogger<DeathPositionsEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Death positions: invalid userId format {UserId}", 
                        LogSanitizer.Sanitize(userId));
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Death positions: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Validate side parameter
                if (!string.IsNullOrEmpty(side))
                {
                    var validSides = new[] { "blue", "red", "all" };
                    if (!validSides.Contains(side.ToLowerInvariant()))
                    {
                        logger.LogWarning("Death positions: invalid side value {Side}", 
                            LogSanitizer.Sanitize(side));
                        return Results.BadRequest(new { error = "Invalid side value. Must be 'blue', 'red', or 'all'." });
                    }
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Death positions: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account (null-safe)
                var primaryAccount = linkedAccounts
                    .Where(la => la.Link.IsPrimary)
                    .Select(la => la.Account)
                    .FirstOrDefault() 
                    ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Fetch death positions data
                logger.LogInformation(
                    "Death positions request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}, side={Side}",
                    userIdInt, primaryPuuid, 
                    LogSanitizer.Sanitize(queueType) ?? "all",
                    LogSanitizer.Sanitize(timeRange) ?? "all",
                    LogSanitizer.Sanitize(side) ?? "all");

                var deathPositions = await deathPositionsRepo.GetDeathPositionsAsync(
                    primaryPuuid, queueType, timeRange, side);

                if (deathPositions == null)
                {
                    logger.LogInformation(
                        "Death positions: no data for puuid {Puuid} with filters queueType={Queue}, timeRange={TimeRange}, side={Side}",
                        primaryPuuid,
                        LogSanitizer.Sanitize(queueType) ?? "all",
                        LogSanitizer.Sanitize(timeRange) ?? "all",
                        LogSanitizer.Sanitize(side) ?? "all");
                    // Return empty response instead of 404 (matches spec behavior for no data)
                    return Results.Ok(new
                    {
                        deaths = Array.Empty<object>(),
                        totalDeaths = 0,
                        matchesAnalyzed = 0,
                        phaseSummary = new { early = 0, mid = 0, late = 0, veryLate = 0 }
                    });
                }

                return Results.Ok(deathPositions);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Death positions: bad request");
                // Do not expose internal exception messages to clients
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Death positions: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
