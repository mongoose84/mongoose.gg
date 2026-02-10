namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for champion matchup queries.
/// These are returned by repositories and mapped to DTOs in endpoints.
/// </summary>

/// <summary>
/// Response containing champion matchups data.
/// </summary>
public record ChampionMatchupsData(
    ChampionMatchupData[] Matchups,
    string QueueType,
    string TimeRange
);

/// <summary>
/// Matchup data for a specific champion the player uses.
/// </summary>
public record ChampionMatchupData(
    int ChampionId,
    string ChampionName,
    string Role,
    int TotalGames,
    int Wins,
    double WinRate,
    OpponentMatchupData[] Opponents
);

/// <summary>
/// Matchup data against a specific opponent champion.
/// Contains in-lane and out-of-lane win/loss counts.
/// </summary>
public record OpponentMatchupData(
    int OpponentChampionId,
    string OpponentChampionName,
    int InLaneWins,
    int InLaneLosses,
    int OutOfLaneWins,
    int OutOfLaneLosses
);

