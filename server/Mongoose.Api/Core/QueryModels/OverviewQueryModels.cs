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

