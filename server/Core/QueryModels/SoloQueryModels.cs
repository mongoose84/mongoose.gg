namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for solo performance queries.
/// These are returned by repositories and mapped to DTOs in endpoints.
/// </summary>

/// <summary>
/// Comprehensive solo performance data containing all required stats.
/// </summary>
public record SoloPerformanceData(
    int GamesPlayed,
    int Wins,
    double WinRate,
    double AvgKda,
    double AvgGameDurationMinutes,
    double AvgKills,
    double AvgDeaths,
    double AvgAssists,
    double OverallWinRate,
    double OverallAvgKills,
    double OverallAvgDeaths,
    double OverallAvgAssists,
    double OverallAvgKda,
    SideWinDistributionData SideStats,
    int UniqueChampsPlayedCount,
    ChampionSummaryData? MainChampion,
    MainChampionRoleGroupData[] MainChampions,
    TrendMetricData? Last10Games,
    TrendMetricData? Last20Games,
    PerformancePhaseData[] PerformanceByPhase,
    RolePerformanceData[] RoleBreakdown,
    DeathEfficiencyData DeathEfficiency,
    string QueueType,
    LpTrendData[] LpTrend
);

public record SideWinDistributionData(
    int BlueWins,
    int RedWins,
    int BlueGames,
    int RedGames,
    int TotalGames,
    double BlueWinDistribution,
    double RedWinDistribution
);

public record ChampionSummaryData(
    int ChampionId,
    string ChampionName,
    int Picks,
    double WinRate,
    double PickRate
);

public record TrendMetricData(
    int Games,
    int Wins,
    double WinRate,
    double AvgKda,
    double AvgKills,
    double AvgDeaths,
    double AvgAssists
);

public record PerformancePhaseData(
    string Phase,
    int Games,
    int Wins,
    double WinRate,
    double AvgKda,
    double AvgGoldPerMin,
    double AvgDamagePerMin
);

public record RolePerformanceData(
    string Role,
    int GamesPlayed,
    int Wins,
    double WinRate,
    double AvgKda
);

public record DeathEfficiencyData(
    int DeathsPre10,
    int Deaths10To20,
    int Deaths20To30,
    int Deaths30Plus,
    double? AvgFirstDeathMinute,
    double? AvgFirstKillParticipationMinute
);

/// <summary>
/// Main champion recommendation grouped by role.
/// </summary>
public record MainChampionRoleGroupData(
    string Role,
    MainChampionEntryData[] Champions
);

/// <summary>
/// Individual champion entry in main champion recommendations.
/// </summary>
public record MainChampionEntryData(
    string ChampionName,
    int ChampionId,
    string Role,
    double WinRate,
    int GamesPlayed,
    int Wins,
    int Losses,
    double MScore
);

