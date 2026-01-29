using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

/// <summary>
/// DTOs for main champion recommendations per role.
/// </summary>
public static class MainChampionDto
{
    public record MainChampionRoleGroup(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("champions")] MainChampionEntry[] Champions
    );

    public record MainChampionEntry(
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses
    );
}

