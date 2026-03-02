using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Interfaces;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;

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
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IDeathPositionsRepository deathPositionsRepo,
            [FromServices] ILogger<DeathPositionsEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

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

                // Resolve requested account scope
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                var puuids = resolvedAccounts!.Select(a => a.Account.Puuid).ToList();

                // Fetch death positions data
                logger.LogInformation(
                    "Death positions request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, side={Side}, account={Account}",
                    authorizedUser.UserId, puuids.Count,
                    LogSanitizer.Sanitize(queueType) ?? "all",
                    LogSanitizer.Sanitize(timeRange) ?? "all",
                    LogSanitizer.Sanitize(side) ?? "all",
                    LogSanitizer.HashForLog(accountId, "primary"));

                var deathPositions = await deathPositionsRepo.GetDeathPositionsAsync(
                    puuids, queueType, timeRange, side);

                if (deathPositions == null)
                {
                    logger.LogInformation(
                        "Death positions: no data for puuid {Puuid} with filters queueType={Queue}, timeRange={TimeRange}, side={Side}",
                        string.Join(",", puuids),
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
