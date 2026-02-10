namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for champion select queries.
/// NOTE: These models are not currently in use. Repositories still return Application DTOs directly.
/// These were created as part of a planned Clean Architecture refactoring (Phase 3) that was not completed.
/// To use these, repositories would need to be updated to return these Core types,
/// and endpoints would map them to Application DTOs.
/// </summary>

/// <summary>
/// Champion select data containing main champions and basic stats.
/// </summary>
public record ChampionSelectData(
    MainChampionRoleGroupData[] MainChampions,
    int GamesPlayed,
    double WinRate
);

