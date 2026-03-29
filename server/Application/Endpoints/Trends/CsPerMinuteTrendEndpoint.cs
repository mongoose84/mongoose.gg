using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
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
            [FromQuery] string? accountId,
            [FromQuery] int? limit,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] ITrendRepository trendRepo,
            [FromServices] ILogger<CsPerMinuteTrendEndpoint> logger
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

                // Fetch CS per minute trend data
                logger.LogInformation("CS per minute trend request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}, limit={Limit}",
                    authorizedUser.UserId, puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", LogSanitizer.HashForLog(accountId, "primary"), validatedLimit?.ToString() ?? "all");

                var csPerMinuteTrend = await trendRepo.GetCsPerMinuteTrendAsync(puuids, queueType, timeRange, validatedLimit, puuidToGameName);

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
