using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Application.Endpoints.Matches;

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
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IMatchesRepository matchesRepo,
            [FromServices] IQueryFilterBuilder filterBuilder,
            [FromServices] ILogger<MatchDetailsEndpoint> logger
        ) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(matchId))
                {
                    return Results.BadRequest(new { error = "matchId is required" });
                }

                // Validate authentication and extract user ID
                var (authError, authenticatedUser) = AuthorizationHelper.GetAuthenticatedUser(httpContext, logger);
                if (authError != null)
                    return authError;

                // Resolve selected account from server-side linked accounts.
                // accountId can be null (defaults to primary), a specific opaque accountId, or "all".
                // This endpoint requires a single account context.
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authenticatedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                if (resolvedAccounts == null || resolvedAccounts.Count != 1)
                {
                    return Results.BadRequest(new { error = "accountId must resolve to a single account" });
                }

                var selectedPuuid = resolvedAccounts[0].Account.Puuid;

                logger.LogInformation("Match details request: matchId={MatchId}, account={Account}",
                    LogSanitizer.Sanitize(matchId), LogSanitizer.HashForLog(accountId, "primary"));

                // Fetch match details using optimized query (CTEs instead of correlated subqueries)
                var matchDetails = await matchesRepo.GetMatchDetailsAsync(matchId, selectedPuuid);

                if (matchDetails == null)
                {
                    logger.LogWarning("Match details: match not found for matchId={MatchId}, account={Account}",
                        LogSanitizer.Sanitize(matchId), LogSanitizer.HashForLog(accountId, "primary"));
                    return Results.NotFound(new { error = "Match not found" });
                }

                // Fetch role baseline for this match's role
                var queueFilter = filterBuilder.BuildQueueFilter("all");
                var baselines = await matchesRepo.GetRoleBaselinesAsync(selectedPuuid, queueFilter);
                baselines.TryGetValue(matchDetails.Role, out var baseline);

                var response = new MatchDetailsResponse(
                    Match: matchDetails,
                    Baseline: baseline
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match details: unhandled error for matchId {MatchId}", LogSanitizer.Sanitize(matchId));
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

