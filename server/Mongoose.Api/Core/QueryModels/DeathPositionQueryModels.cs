namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for death position queries (danger zone heatmap).
/// </summary>
/// 
public record DeathPositionData(
    int X,
    int Y,
    int MinuteMark,
    string Phase,
    int? KillerChampionId,
    int AssistCount,
    string MatchId
);

public record DeathPositionPhaseSummary(
    int Early,
    int Mid,
    int Late,
    int VeryLate
);

public record DeathPositionsResult(
    DeathPositionData[] Deaths,
    int TotalDeaths,
    int MatchesAnalyzed,
    DeathPositionPhaseSummary PhaseSummary
);
