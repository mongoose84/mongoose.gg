using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs.Overview;

/// <summary>
/// Overview endpoint response containing aggregated dashboard data
/// </summary>
public record OverviewResponse(
    [property: JsonPropertyName("playerHeader")] PlayerHeader PlayerHeader,
    [property: JsonPropertyName("rankSnapshot")] RankSnapshot RankSnapshot,
    [property: JsonPropertyName("lastMatch")] LastMatch? LastMatch,
    [property: JsonPropertyName("activeGoals")] GoalPreview[] ActiveGoals,
    [property: JsonPropertyName("suggestedActions")] SuggestedAction[] SuggestedActions
);

/// <summary>
/// Player header data: profile info and active contexts
/// </summary>
public record PlayerHeader(
    [property: JsonPropertyName("summonerName")] string SummonerName,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("profileIconUrl")] string ProfileIconUrl,
    [property: JsonPropertyName("activeContexts")] string[] ActiveContexts
);

/// <summary>
/// Rank snapshot for the primary queue with last 20 games metrics
/// </summary>
public record RankSnapshot(
    [property: JsonPropertyName("primaryQueueLabel")] string PrimaryQueueLabel,
    [property: JsonPropertyName("rank")] string? Rank,
    [property: JsonPropertyName("lp")] int? Lp,
    [property: JsonPropertyName("lpDeltaLast20")] int LpDeltaLast20,
    [property: JsonPropertyName("last20Wins")] int Last20Wins,
    [property: JsonPropertyName("last20Losses")] int Last20Losses,
    [property: JsonPropertyName("lpDeltasLast20")] int[] LpDeltasLast20,
    [property: JsonPropertyName("wlLast20")] bool[] WlLast20
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
    [property: JsonPropertyName("timestamp")] long Timestamp
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
/// Internal DTO for queue count data
/// </summary>
public record QueueMatchCount(
    int QueueId,
    int MatchCount
);

/// <summary>
/// Internal DTO for last match data from DB
/// </summary>
public record LastMatchData(
    string MatchId,
    int ChampionId,
    string ChampionName,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    long GameStartTime
);

/// <summary>
/// Internal DTO for match result in last 20
/// </summary>
public record MatchResultData(
    string MatchId,
    bool Win,
    int? LpAfter,
    long GameStartTime
);

