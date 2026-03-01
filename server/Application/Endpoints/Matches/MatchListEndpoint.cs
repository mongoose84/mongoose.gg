using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Application.Endpoints.Matches;

/// <summary>
/// Match List Endpoint
/// Returns lightweight match summaries for fast list rendering.
/// Full match details are fetched on-demand via GET /matches/{matchId}/details.
/// Includes role baselines for trend badge computation.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all).
/// </summary>
public sealed class MatchListEndpoint : IEndpoint
{
    public string Route { get; }

    public MatchListEndpoint(string basePath)
    {
        Route = basePath + "/matches/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IMatchesRepository matchesRepo,
            [FromServices] IQueryFilterBuilder filterBuilder,
            [FromServices] ILogger<MatchListEndpoint> logger
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

                // Validate and build queue filter using centralized filter builder
                var validatedQueueType = filterBuilder.ValidateQueueType(queueType);
                var queueFilter = filterBuilder.BuildQueueFilter(validatedQueueType);

                logger.LogInformation("Match list request: userId={UserId}, puuid={Puuid}, queueType={Queue}",
                    authorizedUser.UserId, primaryPuuid, LogSanitizer.Sanitize(validatedQueueType) ?? "all");

                // Fetch role baselines first (for trend badge computation)
                var baselines = await matchesRepo.GetRoleBaselinesAsync(primaryPuuid, queueFilter);

                // Fetch lightweight match summaries (no expensive team stat queries)
                var matches = await matchesRepo.GetMatchListSummaryAsync(primaryPuuid, queueFilter, 20, baselines);

                if (matches.Count == 0)
                {
                    logger.LogInformation("Match list: no matches found for puuid {Puuid} with queueType {Queue}",
                        primaryPuuid, LogSanitizer.Sanitize(validatedQueueType) ?? "all");
                    return Results.Ok(new MatchListResponse(
                        Matches: Array.Empty<MatchListSummaryItem>(),
                        BaselinesByRole: baselines,
                        QueueType: validatedQueueType,
                        TotalMatches: 0
                    ));
                }

                var response = new MatchListResponse(
                    Matches: matches.ToArray(),
                    BaselinesByRole: baselines,
                    QueueType: validatedQueueType,
                    TotalMatches: matches.Count
                );

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Match list: bad request");
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match list: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

