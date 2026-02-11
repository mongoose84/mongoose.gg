using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Application.Endpoints.Overview;

/// <summary>
/// Overview Endpoint
/// Returns aggregated dashboard data for the Overview page.
/// Includes player header, rank snapshot, last match, active goals, and suggested actions.
/// Primary queue is auto-selected based on highest match count in recent window.
/// </summary>
public sealed class OverviewEndpoint : IEndpoint
{
    public string Route { get; }

    // Data Dragon version for icon URLs
    private const string DataDragonVersion = "16.1.1";

    public OverviewEndpoint(string basePath)
    {
        Route = basePath + "/overview/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] OverviewStatsRepository overviewStatsRepo,
            [FromServices] ILpCalculationService lpCalc,
            [FromServices] ILogger<OverviewEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return AuthResults.NotAuthenticated();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Overview: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Get riot accounts for the user via junction table
                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
                if (linkedAccounts == null || linkedAccounts.Count == 0)
                {
                    logger.LogWarning("Overview: no Riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No linked Riot accounts found" });
                }

                // Use primary account or first account
                var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary);
                var primaryAccount = primaryLink.Account ?? linkedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;

                logger.LogInformation("Overview request: userId={UserId}, puuid={Puuid}", userIdInt, primaryPuuid);

                // Build player header
                var profileIconUrl = BuildProfileIconUrl(primaryAccount.ProfileIconId);
                var activeContexts = DetermineActiveContexts(linkedAccounts.Count);
                var playerHeader = new PlayerHeader(
                    SummonerName: primaryAccount.SummonerName,
                    Level: primaryAccount.SummonerLevel ?? 0,
                    Region: primaryAccount.Region.ToUpperInvariant(),
                    ProfileIconUrl: profileIconUrl,
                    ActiveContexts: activeContexts
                );

                // Determine primary queue
                var (primaryQueueId, primaryQueueLabel, _) = await overviewStatsRepo.GetPrimaryQueueAsync(primaryPuuid);

                // Get last 20 matches for primary queue
                var last20Matches = await overviewStatsRepo.GetLast20MatchesAsync(primaryPuuid, primaryQueueId);

                // Calculate rank snapshot using per-match LP data
                var rankSnapshot = BuildRankSnapshot(
                    primaryAccount,
                    primaryQueueId,
                    primaryQueueLabel,
                    last20Matches,
                    lpCalc
                );

                // Get last match
                var lastMatchData = await overviewStatsRepo.GetLastMatchAsync(primaryPuuid);
                var lastMatch = lastMatchData != null ? BuildLastMatch(lastMatchData) : null;

                // Active goals (placeholder - no goals table yet, return empty)
                var activeGoals = Array.Empty<GoalPreview>();

                // Suggested actions (placeholder - return empty for now)
                var suggestedActions = Array.Empty<SuggestedAction>();

                var response = new OverviewResponse(
                    PlayerHeader: playerHeader,
                    RankSnapshot: rankSnapshot,
                    LastMatch: lastMatch,
                    ActiveGoals: activeGoals,
                    SuggestedActions: suggestedActions
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Overview: unhandled error for userId {UserId}", userId);
                return Results.Problem("An unexpected error occurred");
            }
        }).RequireAuthorization();
    }

    private static string BuildProfileIconUrl(int? profileIconId)
    {
        var iconId = profileIconId ?? 29; // Default icon if not set
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/profileicon/{iconId}.png";
    }

    private static string[] DetermineActiveContexts(int accountCount)
    {
        // Solo is always active if there's at least one account
        // Duo and Team badges will be added when those features are available
        var contexts = new List<string> { "Solo" };
        
        // Could check for duo partners or team memberships in the future
        // For now, just return Solo
        
        return contexts.ToArray();
    }

    private static RankSnapshot BuildRankSnapshot(
        Mongoose.Api.Core.Entities.RiotAccount account,
        int primaryQueueId,
        string primaryQueueLabel,
        List<MatchResultData> last20Matches,
        ILpCalculationService lpCalc)
    {
        // Get current rank based on primary queue
        string? rank = null;
        int? currentLp = null;
        string? currentTier = null;
        string? currentDivision = null;

        if (primaryQueueId == 420) // Ranked Solo/Duo
        {
            if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
            {
                rank = $"{account.SoloTier} {account.SoloRank}";
                currentLp = account.SoloLp;
                currentTier = account.SoloTier;
                currentDivision = account.SoloRank;
            }
        }
        else if (primaryQueueId == 440) // Ranked Flex
        {
            if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
            {
                rank = $"{account.FlexTier} {account.FlexRank}";
                currentLp = account.FlexLp;
                currentTier = account.FlexTier;
                currentDivision = account.FlexRank;
            }
        }

        // Calculate wins and losses from last 20
        var last20Wins = last20Matches.Count(m => m.Win);
        var last20Losses = last20Matches.Count(m => !m.Win);

        // Build W/L array (newest first, true = win, false = loss)
        var wlLast20 = last20Matches.Select(m => m.Win).ToArray();

        // Calculate LP delta from per-match LP data (participants.lp_after)
        var lpDeltaLast20 = CalculateLpDeltaFromMatches(
            currentTier,
            currentDivision,
            currentLp,
            last20Matches,
            lpCalc
        );

        // We no longer calculate per-match LP deltas since we don't have accurate per-match LP data
        // Return an empty array - the UI should handle this gracefully
        var lpDeltasLast20 = Array.Empty<int>();

        return new RankSnapshot(
            PrimaryQueueLabel: primaryQueueLabel,
            Rank: rank,
            Lp: currentLp,
            LpDeltaLast20: lpDeltaLast20,
            Last20Wins: last20Wins,
            Last20Losses: last20Losses,
            LpDeltasLast20: lpDeltasLast20,
            WlLast20: wlLast20
        );
    }

    /// <summary>
    /// Calculates LP delta by comparing current rank/LP to the LP of the oldest match
    /// in the last 20 that has LP data. Uses per-match LP from participants table
    /// (populated by LP estimation). Accounts for tier/division changes by converting
    /// to absolute LP values.
    /// </summary>
    private static int CalculateLpDeltaFromMatches(
        string? currentTier,
        string? currentDivision,
        int? currentLp,
        List<MatchResultData> last20Matches,
        ILpCalculationService lpCalc)
    {
        // If no current LP or no matches, we can't calculate delta
        if (currentLp == null || last20Matches.Count == 0)
            return 0;

        // Find the oldest match that has LP data (matches are ordered newest first)
        var oldestMatchWithLp = last20Matches
            .Where(m => m.LpAfter != null && m.TierAfter != null && m.RankAfter != null)
            .OrderBy(m => m.GameStartTime)
            .FirstOrDefault();

        if (oldestMatchWithLp == null)
            return 0;

        // Convert both current and old rank to absolute LP for accurate comparison
        var currentAbsoluteLp = lpCalc.CalculateAbsoluteLp(currentTier, currentDivision, currentLp.Value);
        var oldAbsoluteLp = lpCalc.CalculateAbsoluteLp(oldestMatchWithLp.TierAfter, oldestMatchWithLp.RankAfter, oldestMatchWithLp.LpAfter!.Value);

        return currentAbsoluteLp - oldAbsoluteLp;
    }

    private static LastMatch BuildLastMatch(LastMatchData data)
    {
        var championIconUrl = BuildChampionIconUrl(data.ChampionName);
        var result = data.Win ? "Victory" : "Defeat";
        var kda = $"{data.Kills}/{data.Deaths}/{data.Assists}";
        var queueType = LeagueDataHelper.GetQueueLabel(data.QueueId);

        return new LastMatch(
            MatchId: data.MatchId,
            ChampionIconUrl: championIconUrl,
            ChampionName: data.ChampionName,
            Result: result,
            Kda: kda,
            Timestamp: data.GameStartTime,
            QueueType: queueType
        );
    }

    private static string BuildChampionIconUrl(string championName)
    {
        // Normalize champion name for Data Dragon URL
        var normalized = championName.Replace(" ", "").Replace("'", "");
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/champion/{normalized}.png";
    }
}

