using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.RankInfoDto;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo Performance Endpoint
/// Returns comprehensive solo player statistics optimized for dashboard rendering.
/// Supports optional queue filtering (ranked_solo, ranked_flex, normal, aram, all)
/// and optional time range filtering (current_season, last_season, 1w, 1m, 3m, 6m).
/// </summary>
public sealed class SoloPerformanceEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloPerformanceEndpoint(string basePath)
    {
        Route = basePath + "/solo/dashboard/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? timeRange,
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] ISoloPerformanceRepository soloPerformanceRepo,
            [FromServices] ILogger<SoloPerformanceEndpoint> logger
        ) =>
        {
            try
            {
                // Validate authentication and authorization
                var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
                if (authError != null)
                    return authError;

                // Resolve requested account scope (primary/all/specific)
                var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
                if (accountError != null)
                    return accountError;

                if (resolvedAccounts == null || resolvedAccounts.Count == 0)
                {
                    return Results.NotFound(new { error = "No riot accounts found for this user", code = "RIOT_ACCOUNT_NOT_FOUND" });
                }

                var primaryResolvedAccount = resolvedAccounts.FirstOrDefault(a => a.IsPrimary);
                var riotAccount = primaryResolvedAccount?.Account ?? resolvedAccounts[0].Account;
                var puuids = resolvedAccounts.Select(a => a.Account.Puuid).ToList();

                // Fetch performance data
                logger.LogInformation("Solo performance request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, account={Account}",
                    authorizedUser.UserId, puuids.Count,
                    LogSanitizer.Sanitize(queueType) ?? "all",
                    LogSanitizer.Sanitize(timeRange) ?? "all",
                        LogSanitizer.HashForLog(accountId, "primary"));

                var performance = await soloPerformanceRepo.GetSoloPerformanceAsync(puuids, queueType, timeRange);

                if (performance == null)
                {
                    logger.LogInformation("Solo performance: no match data for puuid {Puuid} with queueType {Queue} and timeRange {TimeRange}",
                        LogSanitizer.HashForLog(riotAccount.Puuid),
                        LogSanitizer.Sanitize(queueType) ?? "all",
                        LogSanitizer.Sanitize(timeRange) ?? "all");
                    return Results.NotFound(new { error = "No match data found for this player" });
                }

                // Build rank info from the account data (no additional DB calls needed)
                var soloDuoRank = new QueueRankInfo(
                    riotAccount.SoloTier,
                    riotAccount.SoloRank,
                    riotAccount.SoloLp,
                    !string.IsNullOrEmpty(riotAccount.SoloTier) && !string.IsNullOrEmpty(riotAccount.SoloRank)
                );

                var flexRank = new QueueRankInfo(
                    riotAccount.FlexTier,
                    riotAccount.FlexRank,
                    riotAccount.FlexLp,
                    !string.IsNullOrEmpty(riotAccount.FlexTier) && !string.IsNullOrEmpty(riotAccount.FlexRank)
                );

                var rankInfo = new RankInfo(soloDuoRank, flexRank);

                // Return enhanced response with performance data and rank info
                var response = SoloPerformanceWithRankResponse.FromPerformanceAndRank(performance, rankInfo);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Solo performance: bad request");
                // Do not expose internal exception messages to clients
                return Results.BadRequest(new { error = "Invalid request parameters" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo performance: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}

