using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class SoloSummaryDto
{
    /// <summary>
    /// Comprehensive solo dashboard response containing all required stats
    /// </summary>
    public record SoloDashboardResponse(
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda,
        [property: JsonPropertyName("avgGameDurationMinutes")] double AvgGameDurationMinutes,

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
        [property: JsonPropertyName("queueType")] string QueueType,

        // LP trend (for ranked queues only, empty array if no LP data or non-ranked queue)
        [property: JsonPropertyName("lpTrend")] LpTrendPoint[] LpTrend
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

	    public record MainChampionRoleGroup(
	        [property: JsonPropertyName("role")] string Role,
	        [property: JsonPropertyName("champions")] MainChampionEntry[] Champions
	    );

	    public record MainChampionEntry(
	        [property: JsonPropertyName("championName")] string ChampionName,
	        [property: JsonPropertyName("championId")] int ChampionId,
	        [property: JsonPropertyName("role")] string Role,
	        [property: JsonPropertyName("winRate")] double WinRate,
	        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
	        [property: JsonPropertyName("wins")] int Wins,
	        [property: JsonPropertyName("losses")] int Losses,
	        [property: JsonPropertyName("lpPerGame")] double LpPerGame
	    );

    public record TrendMetric(
        [property: JsonPropertyName("games")] int Games,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKda")] double AvgKda
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

    /// <summary>
    /// A single data point for the winrate trend chart.
    /// Represents the rolling average winrate at a specific game in the timeline.
    /// </summary>
    public record WinrateTrendPoint(
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp
    );

    /// <summary>
    /// A single data point for the LP trend chart.
    /// Represents LP and rank after each ranked game.
    /// </summary>
    public record LpTrendPoint(
        /// <summary>1-indexed game number, oldest to newest</summary>
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        /// <summary>LP gained or lost in this game (null if unknown - first game or missing data)</summary>
        [property: JsonPropertyName("lpGain")] int? LpGain,
        /// <summary>Current LP after this game</summary>
        [property: JsonPropertyName("currentLp")] int CurrentLp,
        /// <summary>Rank string after this game (e.g., "Silver IV")</summary>
        [property: JsonPropertyName("rank")] string Rank,
        /// <summary>Timestamp of the game</summary>
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        /// <summary>True if this game resulted in a promotion (rank changed up)</summary>
        [property: JsonPropertyName("isPromotion")] bool IsPromotion,
        /// <summary>True if this game resulted in a demotion (rank changed down)</summary>
        [property: JsonPropertyName("isDemotion")] bool IsDemotion,
        /// <summary>True if the player won this game</summary>
        [property: JsonPropertyName("win")] bool Win
    );
}
