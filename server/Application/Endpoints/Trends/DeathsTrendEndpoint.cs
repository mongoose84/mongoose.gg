using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Application.Endpoints.Trends;

/// <summary>
/// Deaths Trend Endpoint
/// Returns deaths per game with rolling average trend data for chart display.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class DeathsTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public DeathsTrendEndpoint(string basePath)
    {
        Route = basePath + "/trends/deaths/{userId}";
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
            [FromServices] ILogger<DeathsTrendEndpoint> logger
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

                // Fetch deaths trend data
                logger.LogInformation("Deaths trend request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}, limit={Limit}",
                    authorizedUser.UserId, puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all", LogSanitizer.Sanitize(accountId) ?? "primary", validatedLimit?.ToString() ?? "all");

                var (dataPoints, averageDeaths, overallAverage, trend) = await trendRepo.GetDeathsTrendAsync(puuids, queueType, timeRange, validatedLimit);

                return Results.Ok(new DeathsTrendResponse(dataPoints, averageDeaths, overallAverage, trend));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Deaths trend: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deaths trend: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
