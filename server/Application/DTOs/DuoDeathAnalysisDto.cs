using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class DuoDeathAnalysisDto
{
    // ========================
    // Death Timer Impact
    // ========================
    
    public record DuoDeathTimerResponse(
        [property: JsonPropertyName("players")] IList<DuoPlayerDeathTimer> Players,
        [property: JsonPropertyName("insight")] string Insight
    );

    public record DuoPlayerDeathTimer(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("avgTimeDeadWins")] double AvgTimeDeadWins,
        [property: JsonPropertyName("avgTimeDeadLosses")] double AvgTimeDeadLosses,
        [property: JsonPropertyName("deathTimeDifference")] double DeathTimeDifference
    );

    // ========================
    // Deaths by Game Duration
    // ========================

    public record DuoDeathsByDurationResponse(
        [property: JsonPropertyName("buckets")] IList<DuoDeathDurationBucket> Buckets,
        [property: JsonPropertyName("worstDuration")] string WorstDuration,
        [property: JsonPropertyName("worstAvgDeaths")] double WorstAvgDeaths
    );

    public record DuoDeathDurationBucket(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    // ========================
    // Death Share
    // ========================
    
    public record DuoDeathShareResponse(
        [property: JsonPropertyName("players")] IList<DuoPlayerDeathShare> Players,
        [property: JsonPropertyName("totalDeaths")] int TotalDeaths
    );

    public record DuoPlayerDeathShare(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("deaths")] int Deaths,
        [property: JsonPropertyName("deathShare")] double DeathShare,
        [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame
    );

    // ========================
    // Deaths Trend Over Time
    // ========================
    
    public record DuoDeathsTrendResponse(
        [property: JsonPropertyName("dataPoints")] IList<DuoDeathTrendDataPoint> DataPoints,
        [property: JsonPropertyName("overallAvgDeaths")] double OverallAvgDeaths,
        [property: JsonPropertyName("recentAvgDeaths")] double RecentAvgDeaths,
        [property: JsonPropertyName("trendDirection")] string TrendDirection
    );

    public record DuoDeathTrendDataPoint(
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        [property: JsonPropertyName("duoDeaths")] int DuoDeaths,
        [property: JsonPropertyName("rollingAvgDeaths")] double RollingAvgDeaths,
        [property: JsonPropertyName("win")] bool Win,
        [property: JsonPropertyName("gameDate")] DateTime GameDate
    );
}

