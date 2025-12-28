using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs
{
    public class ChampionPerformanceDto
    {
        /// <summary>
        /// Response containing champion performance data split by server
        /// </summary>
        public record ChampionPerformanceResponse(
            [property: JsonPropertyName("champions")] IList<ChampionStats> Champions
        );

        /// <summary>
        /// Champion statistics across servers
        /// </summary>
        public record ChampionStats(
            [property: JsonPropertyName("championName")] string ChampionName,
            [property: JsonPropertyName("championId")] int ChampionId,
            [property: JsonPropertyName("servers")] IList<ServerStats> Servers
        );

        /// <summary>
        /// Statistics for a specific server
        /// </summary>
        public record ServerStats(
            [property: JsonPropertyName("serverName")] string ServerName,
            [property: JsonPropertyName("gamerName")] string GamerName,
            [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
            [property: JsonPropertyName("wins")] int Wins,
            [property: JsonPropertyName("winrate")] double Winrate
        );
    }
}

