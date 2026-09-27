using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.Services;
using Mongoose.Api.Infrastructure.Helpers;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.Endpoints.Overview;

/// <summary>
/// Overview Endpoint
/// Returns aggregated dashboard data for the Overview page.
/// Includes player header, last match, most-played champion, champion pool, session and survival stats.
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

                // All linked accounts, for the per-account summaries in Overall mode
                var (allAccountsError, allAccounts) = await puuidResolutionService.ResolveAllAccountsAsync(authorizedUser.UserId);
                if (allAccountsError != null)
                    return allAccountsError;

                logger.LogInformation("Overview request: userId={UserId}, accountCount={AccountCount}, account={Account}",
                    LogSanitizer.Sanitize(authorizedUser.UserId.ToString()), selectedPuuids.Count, LogSanitizer.HashForLog(accountId, "primary"));

                // Build player header
                var (primaryRank, primaryLp) = ResolveRankedQueue(primaryAccount);
                var playerHeader = new PlayerHeader(
                    SummonerName: primaryAccount.SummonerName,
                    Level: primaryAccount.SummonerLevel ?? 0,
                    Region: primaryAccount.Region.ToUpperInvariant(),
                    Rank: primaryRank,
                    Lp: primaryLp
                );

                // Compute rank-adaptive death thresholds from the primary account's solo queue tier
                var (lowDeathThreshold, highDeathThreshold) = DeathThresholds.ForRank(primaryAccount.SoloTier);

                // Parallelize independent data fetches
                var lastMatchTask = overviewStatsRepo.GetLastMatchAsync(selectedPuuids);
                var mostPlayedChampionTask = overviewStatsRepo.GetMostPlayedChampionAsync(selectedPuuids);
                var sessionStatsTask = overviewStatsRepo.GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow);
                var survivalStatsTask = overviewStatsRepo.GetSurvivalStatsAsync(selectedPuuids, lowDeathThreshold, highDeathThreshold);
                var championPoolTask = overviewStatsRepo.GetChampionPoolStatsAsync(selectedPuuids);
                await Task.WhenAll(lastMatchTask, mostPlayedChampionTask, sessionStatsTask, survivalStatsTask, championPoolTask);

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

                AccountSummary[]? accountSummaries = null;
                var isAllMode = string.Equals(accountId, "all", StringComparison.OrdinalIgnoreCase);
                if (isAllMode && allAccounts != null)
                {
                    accountSummaries = allAccounts
                        .Select(resolved =>
                        {
                            var perAccountData = sessionStatsData.PerAccount
                                .FirstOrDefault(a => a.Puuid == resolved.Account.Puuid);
                            var (acctRank, acctLp) = ResolveRankedQueue(resolved.Account);
                            return new AccountSummary(
                                AccountId: resolved.AccountId,
                                GameName: resolved.Account.GameName,
                                TagLine: resolved.Account.TagLine,
                                Region: resolved.Account.Region,
                                Rank: acctRank,
                                Lp: acctLp,
                                GamesToday: perAccountData?.GamesToday ?? 0,
                                GamesThisWeek: perAccountData?.GamesThisWeek ?? 0
                            );
                        })
                        .ToArray();
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
                    LastMatch: lastMatch,
                    MostPlayedChampion: mostPlayedChampion,
                    AccountSummaries: accountSummaries,
                    SessionStats: sessionStats,
                    SurvivalStats: survivalStats,
                    ChampionPool: BuildChampionPool(championPoolTask.Result)
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

    private static ChampionPool BuildChampionPool(ChampionPoolData data)
    {
        var pool = ChampionPoolBuilder.Build(data.Champions, data.RoleCounts);

        static PoolChampion ToDto(ChampionPoolBuilder.PoolEntry e) => new(
            ChampionId: e.ChampionId,
            ChampionName: e.ChampionName,
            Role: e.Role,
            Matches: e.Matches,
            Wins: e.Wins,
            WinRate: e.WinRate,
            AvgKda: e.AvgKda,
            MScore: e.MScore,
            StrengthTag: e.StrengthTag);

        return new ChampionPool(
            Champions: pool.Champions.Select(ToDto).ToArray(),
            AlsoPlayed: pool.AlsoPlayed.Select(ToDto).ToArray());
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

    private static (string? Rank, int? Lp) ResolveRankedQueue(Mongoose.Api.Core.Entities.RiotAccount account)
    {
        if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
            return ($"{account.SoloTier} {account.SoloRank}", account.SoloLp);

        if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
            return ($"{account.FlexTier} {account.FlexRank}", account.FlexLp);

        return (null, null);
    }
}
