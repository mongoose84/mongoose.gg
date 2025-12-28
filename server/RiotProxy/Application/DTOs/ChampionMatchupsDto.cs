using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs
{
    public class ChampionMatchupsDto
    {
        /// <summary>
        /// Response containing champion matchup data grouped by role
        /// </summary>
        public record ChampionMatchupsResponse(
            [property: JsonPropertyName("matchups")] IList<ChampionRoleMatchup> Matchups
        );

        /// <summary>
        /// Matchup data for a specific champion in a specific role
        /// </summary>
        public record ChampionRoleMatchup(
            [property: JsonPropertyName("championName")] string ChampionName,
            [property: JsonPropertyName("championId")] int ChampionId,
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("totalGames")] int TotalGames,
            [property: JsonPropertyName("totalWins")] int TotalWins,
            [property: JsonPropertyName("winrate")] double Winrate,
            [property: JsonPropertyName("opponents")] IList<OpponentStats> Opponents
        );

        /// <summary>
        /// Statistics against a specific opponent champion
        /// </summary>
        public record OpponentStats(
            [property: JsonPropertyName("opponentChampionName")] string OpponentChampionName,
            [property: JsonPropertyName("opponentChampionId")] int OpponentChampionId,
            [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
            [property: JsonPropertyName("wins")] int Wins,
            [property: JsonPropertyName("losses")] int Losses,
            [property: JsonPropertyName("winrate")] double Winrate
        );
    }
}

