using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class DuoTrendAnalysisDto
{
    // ========================
    // Win Rate Trend
    // ========================
    
    public record DuoWinRateTrendResponse(
        [property: JsonPropertyName("dataPoints")] IList<DuoWinRateTrendDataPoint> DataPoints,
        [property: JsonPropertyName("overallWinRate")] double OverallWinRate,
        [property: JsonPropertyName("recentWinRate")] double RecentWinRate,
        [property: JsonPropertyName("trendDirection")] string TrendDirection,
        [property: JsonPropertyName("totalGames")] int TotalGames,
        [property: JsonPropertyName("totalWins")] int TotalWins
    );

    public record DuoWinRateTrendDataPoint(
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        [property: JsonPropertyName("win")] bool Win,
        [property: JsonPropertyName("rollingWinRate")] double RollingWinRate,
        [property: JsonPropertyName("gameDate")] DateTime GameDate
    );

    // ========================
    // Performance Radar
    // ========================
    
    public record DuoPerformanceRadarResponse(
        [property: JsonPropertyName("metrics")] DuoRadarMetrics Metrics,
        [property: JsonPropertyName("normalized")] DuoRadarNormalized Normalized,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed
    );

    public record DuoRadarMetrics(
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("avgAssists")] double AvgAssists,
        [property: JsonPropertyName("avgCs")] double AvgCs,
        [property: JsonPropertyName("avgGoldEarned")] double AvgGoldEarned,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    public record DuoRadarNormalized(
        [property: JsonPropertyName("kills")] double Kills,
        [property: JsonPropertyName("survival")] double Survival,
        [property: JsonPropertyName("assists")] double Assists,
        [property: JsonPropertyName("farming")] double Farming,
        [property: JsonPropertyName("gold")] double Gold,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    // ========================
    // Streak Tracker
    // ========================
    
    public record DuoStreakResponse(
        [property: JsonPropertyName("currentStreak")] int CurrentStreak,
        [property: JsonPropertyName("isWinStreak")] bool IsWinStreak,
        [property: JsonPropertyName("longestWinStreak")] int LongestWinStreak,
        [property: JsonPropertyName("longestLossStreak")] int LongestLossStreak,
        [property: JsonPropertyName("streakMessage")] string StreakMessage
    );
}

