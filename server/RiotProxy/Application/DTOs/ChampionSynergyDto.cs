using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class ChampionSynergyDto
{
    /// <summary>
    /// Response containing champion synergy data for duo games
    /// </summary>
    public record ChampionSynergyResponse(
        [property: JsonPropertyName("synergies")] IList<ChampionPairStats> Synergies
    );

    /// <summary>
    /// Statistics for a specific champion pair combination
    /// </summary>
    public record ChampionPairStats(
        [property: JsonPropertyName("championId1")] int ChampionId1,
        [property: JsonPropertyName("championName1")] string ChampionName1,
        [property: JsonPropertyName("championId2")] int ChampionId2,
        [property: JsonPropertyName("championName2")] string ChampionName2,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses,
        [property: JsonPropertyName("winrate")] double Winrate
    );
}

