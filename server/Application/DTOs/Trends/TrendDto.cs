using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// DTOs for trend-related endpoints.
/// Provides data structures for winrate and LP trend charts.
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

    /// <summary>
    /// Response DTO for the winrate trend endpoint.
    /// </summary>
    public record WinrateTrendResponse(
        [property: JsonPropertyName("winrateTrend")] WinrateTrendPoint[] WinrateTrend
    );

    /// <summary>
    /// Response DTO for the LP trend endpoint.
    /// </summary>
    public record LpTrendResponse(
        [property: JsonPropertyName("lpTrend")] LpTrendPoint[] LpTrend
    );
}

