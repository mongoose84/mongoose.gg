using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
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
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] IOverviewStatsRepository overviewStatsRepo,
            [FromServices] ISoloPerformanceRepository soloPerformanceRepo,
            [FromServices] ILogger<OverviewEndpoint> logger
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

                var selectedAccounts = resolvedAccounts!;
                var primaryAccount = selectedAccounts.FirstOrDefault(a => a.IsPrimary)?.Account ?? selectedAccounts[0].Account;
                var primaryPuuid = primaryAccount.Puuid;
                var selectedPuuids = selectedAccounts.Select(a => a.Account.Puuid).ToList();

                // Get all linked accounts for active contexts determination
                var (allAccountsError, allAccounts) = await puuidResolutionService.ResolveAllAccountsAsync(authorizedUser.UserId);
                if (allAccountsError != null)
                    return allAccountsError;

                var linkedAccountsCount = allAccounts?.Count ?? 1;

                logger.LogInformation("Overview request: userId={UserId}, accountCount={AccountCount}, account={Account}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), selectedPuuids.Count, LogSanitizer.HashForLog(accountId, "primary"));

                // Build player header
                var profileIconUrl = BuildProfileIconUrl(primaryAccount.ProfileIconId);
                var activeContexts = DetermineActiveContexts(linkedAccountsCount);
                var playerHeader = new PlayerHeader(
                    SummonerName: primaryAccount.SummonerName,
                    Level: primaryAccount.SummonerLevel ?? 0,
                    Region: primaryAccount.Region.ToUpperInvariant(),
                    ProfileIconUrl: profileIconUrl,
                    ActiveContexts: activeContexts
                );

                // Determine primary queue
                var (primaryQueueId, primaryQueueLabel, _) = await overviewStatsRepo.GetPrimaryQueueAsync(selectedPuuids);

                // Calculate rank snapshot (wlLast20 fields removed)
                var rankSnapshot = BuildRankSnapshot(primaryAccount, primaryQueueId, primaryQueueLabel);

                // Compute rank-adaptive death thresholds from the primary account's solo queue tier
                var (lowDeathThreshold, highDeathThreshold) = DeathThresholds.ForRank(primaryAccount.SoloTier);

                // Parallelize independent data fetches
                var lastMatchTask = overviewStatsRepo.GetLastMatchAsync(selectedPuuids);
                var mostPlayedChampionTask = overviewStatsRepo.GetMostPlayedChampionAsync(selectedPuuids);
                var sessionStatsTask = overviewStatsRepo.GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow);
                var survivalStatsTask = overviewStatsRepo.GetSurvivalStatsAsync(selectedPuuids, lowDeathThreshold, highDeathThreshold);
                await Task.WhenAll(lastMatchTask, mostPlayedChampionTask, sessionStatsTask, survivalStatsTask);

                var lastMatchData = lastMatchTask.Result;
                var lastMatch = lastMatchData != null ? BuildLastMatch(lastMatchData) : null;

                var mostPlayedChampionData = mostPlayedChampionTask.Result;
                var mostPlayedChampion = mostPlayedChampionData != null
                    ? new MostPlayedChampion(
                        ChampionName: mostPlayedChampionData.ChampionName,
                        GamesPlayed: mostPlayedChampionData.GamesPlayed,
                        Source: "current_season")
                    : null;

                var sessionStatsData = sessionStatsTask.Result;
                var survivalStatsData = survivalStatsTask.Result;

                // Active goals (placeholder - no goals table yet, return empty)
                var activeGoals = Array.Empty<GoalPreview>();

                // Suggested actions (placeholder - return empty for now)
                var suggestedActions = Array.Empty<SuggestedAction>();

                AccountSummary[]? accountSummaries = null;
                CombinedStats? combinedStats = null;
                var isAllMode = string.Equals(accountId, "all", StringComparison.OrdinalIgnoreCase);
                if (isAllMode && allAccounts != null)
                {
                    accountSummaries = allAccounts
                        .Select(resolved =>
                        {
                            var perAccountData = sessionStatsData.PerAccount
                                .FirstOrDefault(a => a.Puuid == resolved.Account.Puuid);
                            return new AccountSummary(
                                AccountId: resolved.AccountId,
                                GameName: resolved.Account.GameName,
                                TagLine: resolved.Account.TagLine,
                                Region: resolved.Account.Region,
                                Rank: BuildRankString(resolved.Account),
                                Lp: BuildLpValue(resolved.Account),
                                GamesToday: perAccountData?.GamesToday ?? 0,
                                GamesThisWeek: perAccountData?.GamesThisWeek ?? 0
                            );
                        })
                        .ToArray();

                    var aggregatePerformance = await soloPerformanceRepo.GetSoloPerformanceAsync(selectedPuuids, "all", null);
                    if (aggregatePerformance != null)
                    {
                        combinedStats = new CombinedStats(
                            TotalGames: aggregatePerformance.GamesPlayed,
                            WinRate: aggregatePerformance.WinRate,
                            AvgKda: aggregatePerformance.AvgKda
                        );
                    }
                }

                var sessionStats = BuildSessionStats(sessionStatsData);
                var survivalStats = new SurvivalStats(
                    AvgDeathsPerGame: survivalStatsData.AvgDeathsPerGame,
                    WinRateLowDeaths: survivalStatsData.WinRateLowDeaths,
                    WinRateHighDeaths: survivalStatsData.WinRateHighDeaths,
                    GamesLowDeaths: survivalStatsData.GamesLowDeaths,
                    GamesHighDeaths: survivalStatsData.GamesHighDeaths,
                    LowDeathThreshold: lowDeathThreshold,
                    HighDeathThreshold: highDeathThreshold,
                    TotalGames: survivalStatsData.TotalGames
                );

                var response = new OverviewResponse(
                    PlayerHeader: playerHeader,
                    RankSnapshot: rankSnapshot,
                    LastMatch: lastMatch,
                    MostPlayedChampion: mostPlayedChampion,
                    ActiveGoals: activeGoals,
                    SuggestedActions: suggestedActions,
                    AccountSummaries: accountSummaries,
                    CombinedStats: combinedStats,
                    SessionStats: sessionStats,
                    SurvivalStats: survivalStats
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Overview: unhandled error for userId {UserId}", LogSanitizer.Sanitize(userId));
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
        string primaryQueueLabel)
    {
        string? rank = null;
        int? currentLp = null;

        if (primaryQueueId == 420) // Ranked Solo/Duo
        {
            if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
            {
                rank = $"{account.SoloTier} {account.SoloRank}";
                currentLp = account.SoloLp;
            }
        }
        else if (primaryQueueId == 440) // Ranked Flex
        {
            if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
            {
                rank = $"{account.FlexTier} {account.FlexRank}";
                currentLp = account.FlexLp;
            }
        }

        return new RankSnapshot(
            PrimaryQueueLabel: primaryQueueLabel,
            Rank: rank,
            Lp: currentLp
        );
    }

    private static SessionStats BuildSessionStats(SessionStatsData data)
    {
        var perAccount = data.PerAccount;
        var totalGamesToday = perAccount.Sum(a => a.GamesToday);
        var totalWinsToday = perAccount.Sum(a => a.WinsToday);
        var totalLossesToday = perAccount.Sum(a => a.LossesToday);

        double? avgKdaToday = null;
        if (totalGamesToday > 0)
        {
            var weightedSum = perAccount
                .Where(a => a.AvgKdaToday.HasValue && a.GamesToday > 0)
                .Sum(a => a.AvgKdaToday!.Value * a.GamesToday);
            avgKdaToday = weightedSum / totalGamesToday;
        }

        SessionChampion? bestChampionToday = null;
        var bestChamp = perAccount
            .Where(a => a.BestChampionName != null)
            .OrderByDescending(a => a.BestChampionWins + a.BestChampionLosses > 0
                ? (double)a.BestChampionWins / (a.BestChampionWins + a.BestChampionLosses)
                : 0.0)
            .ThenByDescending(a => a.BestChampionAvgKda)
            .FirstOrDefault();
        if (bestChamp != null)
        {
            bestChampionToday = new SessionChampion(
                ChampionName: bestChamp.BestChampionName!,
                Wins: bestChamp.BestChampionWins,
                Losses: bestChamp.BestChampionLosses,
                AvgKda: bestChamp.BestChampionAvgKda
            );
        }

        var totalGamesThisWeek = perAccount.Sum(a => a.GamesThisWeek);
        var totalWinsThisWeek = perAccount.Sum(a => a.WinsThisWeek);
        var totalLossesThisWeek = perAccount.Sum(a => a.LossesThisWeek);

        double? avgKdaThisWeek = null;
        if (totalGamesThisWeek > 0)
        {
            var weightedSum = perAccount
                .Where(a => a.AvgKdaThisWeek.HasValue && a.GamesThisWeek > 0)
                .Sum(a => a.AvgKdaThisWeek!.Value * a.GamesThisWeek);
            avgKdaThisWeek = weightedSum / totalGamesThisWeek;
        }

        return new SessionStats(
            GamesToday: totalGamesToday,
            WinsToday: totalWinsToday,
            LossesToday: totalLossesToday,
            AvgKdaToday: avgKdaToday,
            BestChampionToday: bestChampionToday,
            GamesThisWeek: totalGamesThisWeek,
            WinsThisWeek: totalWinsThisWeek,
            LossesThisWeek: totalLossesThisWeek,
            AvgKdaThisWeek: avgKdaThisWeek
        );
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

    private static string? BuildRankString(Mongoose.Api.Core.Entities.RiotAccount account)
    {
        if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
        {
            return $"{account.SoloTier} {account.SoloRank}";
        }

        if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
        {
            return $"{account.FlexTier} {account.FlexRank}";
        }

        return null;
    }

    private static int? BuildLpValue(Mongoose.Api.Core.Entities.RiotAccount account)
    {
        if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
        {
            return account.SoloLp;
        }

        if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
        {
            return account.FlexLp;
        }

        return null;
    }
}

