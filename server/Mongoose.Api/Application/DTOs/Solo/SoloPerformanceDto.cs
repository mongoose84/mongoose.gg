using System.Text.Json.Serialization;
using static Mongoose.Api.Application.DTOs.MainChampionDto;

namespace Mongoose.Api.Application.DTOs;

public static class SoloPerformanceDto
{
    /// <summary>
    /// Comprehensive solo performance response containing all required stats
    /// </summary>
    public record SoloPerformanceResponse(
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda,
        [property: JsonPropertyName("avgGameDurationMinutes")] double AvgGameDurationMinutes,

        // K/D/A breakdown for summary stats card (filtered by time range)
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("avgAssists")] double AvgAssists,

        // Overall stats (all-time, for tooltip comparison)
        [property: JsonPropertyName("overallWinRate")] double OverallWinRate,
        [property: JsonPropertyName("overallAvgKills")] double OverallAvgKills,
        [property: JsonPropertyName("overallAvgDeaths")] double OverallAvgDeaths,
        [property: JsonPropertyName("overallAvgAssists")] double OverallAvgAssists,
        [property: JsonPropertyName("overallAvgKda")] double OverallAvgKda,

        // Side statistics (win distribution)
        [property: JsonPropertyName("sideStats")] SideWinDistribution SideStats,

        // Champion pool summary
        [property: JsonPropertyName("uniqueChampsPlayedCount")] int UniqueChampsPlayedCount,
        [property: JsonPropertyName("mainChampion")] ChampionSummary? MainChampion,
        [property: JsonPropertyName("mainChampions")] MainChampionRoleGroup[] MainChampions,

        // Recent trend
        [property: JsonPropertyName("last10Games")] TrendMetric? Last10Games,
        [property: JsonPropertyName("last20Games")] TrendMetric? Last20Games,

        // Performance by phase
        [property: JsonPropertyName("performanceByPhase")] PerformancePhase[] PerformanceByPhase,

        // Role breakdown
        [property: JsonPropertyName("roleBreakdown")] RolePerformance[] RoleBreakdown,

        // Death efficiency
        [property: JsonPropertyName("deathEfficiency")] DeathEfficiency DeathEfficiency,

        // Queue type
        [property: JsonPropertyName("queueType")] string QueueType
    );

    public record SideWinDistribution(
        [property: JsonPropertyName("blueWins")] int BlueWins,
        [property: JsonPropertyName("redWins")] int RedWins,
        [property: JsonPropertyName("blueGames")] int BlueGames,
        [property: JsonPropertyName("redGames")] int RedGames,
        [property: JsonPropertyName("totalGames")] int TotalGames,
        [property: JsonPropertyName("blueWinDistribution")] double BlueWinDistribution,
        [property: JsonPropertyName("redWinDistribution")] double RedWinDistribution
    );

    public record ChampionSummary(
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("picks")] int Picks,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("pickRate")] double PickRate
    );

    public record TrendMetric(
        [property: JsonPropertyName("games")] int Games,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda,
        // K/D/A breakdown for trend comparison
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("avgAssists")] double AvgAssists
    );

    public record PerformancePhase(
        [property: JsonPropertyName("phase")] string Phase,
        [property: JsonPropertyName("games")] int Games,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda,
        [property: JsonPropertyName("avgGoldPerMin")] double AvgGoldPerMin,
        [property: JsonPropertyName("avgDamagePerMin")] double AvgDamagePerMin
    );

    public record RolePerformance(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda
    );

    public record DeathEfficiency(
        [property: JsonPropertyName("deathsPre10")] int DeathsPre10,
        [property: JsonPropertyName("deaths10To20")] int Deaths10To20,
        [property: JsonPropertyName("deaths20To30")] int Deaths20To30,
        [property: JsonPropertyName("deaths30Plus")] int Deaths30Plus,
        [property: JsonPropertyName("avgFirstDeathMinute")] double? AvgFirstDeathMinute,
        [property: JsonPropertyName("avgFirstKillParticipationMinute")] double? AvgFirstKillParticipationMinute
    );
}

