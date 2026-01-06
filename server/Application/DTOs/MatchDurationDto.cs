using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class MatchDurationDto
{
    /// <summary>
    /// Response containing winrate by game duration buckets for all gamers
    /// </summary>
    public record MatchDurationResponse(
        [property: JsonPropertyName("gamers")] IList<GamerDurationStats> Gamers
    );

    /// <summary>
    /// Duration statistics for a single gamer account
    /// </summary>
    public record GamerDurationStats(
        [property: JsonPropertyName("gamerName")] string GamerName,
        [property: JsonPropertyName("serverName")] string ServerName,
        [property: JsonPropertyName("buckets")] IList<DurationBucket> Buckets
    );

    /// <summary>
    /// Winrate statistics for a specific duration bucket
    /// </summary>
    public record DurationBucket(
        [property: JsonPropertyName("durationRange")] string DurationRange,
        [property: JsonPropertyName("minMinutes")] int MinMinutes,
        [property: JsonPropertyName("maxMinutes")] int MaxMinutes,
        [property: JsonPropertyName("winrate")] double Winrate,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed
    );
}

