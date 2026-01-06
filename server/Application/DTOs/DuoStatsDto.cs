using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class DuoStatsDto
{
    /// <summary>
    /// Response containing statistics for games played together by two players
    /// </summary>
    public record DuoStatsResponse(
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("queueType")] string QueueType
    );
}

