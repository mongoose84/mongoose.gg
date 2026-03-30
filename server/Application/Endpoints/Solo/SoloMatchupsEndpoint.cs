using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo Matchups Endpoint
/// Returns champion matchup data showing top 5 most-played champions with opponent details.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class SoloMatchupsEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloMatchupsEndpoint(string basePath)
    {
        Route = basePath + "/solo/matchups/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IMatchupRepository matchupRepo,
            [FromServices] ILogger<SoloMatchupsEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

                // Resolve primary Riot account
                var (accountError, resolvedAccount) = await puuidResolutionService.ResolvePrimaryAccountAsync(authorizedUser!.UserId);
                if (accountError != null)
                    return accountError;

                var primaryPuuid = resolvedAccount!.Account.Puuid;

                // Fetch matchups data
                logger.LogInformation("Solo matchups request: userId={UserId}, puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), LogSanitizer.HashForLog(primaryPuuid), LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(timeRange) ?? "all");
                var matchups = await matchupRepo.GetChampionMatchupsAsync(primaryPuuid, queueType, timeRange);

                return Results.Ok(matchups);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo matchups: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo matchups: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

