using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// DTOs for trend-related endpoints.
/// Provides data structures for winrate trend charts.
/// </summary>
public static class TrendDto
{
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
    /// Response DTO for the winrate trend endpoint.
    /// </summary>
    public record WinrateTrendResponse(
        [property: JsonPropertyName("winrateTrend")] WinrateTrendPoint[] WinrateTrend
    );

    /// <summary>
    /// A single data point for the gold at 15 trend chart.
    /// Represents player's gold at 15 minutes with opponent comparison.
    /// </summary>
    public record GoldAt15TrendPoint(
        [property: JsonPropertyName("matchId")] string MatchId,
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("playerGold")] int PlayerGold,
        [property: JsonPropertyName("opponentGold")] int? OpponentGold,
        [property: JsonPropertyName("goldDifferential")] int? GoldDifferential,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("opponentChampion")] string? OpponentChampion
    );

    /// <summary>
    /// Response DTO for the gold at 15 trend endpoint.
    /// </summary>
    public record GoldAt15TrendResponse(
        [property: JsonPropertyName("goldAt15Trend")] GoldAt15TrendPoint[] GoldAt15Trend
    );

    /// <summary>
    /// A single data point for the CS per minute trend chart.
    /// Represents player's farming efficiency over time.
    /// </summary>
    public record CsPerMinuteTrendPoint(
        [property: JsonPropertyName("matchId")] string MatchId,
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("totalCs")] int TotalCs,
        [property: JsonPropertyName("csPerMinute")] double CsPerMinute,
        [property: JsonPropertyName("gameDurationMinutes")] double GameDurationMinutes,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string? Role
    );

    /// <summary>
    /// Response DTO for the CS per minute trend endpoint.
    /// </summary>
    public record CsPerMinuteTrendResponse(
        [property: JsonPropertyName("csPerMinuteTrend")] CsPerMinuteTrendPoint[] CsPerMinuteTrend
    );

    /// <summary>
    /// A single data point for the deaths over time trend chart.
    /// Represents player's death count per game with rolling average for trend analysis.
    /// </summary>
    public record DeathsTrendPoint(
        [property: JsonPropertyName("matchId")] string MatchId,
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("deaths")] int Deaths,
        [property: JsonPropertyName("rollingAverage")] double RollingAverage,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("gameDurationMinutes")] double GameDurationMinutes
    );

    /// <summary>
    /// Response DTO for the deaths trend endpoint.
    /// </summary>
    public record DeathsTrendResponse(
        [property: JsonPropertyName("deathsTrend")] DeathsTrendPoint[] DeathsTrend,
        [property: JsonPropertyName("averageDeaths")] double AverageDeaths,
        [property: JsonPropertyName("overallAverage")] double OverallAverage,
        [property: JsonPropertyName("trend")] string Trend
    );

    /// <summary>
    /// A single data point for the dragon participation trend chart.
    /// Represents player's dragon participation rate for a specific game.
    /// </summary>
    public record DragonParticipationTrendPoint(
        [property: JsonPropertyName("matchId")] string MatchId,
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("teamDragons")] int TeamDragons,
        [property: JsonPropertyName("dragonsParticipated")] int DragonsParticipated,
        [property: JsonPropertyName("participationRate")] double ParticipationRate,
        [property: JsonPropertyName("rollingAverage")] double RollingAverage,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string? Role
    );

    /// <summary>
    /// Response DTO for the dragon participation trend endpoint.
    /// </summary>
    public record DragonParticipationTrendResponse(
        [property: JsonPropertyName("dragonParticipationTrend")] DragonParticipationTrendPoint[] DragonParticipationTrend,
        [property: JsonPropertyName("averageParticipation")] double AverageParticipation,
        [property: JsonPropertyName("overallAverage")] double OverallAverage,
        [property: JsonPropertyName("trend")] string Trend
    );

    /// <summary>
    /// A single data point for the vision score trend chart.
    /// Represents player's vision score per minute for a specific game.
    /// </summary>
    public record VisionScoreTrendPoint(
        [property: JsonPropertyName("matchId")] string MatchId,
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("visionScore")] int VisionScore,
        [property: JsonPropertyName("visionScorePerMinute")] double VisionScorePerMinute,
        [property: JsonPropertyName("rollingAverage")] double RollingAverage,
        [property: JsonPropertyName("gameDurationMinutes")] double GameDurationMinutes,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("wardsPlaced")] int WardsPlaced,
        [property: JsonPropertyName("wardsDestroyed")] int WardsDestroyed
    );

    /// <summary>
    /// Response DTO for the vision score trend endpoint.
    /// </summary>
    public record VisionScoreTrendResponse(
        [property: JsonPropertyName("visionScoreTrend")] VisionScoreTrendPoint[] VisionScoreTrend,
        [property: JsonPropertyName("averageVisionPerMinute")] double AverageVisionPerMinute,
        [property: JsonPropertyName("overallAverage")] double OverallAverage,
        [property: JsonPropertyName("roleTarget")] double RoleTarget,
        [property: JsonPropertyName("trend")] string Trend
    );
}

