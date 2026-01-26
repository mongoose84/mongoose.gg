using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Overview;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Overview;

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
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] OverviewStatsRepository overviewStatsRepo,
            [FromServices] ILogger<OverviewEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Overview: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Get riot accounts for the user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);
                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Overview: no Riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No linked Riot accounts found" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                logger.LogInformation("Overview request: userId={UserId}, puuid={Puuid}", userIdInt, primaryPuuid);

                // Build player header
                var profileIconUrl = BuildProfileIconUrl(primaryAccount.ProfileIconId);
                var activeContexts = DetermineActiveContexts(riotAccounts.Count);
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

                // Calculate rank snapshot
                var rankSnapshot = BuildRankSnapshot(
                    primaryAccount,
                    primaryQueueId,
                    primaryQueueLabel,
                    last20Matches
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
        RiotProxy.External.Domain.Entities.RiotAccount account,
        int primaryQueueId,
        string primaryQueueLabel,
        List<MatchResultData> last20Matches)
    {
        // Get current rank based on primary queue
        string? rank = null;
        int? lp = null;

        if (primaryQueueId == 420) // Ranked Solo/Duo
        {
            if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
            {
                rank = $"{account.SoloTier} {account.SoloRank}";
                lp = account.SoloLp;
            }
        }
        else if (primaryQueueId == 440) // Ranked Flex
        {
            if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
            {
                rank = $"{account.FlexTier} {account.FlexRank}";
                lp = account.FlexLp;
            }
        }

        // Calculate wins and losses from last 20
        var last20Wins = last20Matches.Count(m => m.Win);
        var last20Losses = last20Matches.Count(m => !m.Win);

        // Build W/L array (newest first, true = win, false = loss)
        var wlLast20 = last20Matches.Select(m => m.Win).ToArray();

        // Calculate LP deltas for last 20 matches
        // We need to compute deltas between consecutive matches
        var lpDeltasLast20 = CalculateLpDeltas(last20Matches);
        var lpDeltaLast20 = lpDeltasLast20.Sum();

        return new RankSnapshot(
            PrimaryQueueLabel: primaryQueueLabel,
            Rank: rank,
            Lp: lp,
            LpDeltaLast20: lpDeltaLast20,
            Last20Wins: last20Wins,
            Last20Losses: last20Losses,
            LpDeltasLast20: lpDeltasLast20,
            WlLast20: wlLast20
        );
    }

    private static int[] CalculateLpDeltas(List<MatchResultData> matches)
    {
        // Matches are ordered newest first
        // We need LP after each match to calculate deltas
        // Delta = LP after this match - LP after previous match

        if (matches.Count == 0)
            return Array.Empty<int>();

        var deltas = new List<int>();

        // Reverse to process oldest to newest for delta calculation
        var orderedMatches = matches.OrderBy(m => m.GameStartTime).ToList();

        for (int i = 0; i < orderedMatches.Count; i++)
        {
            var currentLp = orderedMatches[i].LpAfter;

            if (i == 0 || currentLp == null)
            {
                // First match or no LP data - assume 0 delta
                deltas.Add(0);
            }
            else
            {
                var previousLp = orderedMatches[i - 1].LpAfter;
                if (previousLp != null)
                {
                    // Simple delta calculation
                    // Note: This doesn't handle rank-ups/downs correctly (e.g., 99 LP -> 15 LP on win)
                    // For now, use a heuristic: if win and delta is negative, assume rank-up
                    var delta = currentLp.Value - previousLp.Value;

                    if (orderedMatches[i].Win && delta < -50)
                    {
                        // Likely a rank-up: assume gained ~20 LP
                        delta = 100 - previousLp.Value + currentLp.Value;
                    }
                    else if (!orderedMatches[i].Win && delta > 50)
                    {
                        // Likely a rank-down: assume lost ~15 LP
                        delta = -(previousLp.Value + (100 - currentLp.Value));
                    }

                    deltas.Add(delta);
                }
                else
                {
                    deltas.Add(0);
                }
            }
        }

        // Return in newest-first order to match wlLast20
        deltas.Reverse();
        return deltas.ToArray();
    }

    private static LastMatch BuildLastMatch(LastMatchData data)
    {
        var championIconUrl = BuildChampionIconUrl(data.ChampionName);
        var result = data.Win ? "Victory" : "Defeat";
        var kda = $"{data.Kills}/{data.Deaths}/{data.Assists}";
        var queueType = OverviewStatsRepository.GetQueueLabel(data.QueueId);

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

