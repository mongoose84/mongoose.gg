using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// Winrate Trend Endpoint
/// Returns rolling average winrate trend data for chart display.
/// Shared endpoint that can be used by solo, duo, and team dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class WinrateTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public WinrateTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/winrate/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromQuery] string? accountId,
            [FromQuery] int? limit,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] ITrendRepository trendRepo,
            [FromServices] ILogger<WinrateTrendEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                var puuids = resolvedAccounts!.Select(a => a.Account.Puuid).ToList();
                var puuidToGameName = resolvedAccounts!.ToDictionary(a => a.Account.Puuid, a => $"{a.Account.GameName}#{a.Account.TagLine}");

                // Validate limit if provided
                int? validatedLimit = null;
                if (limit.HasValue)
                {
                    validatedLimit = limit.Value;
                    if (validatedLimit < 1) validatedLimit = 20;
                    if (validatedLimit > 500) validatedLimit = 500;
                }

                // Fetch winrate trend data
                logger.LogInformation("Winrate trend request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}, limit={Limit}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", LogSanitizer.HashForLog(accountId, "primary"), validatedLimit?.ToString() ?? "all");

                var winrateTrend = await trendRepo.GetWinrateTrendAsync(puuids, queueType, timeRange, validatedLimit, puuidToGameName);

                return Results.Ok(new WinrateTrendResponse(winrateTrend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Winrate trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Winrate trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

