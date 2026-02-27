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

                // Resolve primary Riot account
                var (accountError, resolvedAccount) = await puuidResolutionService.ResolvePrimaryAccountAsync(authorizedUser!.UserId);
                if (accountError != null)
                    return accountError;

                var primaryPuuid = resolvedAccount!.Account.Puuid;

                // Fetch daily match counts for past 6 months (182 days)
                const int daysBack = 182;
                var dailyCounts = await trendRepo.GetDailyMatchCountsAsync(primaryPuuid, daysBack);
                
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-daysBack);
                var totalMatches = dailyCounts.Values.Sum();

                var response = new MatchActivityResponse(
                    DailyMatchCounts: dailyCounts,
                    StartDate: startDate.ToString("yyyy-MM-dd"),
                    EndDate: endDate.ToString("yyyy-MM-dd"),
                    TotalMatches: totalMatches
                );

                logger.LogInformation("Match activity: userId={UserId}, puuid={Puuid}, totalMatches={Total}", 
                    authorizedUser.UserId, primaryPuuid, totalMatches);

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

