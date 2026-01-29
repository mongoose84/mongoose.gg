using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Matches;
using RiotProxy.Core.QueryModels;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Matches;

/// <summary>
/// Match List Endpoint
/// Returns the last 20 matches with full stats and trend badges.
/// Includes role baselines for client-side highlight computation.
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
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] MatchesRepository matchesRepo,
            [FromServices] ILogger<MatchListEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

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

                // Get riot accounts for this user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);

                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Match list: no riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No riot accounts found for this user" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                // Validate and build queue filter
                var validatedQueueType = MatchesRepository.ValidateQueueType(queueType);
                var queueFilter = MatchesRepository.BuildQueueFilter(validatedQueueType);

                logger.LogInformation("Match list request: userId={UserId}, puuid={Puuid}, queueType={Queue}",
                    userIdInt, primaryPuuid, validatedQueueType);

                // Fetch role baselines first (for trend badge computation)
                var baselines = await matchesRepo.GetRoleBaselinesAsync(primaryPuuid, queueFilter);

                // Fetch matches with trend badges
                var matches = await matchesRepo.GetMatchListAsync(primaryPuuid, queueFilter, 20, baselines);

                if (matches.Count == 0)
                {
                    logger.LogInformation("Match list: no matches found for puuid {Puuid} with queueType {Queue}",
                        primaryPuuid, validatedQueueType);
                    return Results.Ok(new MatchListResponse(
                        Matches: Array.Empty<MatchListItem>(),
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

