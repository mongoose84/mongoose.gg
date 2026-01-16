using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

/// <summary>
/// DTOs for the Champion Matchups endpoint.
/// Shows top champions with their performance against specific opponents.
/// </summary>
public static class SoloMatchupsDto
{
    /// <summary>
    /// Response containing champion matchups data.
    /// </summary>
    public record ChampionMatchupsResponse(
        [property: JsonPropertyName("matchups")] ChampionMatchup[] Matchups,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("timeRange")] string TimeRange
    );

    /// <summary>
    /// Matchup data for a specific champion the player uses.
    /// </summary>
    public record ChampionMatchup(
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("totalGames")] int TotalGames,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("opponents")] OpponentMatchup[] Opponents
    );

    /// <summary>
    /// Matchup data against a specific opponent champion.
    /// </summary>
    public record OpponentMatchup(
        [property: JsonPropertyName("opponentChampionId")] int OpponentChampionId,
        [property: JsonPropertyName("opponentChampionName")] string OpponentChampionName,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses,
        [property: JsonPropertyName("winRate")] double WinRate
    );
}

