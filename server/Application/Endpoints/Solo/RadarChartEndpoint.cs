using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.Solo.RadarChartDto;

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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IRadarChartRepository radarChartRepo,
            [FromServices] ILogger<RadarChartEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Radar chart: invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Radar chart: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Radar chart: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                logger.LogInformation(
                    "Radar chart request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}",
                    userIdInt,
                    primaryPuuid,
                    LogSanitizer.Sanitize(queueType) ?? "all",
                    LogSanitizer.Sanitize(timeRange) ?? "all");

                var radarData = await radarChartRepo.GetRadarChartAsync(primaryPuuid, queueType, timeRange);
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