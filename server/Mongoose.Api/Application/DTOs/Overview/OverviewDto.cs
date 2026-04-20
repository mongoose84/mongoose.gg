using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// Overview endpoint response containing aggregated dashboard data
/// </summary>
public record OverviewResponse(
    [property: JsonPropertyName("playerHeader")] PlayerHeader PlayerHeader,
    [property: JsonPropertyName("lastMatch")] LastMatch? LastMatch,
    [property: JsonPropertyName("mostPlayedChampion")] MostPlayedChampion? MostPlayedChampion,
    [property: JsonPropertyName("activeGoals")] GoalPreview[] ActiveGoals,
    [property: JsonPropertyName("suggestedActions")] SuggestedAction[] SuggestedActions,
    [property: JsonPropertyName("accountSummaries")] AccountSummary[]? AccountSummaries = null,
    [property: JsonPropertyName("combinedStats")] CombinedStats? CombinedStats = null,
    [property: JsonPropertyName("sessionStats")] SessionStats? SessionStats = null,
    [property: JsonPropertyName("survivalStats")] SurvivalStats? SurvivalStats = null
);

public record AccountSummary(
    [property: JsonPropertyName("accountId")] string AccountId,
    [property: JsonPropertyName("gameName")] string GameName,
    [property: JsonPropertyName("tagLine")] string TagLine,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("rank")] string? Rank,
    [property: JsonPropertyName("lp")] int? Lp,
    [property: JsonPropertyName("gamesToday")] int GamesToday,
    [property: JsonPropertyName("gamesThisWeek")] int GamesThisWeek
);

public record CombinedStats(
    [property: JsonPropertyName("totalGames")] int TotalGames,
    [property: JsonPropertyName("winRate")] double WinRate,
    [property: JsonPropertyName("avgKda")] double AvgKda
);

/// <summary>
/// Player header data: profile info and active contexts
/// </summary>
public record PlayerHeader(
    [property: JsonPropertyName("summonerName")] string SummonerName,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("profileIconUrl")] string ProfileIconUrl,
    [property: JsonPropertyName("activeContexts")] string[] ActiveContexts,
    [property: JsonPropertyName("rank")] string? Rank = null,
    [property: JsonPropertyName("lp")] int? Lp = null,
    [property: JsonPropertyName("primaryQueueLabel")] string? PrimaryQueueLabel = null
);

/// <summary>
/// Last match summary
/// </summary>
public record LastMatch(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("championIconUrl")] string ChampionIconUrl,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("kda")] string Kda,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("queueType")] string QueueType
);

/// <summary>
/// Most played champion in the current season window for CTA personalization.
/// </summary>
public record MostPlayedChampion(
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
    [property: JsonPropertyName("source")] string Source
);

/// <summary>
/// Goal preview for the overview page (max 3)
/// </summary>
public record GoalPreview(
    [property: JsonPropertyName("goalId")] string GoalId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("context")] string Context,
    [property: JsonPropertyName("progress")] double Progress
);

/// <summary>
/// Suggested action for the overview page (max 3)
/// </summary>
public record SuggestedAction(
    [property: JsonPropertyName("actionId")] string ActionId,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("deepLink")] string DeepLink,
    [property: JsonPropertyName("priority")] int Priority
);

/// <summary>
/// Session stats aggregated across all selected accounts for today and this week.
/// </summary>
public record SessionStats(
    [property: JsonPropertyName("gamesToday")] int GamesToday,
    [property: JsonPropertyName("winsToday")] int WinsToday,
    [property: JsonPropertyName("lossesToday")] int LossesToday,
    [property: JsonPropertyName("avgKdaToday")] double? AvgKdaToday,
    [property: JsonPropertyName("bestChampionToday")] SessionChampion? BestChampionToday,
    [property: JsonPropertyName("gamesThisWeek")] int GamesThisWeek,
    [property: JsonPropertyName("winsThisWeek")] int WinsThisWeek,
    [property: JsonPropertyName("lossesThisWeek")] int LossesThisWeek,
    [property: JsonPropertyName("avgKdaThisWeek")] double? AvgKdaThisWeek
);

/// <summary>
/// Best-performing champion in today's session.
/// </summary>
public record SessionChampion(
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("wins")] int Wins,
    [property: JsonPropertyName("losses")] int Losses,
    [property: JsonPropertyName("avgKda")] double AvgKda
);

/// <summary>
/// Survival statistics derived from the last 20 games across all selected accounts.
/// Death buckets are rank-adaptive: thresholds are resolved server-side and returned
/// so the frontend can display them without duplicating rank logic.
/// </summary>
public record SurvivalStats(
    [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame,
    [property: JsonPropertyName("winRateLowDeaths")] double? WinRateLowDeaths,
    [property: JsonPropertyName("winRateHighDeaths")] double? WinRateHighDeaths,
    [property: JsonPropertyName("gamesLowDeaths")] int GamesLowDeaths,
    [property: JsonPropertyName("gamesHighDeaths")] int GamesHighDeaths,
    [property: JsonPropertyName("lowDeathThreshold")] int LowDeathThreshold,
    [property: JsonPropertyName("highDeathThreshold")] int HighDeathThreshold,
    [property: JsonPropertyName("totalGames")] int TotalGames
);
