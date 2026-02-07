using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Matches;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IMatchesRepository matchesRepo,
            [FromServices] IQueryFilterBuilder filterBuilder,
            [FromServices] ILogger<MatchListEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Match list: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Verify authenticated user matches route userId
                var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
                {
                    logger.LogWarning("Match list: user {AuthUserId} attempted to access data for user {RouteUserId}",
                        authenticatedUserId, userIdInt);
                    return Results.Forbid();
                }

                // Get riot accounts for this user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);

                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Match list: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                // Validate and build queue filter using centralized filter builder
                var validatedQueueType = filterBuilder.ValidateQueueType(queueType);
                var queueFilter = filterBuilder.BuildQueueFilter(validatedQueueType);

                logger.LogInformation("Match list request: userId={UserId}, puuid={Puuid}, queueType={Queue}",
                    userIdInt, primaryPuuid, validatedQueueType);

                // Fetch role baselines first (for trend badge computation)
                var baselines = await matchesRepo.GetRoleBaselinesAsync(primaryPuuid, queueFilter);

                // Fetch lightweight match summaries (no expensive team stat queries)
                var matches = await matchesRepo.GetMatchListSummaryAsync(primaryPuuid, queueFilter, 20, baselines);

                if (matches.Count == 0)
                {
                    logger.LogInformation("Match list: no matches found for puuid {Puuid} with queueType {Queue}",
                        primaryPuuid, validatedQueueType);
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

