namespace Mongoose.Api.Core.QueryModels;

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
    long GameStartTime,
    int QueueId
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

/// <summary>
/// Internal DTO for most played champion aggregation.
/// </summary>
public record MostPlayedChampionData(
    string ChampionName,
    int GamesPlayed
);

/// <summary>
/// Per-PUUID session breakdown. The repository returns one entry per PUUID so the
/// endpoint can populate both the aggregate SessionStats DTO and per-account
/// AccountSummary.GamesToday / GamesThisWeek fields in a single query.
/// </summary>
public record PerAccountSessionData(
    string Puuid,
    int GamesToday,
    int WinsToday,
    int LossesToday,
    double? AvgKdaToday,
    string? BestChampionName,
    int BestChampionWins,
    int BestChampionLosses,
    double BestChampionAvgKda,
    int GamesThisWeek,
    int WinsThisWeek,
    int LossesThisWeek,
    double? AvgKdaThisWeek
);

/// <summary>
/// Aggregate session stats across all requested PUUIDs.
/// Built by the endpoint from the per-account breakdown.
/// </summary>
public record SessionStatsData(
    IReadOnlyList<PerAccountSessionData> PerAccount
);

/// <summary>
/// Survival analysis over the last N games.
/// </summary>
public record SurvivalStatsData(
    double AvgDeathsPerGame,
    double DeathsBefore10Pct,
    double? WinRateAtOrBelow3Deaths,
    double? WinRateAbove5Deaths,
    int GamesAtOrBelow3Deaths,
    int GamesAbove5Deaths,
    int TotalGames
);

