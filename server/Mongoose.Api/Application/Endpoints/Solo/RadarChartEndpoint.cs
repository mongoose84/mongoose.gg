using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Radar chart endpoint.
/// Returns normalized and raw solo performance dimensions for spider chart visualization.
/// </summary>
public sealed class RadarChartEndpoint : IEndpoint
{
    public string Route { get; }

    public RadarChartEndpoint(string basePath)
    {
        Route = basePath + "/solo/radar-chart/{userId}";
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
            [FromServices] IRadarChartRepository radarChartRepo,
            [FromServices] ILogger<RadarChartEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

                // Resolve requested account scope
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                var puuids = resolvedAccounts!.Select(a => a.Account.Puuid).ToList();

                logger.LogInformation(
                    "Radar chart request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()),
                    puuids.Count,
                    LogSanitizer.Sanitize(queueType) ?? "all",
                    LogSanitizer.Sanitize(timeRange) ?? "all",
                    LogSanitizer.HashForLog(accountId, "primary"));

                var radarData = await radarChartRepo.GetRadarChartAsync(puuids, queueType, timeRange);
                if (radarData == null)
                {
                    return Results.Ok(new RadarChartResponse(Array.Empty<RadarAxis>(), 0));
                }

                return Results.Ok(radarData);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Radar chart: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Radar chart: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}