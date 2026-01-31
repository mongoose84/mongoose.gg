using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Matches;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Application.Endpoints.Matches;

/// <summary>
/// Match Details Endpoint
/// Returns full match data for a single selected match.
/// Called on-demand when user selects a match from the list.
/// Includes team stats, objectives, and performance metrics.
/// </summary>
public sealed class MatchDetailsEndpoint : IEndpoint
{
    public string Route { get; }

    public MatchDetailsEndpoint(string basePath)
    {
        Route = basePath + "/matches/{matchId}/details";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string matchId,
            [FromQuery] string? puuid,
            [FromServices] IMatchesRepository matchesRepo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IQueryFilterBuilder filterBuilder,
            [FromServices] ILogger<MatchDetailsEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(matchId))
                {
                    return Results.BadRequest(new { error = "matchId is required" });
                }

                if (string.IsNullOrWhiteSpace(puuid))
                {
                    return Results.BadRequest(new { error = "puuid query parameter is required" });
                }

                // Verify the puuid belongs to the authenticated user
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                // Check if user has this puuid linked via junction table
                var isLinked = await userRiotAccountsRepo.IsLinkedAsync(userId, puuid);
                if (!isLinked)
                {
                    logger.LogWarning("Match details: user {UserId} attempted to access data for unowned puuid {Puuid}",
                        userId, puuid);
                    return Results.Forbid();
                }

                logger.LogInformation("Match details request: matchId={MatchId}, puuid={Puuid}",
                    matchId, puuid);

                // Fetch match details using optimized query (CTEs instead of correlated subqueries)
                var matchDetails = await matchesRepo.GetMatchDetailsAsync(matchId, puuid);

                if (matchDetails == null)
                {
                    logger.LogWarning("Match details: match not found for matchId={MatchId}, puuid={Puuid}",
                        matchId, puuid);
                    return Results.NotFound(new { error = "Match not found" });
                }

                // Fetch role baseline for this match's role
                var queueFilter = filterBuilder.BuildQueueFilter("all");
                var baselines = await matchesRepo.GetRoleBaselinesAsync(puuid, queueFilter);
                baselines.TryGetValue(matchDetails.Role, out var baseline);

                var response = new MatchDetailsResponse(
                    Match: matchDetails,
                    Baseline: baseline
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match details: unhandled error for matchId {MatchId}", matchId);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

