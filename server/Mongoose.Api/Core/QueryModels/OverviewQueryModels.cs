namespace Mongoose.Api.Core.QueryModels;

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
/// Internal DTO for most played champion aggregation.
/// </summary>
public record MostPlayedChampionData(
    string ChampionName,
    int GamesPlayed
);

/// <summary>
/// Per-champion ranked aggregate for the Overview champion pool (current season).
/// Nullable averages are null when no match of the champion has that metric;
/// the sample counts say how many matches each average is based on.
/// </summary>
public record ChampionPoolStatsData(
    int ChampionId,
    string ChampionName,
    int Games,
    int Wins,
    double AvgKills,
    double AvgDeaths,
    double AvgAssists,
    double AvgCsPerMin,
    double? AvgGoldDiff15,
    int GoldDiff15Samples,
    double? AvgDeathsPre10,
    double? AvgVisionPerMin,
    double? AvgDamageSharePct,
    double? AvgKillParticipationPct,
    int MetricSamples,
    long LastPlayed
);

/// <summary>
/// Matches per champion and role, used to find each champion's primary role.
/// </summary>
public record ChampionRoleCountData(
    int ChampionId,
    string Role,
    int Games,
    long LastPlayed
);

/// <summary>
/// Raw champion pool data: per-champion aggregates and per-role match counts.
/// </summary>
public record ChampionPoolData(
    IReadOnlyList<ChampionPoolStatsData> Champions,
    IReadOnlyList<ChampionRoleCountData> RoleCounts
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
/// Bucket boundaries are determined by rank-adaptive thresholds passed to the repository.
/// </summary>
public record SurvivalStatsData(
    double AvgDeathsPerGame,
    double? WinRateLowDeaths,
    double? WinRateHighDeaths,
    int GamesLowDeaths,
    int GamesHighDeaths,
    int TotalGames
);

