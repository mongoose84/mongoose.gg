using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamDeathAnalysisDto
{
    // ========================
    // Death Timer Impact
    // ========================
    
    /// <summary>
    /// Response for death timer impact analysis - shows time dead in wins vs losses per player
    /// </summary>
    public record DeathTimerImpactResponse(
        [property: JsonPropertyName("players")] IList<PlayerDeathTimerStats> Players,
        [property: JsonPropertyName("teamAvgTimeDeadWins")] double TeamAvgTimeDeadWins,
        [property: JsonPropertyName("teamAvgTimeDeadLosses")] double TeamAvgTimeDeadLosses
    );

    public record PlayerDeathTimerStats(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("avgTimeDeadWins")] double AvgTimeDeadWins,
        [property: JsonPropertyName("avgTimeDeadLosses")] double AvgTimeDeadLosses,
        [property: JsonPropertyName("avgDeathsWins")] double AvgDeathsWins,
        [property: JsonPropertyName("avgDeathsLosses")] double AvgDeathsLosses
    );

    // ========================
    // Deaths by Game Duration
    // ========================
    
    /// <summary>
    /// Response for deaths by game duration - shows avg deaths in short/medium/long games
    /// </summary>
    public record DeathsByDurationResponse(
        [property: JsonPropertyName("buckets")] IList<DeathDurationBucket> Buckets
    );

    public record DeathDurationBucket(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("minMinutes")] int MinMinutes,
        [property: JsonPropertyName("maxMinutes")] int MaxMinutes,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    // ========================
    // Death Share Distribution
    // ========================
    
    /// <summary>
    /// Response for death share distribution - shows each player's % of team deaths
    /// </summary>
    public record DeathShareResponse(
        [property: JsonPropertyName("players")] IList<PlayerDeathShare> Players,
        [property: JsonPropertyName("totalTeamDeaths")] int TotalTeamDeaths
    );

    public record PlayerDeathShare(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("totalDeaths")] int TotalDeaths,
        [property: JsonPropertyName("deathSharePercent")] double DeathSharePercent,
        [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame
    );

    // ========================
    // Deaths Trend Over Time
    // ========================
    
    /// <summary>
    /// Response for deaths trend - rolling average of team deaths over recent games
    /// </summary>
    public record DeathsTrendResponse(
        [property: JsonPropertyName("dataPoints")] IList<DeathTrendDataPoint> DataPoints,
        [property: JsonPropertyName("overallAvgDeaths")] double OverallAvgDeaths,
        [property: JsonPropertyName("recentAvgDeaths")] double RecentAvgDeaths,
        [property: JsonPropertyName("trendDirection")] string TrendDirection
    );

    public record DeathTrendDataPoint(
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        [property: JsonPropertyName("teamDeaths")] int TeamDeaths,
        [property: JsonPropertyName("rollingAvgDeaths")] double RollingAvgDeaths,
        [property: JsonPropertyName("win")] bool Win,
        [property: JsonPropertyName("gameDate")] DateTime GameDate
    );
}

