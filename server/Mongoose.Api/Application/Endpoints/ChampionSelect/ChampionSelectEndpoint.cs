using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.ChampionSelect;

/// <summary>
/// Champion Select Endpoint
/// Returns champion recommendations and statistics optimized for champion select.
/// Uses a focused repository that fetches only the data needed (main champions, games played, win rate)
/// instead of the full solo performance data.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class ChampionSelectEndpoint : IEndpoint
{
    public string Route { get; }

    public ChampionSelectEndpoint(string basePath)
    {
        Route = basePath + "/champion-select/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IChampionSelectRepository championSelectRepo,
            [FromServices] ILogger<ChampionSelectEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

                // Resolve requested account scope (primary/all/specific)
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                if (resolvedAccounts == null || resolvedAccounts.Count == 0)
                {
                    return Results.NotFound(new { error = "No riot accounts found for this user", code = "RIOT_ACCOUNT_NOT_FOUND" });
                }

                var puuids = resolvedAccounts.Select(a => a.Account.Puuid).ToList();

                // Fetch champion select data (only main champions, games played, win rate)
                logger.LogInformation("Champion select request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), resolvedAccounts.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all");
                var championSelectData = await championSelectRepo.GetChampionSelectDataAsync(puuids, queueType, timeRange);

                if (championSelectData == null)
                {
                    logger.LogInformation("Champion select: no match data for accounts {AccountCount} with queueType {Queue} and timeRange {TimeRange}",
                        resolvedAccounts.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all");
                    return Results.NotFound(new { error = "No match data found for this player" });
                }

                return Results.Ok(championSelectData);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Champion select: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Champion select: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

