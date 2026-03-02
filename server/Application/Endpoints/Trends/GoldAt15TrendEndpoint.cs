using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// Gold at 15 Trend Endpoint
/// Returns gold at 15-minute mark trend data for chart display with opponent comparison.
/// Shared endpoint that can be used by solo, duo, and team dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class GoldAt15TrendEndpoint : IEndpoint
{
    public string Route { get; }

    public GoldAt15TrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/gold-at-15/{userId}";
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
            [FromServices] ILogger<GoldAt15TrendEndpoint> logger
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

                // Validate limit if provided  
                int? validatedLimit = null;
                if (limit.HasValue)
                {
                    validatedLimit = limit.Value;
                    if (validatedLimit < 1) validatedLimit = 20;
                    if (validatedLimit > 500) validatedLimit = 500;
                }

                // Fetch gold at 15 trend data
                logger.LogInformation("Gold at 15 trend request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}, limit={Limit}",
                    authorizedUser.UserId, puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", LogSanitizer.HashForLog(accountId, "primary"), validatedLimit?.ToString() ?? "all");

                var goldAt15Trend = await trendRepo.GetGoldAt15TrendAsync(puuids, queueType, timeRange, validatedLimit);

                return Results.Ok(new GoldAt15TrendResponse(goldAt15Trend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Gold at 15 trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gold at 15 trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
