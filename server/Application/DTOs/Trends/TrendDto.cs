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
}

