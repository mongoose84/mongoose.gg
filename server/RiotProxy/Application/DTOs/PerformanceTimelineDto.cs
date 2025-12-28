using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class PerformanceTimelineDto
{
    /// <summary>
    /// Response containing performance data points for all gamers in a user's dashboard.
    /// </summary>
    public record PerformanceTimelineResponse(
        [property: JsonPropertyName("gamers")] IList<GamerTimeline> Gamers
    );

    /// <summary>
    /// Performance timeline for a single gamer account.
    /// </summary>
    public record GamerTimeline(
        [property: JsonPropertyName("gamerName")] string GamerName,
        [property: JsonPropertyName("dataPoints")] IList<PerformanceDataPoint> DataPoints
    );

    /// <summary>
    /// A single data point representing performance metrics at a specific game.
    /// Games are ordered from oldest to newest (ascending by GameEndTimestamp).
    /// </summary>
    public record PerformanceDataPoint(
        /// <summary>Game number in sequence (1 = oldest game)</summary>
        [property: JsonPropertyName("gameNumber")] int GameNumber,
        
        /// <summary>Running winrate percentage up to and including this game</summary>
        [property: JsonPropertyName("winrate")] double Winrate,
        
        /// <summary>Gold earned per minute in this specific game</summary>
        [property: JsonPropertyName("goldPerMin")] double GoldPerMin,
        
        /// <summary>CS (creep score) per minute in this specific game</summary>
        [property: JsonPropertyName("csPerMin")] double CsPerMin,
        
        /// <summary>Whether this specific game was a win</summary>
        [property: JsonPropertyName("win")] bool Win,
        
        /// <summary>Game end timestamp (ISO 8601)</summary>
        [property: JsonPropertyName("gameEndTimestamp")] DateTime GameEndTimestamp,
        
        /// <summary>Optional: Patch version if available</summary>
        [property: JsonPropertyName("patch")] string? Patch
    );
}
