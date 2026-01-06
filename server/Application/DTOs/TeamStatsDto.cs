using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamStatsDto
{
    /// <summary>
    /// Response containing overall statistics for games played together by a team (3+ players)
    /// </summary>
    public record TeamStatsResponse(
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("avgKda")] double AvgKda,
        [property: JsonPropertyName("avgGameDurationMinutes")] double AvgGameDurationMinutes,
        [property: JsonPropertyName("playerCount")] int PlayerCount
    );
}

