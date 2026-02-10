namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for champion select queries.
/// These are returned by repositories and mapped to DTOs in endpoints.
/// </summary>

/// <summary>
/// Champion select data containing main champions and basic stats.
/// </summary>
public record ChampionSelectData(
    MainChampionRoleGroupData[] MainChampions,
    int GamesPlayed,
    double WinRate
);

