using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamKillAnalysisDto
{
    // ========================
    // Kill Participation Share
    // ========================
    
    /// <summary>
    /// Response for kill participation - shows each player's contribution to team kills
    /// </summary>
    public record KillParticipationResponse(
        [property: JsonPropertyName("players")] IList<PlayerKillParticipation> Players,
        [property: JsonPropertyName("totalTeamKills")] int TotalTeamKills
    );

    public record PlayerKillParticipation(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("kills")] int Kills,
        [property: JsonPropertyName("assists")] int Assists,
        [property: JsonPropertyName("killParticipation")] double KillParticipation,
        [property: JsonPropertyName("avgKillsPerGame")] double AvgKillsPerGame
    );

    // ========================
    // Kills by Game Duration
    // ========================

    /// <summary>
    /// Response for kills by game duration - average kills in different length games
    /// </summary>
    public record KillsByDurationResponse(
        [property: JsonPropertyName("buckets")] IList<KillDurationBucket> Buckets,
        [property: JsonPropertyName("bestDuration")] string BestDuration,
        [property: JsonPropertyName("bestAvgKills")] double BestAvgKills
    );

    public record KillDurationBucket(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("minMinutes")] int MinMinutes,
        [property: JsonPropertyName("maxMinutes")] int MaxMinutes,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    // ========================
    // Kills Trend Over Time
    // ========================
    
    /// <summary>
    /// Response for kills trend - rolling average of team kills over recent games
    /// </summary>
    public record KillsTrendResponse(
        [property: JsonPropertyName("dataPoints")] IList<KillTrendDataPoint> DataPoints,
        [property: JsonPropertyName("overallAvgKills")] double OverallAvgKills,
        [property: JsonPropertyName("recentAvgKills")] double RecentAvgKills,
        [property: JsonPropertyName("trendDirection")] string TrendDirection
    );

    public record KillTrendDataPoint(
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        [property: JsonPropertyName("teamKills")] int TeamKills,
        [property: JsonPropertyName("rollingAvgKills")] double RollingAvgKills,
        [property: JsonPropertyName("win")] bool Win,
        [property: JsonPropertyName("gameDate")] DateTime GameDate
    );

    // ========================
    // Multi-Kill Showcase
    // ========================
    
    /// <summary>
    /// Response for multi-kill showcase - displays team's multi-kill achievements
    /// </summary>
    public record MultiKillsResponse(
        [property: JsonPropertyName("players")] IList<PlayerMultiKills> Players,
        [property: JsonPropertyName("teamTotals")] TeamMultiKillTotals TeamTotals
    );

    public record PlayerMultiKills(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("doubleKills")] int DoubleKills,
        [property: JsonPropertyName("tripleKills")] int TripleKills,
        [property: JsonPropertyName("quadraKills")] int QuadraKills,
        [property: JsonPropertyName("pentaKills")] int PentaKills,
        [property: JsonPropertyName("totalMultiKills")] int TotalMultiKills
    );

    public record TeamMultiKillTotals(
        [property: JsonPropertyName("doubleKills")] int DoubleKills,
        [property: JsonPropertyName("tripleKills")] int TripleKills,
        [property: JsonPropertyName("quadraKills")] int QuadraKills,
        [property: JsonPropertyName("pentaKills")] int PentaKills
    );
}

