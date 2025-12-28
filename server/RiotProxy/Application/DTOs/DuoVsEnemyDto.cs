using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class DuoVsEnemyDto
{
    /// <summary>
    /// Response containing duo champion combination vs enemy champion statistics
    /// </summary>
    public record DuoVsEnemyResponse(
        [property: JsonPropertyName("matchups")] IList<DuoVsEnemyStats> Matchups
    );

    /// <summary>
    /// Statistics for a duo champion combination vs a specific enemy champion
    /// </summary>
    public record DuoVsEnemyStats(
        [property: JsonPropertyName("duoChampionId1")] int DuoChampionId1,
        [property: JsonPropertyName("duoChampionName1")] string DuoChampionName1,
        [property: JsonPropertyName("duoLane1")] string DuoLane1,
        [property: JsonPropertyName("duoChampionId2")] int DuoChampionId2,
        [property: JsonPropertyName("duoChampionName2")] string DuoChampionName2,
        [property: JsonPropertyName("duoLane2")] string DuoLane2,
        [property: JsonPropertyName("enemyLane")] string EnemyLane,
        [property: JsonPropertyName("enemyChampionId")] int EnemyChampionId,
        [property: JsonPropertyName("enemyChampionName")] string EnemyChampionName,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses,
        [property: JsonPropertyName("winrate")] double Winrate
    );
}

