using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class DuoKillAnalysisDto
{
    // ========================
    // Multi-Kill Showcase
    // ========================
    
    public record DuoMultiKillsResponse(
        [property: JsonPropertyName("players")] IList<DuoPlayerMultiKills> Players,
        [property: JsonPropertyName("duoTotals")] DuoMultiKillTotals DuoTotals
    );

    public record DuoPlayerMultiKills(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("doubleKills")] int DoubleKills,
        [property: JsonPropertyName("tripleKills")] int TripleKills,
        [property: JsonPropertyName("quadraKills")] int QuadraKills,
        [property: JsonPropertyName("pentaKills")] int PentaKills,
        [property: JsonPropertyName("totalMultiKills")] int TotalMultiKills
    );

    public record DuoMultiKillTotals(
        [property: JsonPropertyName("doubleKills")] int DoubleKills,
        [property: JsonPropertyName("tripleKills")] int TripleKills,
        [property: JsonPropertyName("quadraKills")] int QuadraKills,
        [property: JsonPropertyName("pentaKills")] int PentaKills
    );

    // ========================
    // Kills by Game Duration
    // ========================

    public record DuoKillsByDurationResponse(
        [property: JsonPropertyName("buckets")] IList<DuoKillDurationBucket> Buckets,
        [property: JsonPropertyName("bestDuration")] string BestDuration,
        [property: JsonPropertyName("bestAvgKills")] double BestAvgKills
    );

    public record DuoKillDurationBucket(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    // ========================
    // Kill Participation
    // ========================
    
    public record DuoKillParticipationResponse(
        [property: JsonPropertyName("players")] IList<DuoPlayerKillParticipation> Players,
        [property: JsonPropertyName("totalDuoKills")] int TotalDuoKills
    );

    public record DuoPlayerKillParticipation(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("kills")] int Kills,
        [property: JsonPropertyName("assists")] int Assists,
        [property: JsonPropertyName("killParticipation")] double KillParticipation,
        [property: JsonPropertyName("avgKillsPerGame")] double AvgKillsPerGame
    );

    // ========================
    // Kills Trend Over Time
    // ========================
    
    public record DuoKillsTrendResponse(
        [property: JsonPropertyName("dataPoints")] IList<DuoKillTrendDataPoint> DataPoints,
        [property: JsonPropertyName("overallAvgKills")] double OverallAvgKills,
        [property: JsonPropertyName("recentAvgKills")] double RecentAvgKills,
        [property: JsonPropertyName("trendDirection")] string TrendDirection
    );

    public record DuoKillTrendDataPoint(
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        [property: JsonPropertyName("duoKills")] int DuoKills,
        [property: JsonPropertyName("rollingAvgKills")] double RollingAvgKills,
        [property: JsonPropertyName("win")] bool Win,
        [property: JsonPropertyName("gameDate")] DateTime GameDate
    );
}

