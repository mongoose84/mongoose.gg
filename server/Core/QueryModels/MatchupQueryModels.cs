namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for champion matchup queries.
/// NOTE: These models are not currently in use. Repositories still return Application DTOs directly.
/// These were created as part of a planned Clean Architecture refactoring (Phase 3) that was not completed.
/// To use these, repositories would need to be updated to return these Core types,
/// and endpoints would map them to Application DTOs.
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

