using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Match Activity Endpoint
/// Returns daily match counts for the past 6 months for heatmap visualization.
/// Used to render a GitHub-style contribution graph.
/// </summary>
public sealed class MatchActivityEndpoint : IEndpoint
{
    public string Route { get; }

    public MatchActivityEndpoint(string basePath)
    {
        Route = basePath + "/solo/activity/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] ITrendRepository trendRepo,
            [FromServices] ILogger<MatchActivityEndpoint> logger
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

                // Fetch daily match counts for past 6 months (182 days)
                const int daysBack = 182;
                var dailyCounts = await trendRepo.GetDailyMatchCountsAsync(puuids, daysBack);
                
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-daysBack);
                var totalMatches = dailyCounts.Values.Sum();

                var response = new MatchActivityResponse(
                    DailyMatchCounts: dailyCounts,
                    StartDate: startDate.ToString("yyyy-MM-dd"),
                    EndDate: endDate.ToString("yyyy-MM-dd"),
                    TotalMatches: totalMatches
                );

                logger.LogInformation("Match activity: userId={UserId}, accountCount={AccountCount}, totalMatches={Total}", 
                    authorizedUser.UserId, puuids.Count, totalMatches);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match activity: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

