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
            [FromQuery] string? accountId,
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

                // Resolve requested account scope
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                var puuids = resolvedAccounts!.Select(a => a.Account.Puuid).ToList();

                // Validate and build queue filter using centralized filter builder
                var validatedQueueType = filterBuilder.ValidateQueueType(queueType);
                var queueFilter = filterBuilder.BuildQueueFilter(validatedQueueType);

                logger.LogInformation("Match list request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, account={Account}",
                    authorizedUser.UserId, puuids.Count, LogSanitizer.Sanitize(validatedQueueType) ?? "all", LogSanitizer.HashForLog(accountId, "primary"));

                // Fetch role baselines first (for trend badge computation)
                var baselines = await matchesRepo.GetRoleBaselinesAsync(puuids, queueFilter);

                // Fetch lightweight match summaries (no expensive team stat queries)
                var matches = await matchesRepo.GetMatchListSummaryAsync(puuids, queueFilter, 20, baselines);

                if (matches.Count == 0)
                {
                    logger.LogInformation("Match list: no matches found for puuid {Puuid} with queueType {Queue}",
                        string.Join(",", puuids.Select(p => LogSanitizer.HashForLog(p))), LogSanitizer.Sanitize(validatedQueueType) ?? "all");
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

