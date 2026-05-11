using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// Damage Per Minute Trend Endpoint
/// Returns DPM (totalDamageDealtToChampions / gameDurationMinutes) trend data for chart display.
/// Shared endpoint that can be used by solo, duo, and team dashboards.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// Filters out games shorter than 15 minutes for accuracy.
/// Free-tier users receive primary account data only; Pro-tier users may request all accounts.
/// </summary>
public sealed class DpmTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public DpmTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/damage-per-minute/{userId}";
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
            [FromServices] ILogger<DpmTrendEndpoint> logger
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

                // Fetch DPM trend data
                logger.LogInformation("DPM trend request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}, limit={Limit}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", LogSanitizer.HashForLog(accountId, "primary"), validatedLimit?.ToString() ?? "all");

                var (dataPoints, averageDamagePerMinute, overallAverage, trend) = await trendRepo.GetDpmTrendAsync(puuids, queueType, timeRange, validatedLimit, puuidToGameName);

                return Results.Ok(new DpmTrendResponse(dataPoints, averageDamagePerMinute, overallAverage, trend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "DPM trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DPM trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
