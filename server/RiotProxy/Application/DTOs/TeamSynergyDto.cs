using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamSynergyDto
{
    /// <summary>
    /// Response containing pairwise synergy data for team games (3+ players)
    /// Shows win rates for each player pair combination within the team
    /// </summary>
    public record TeamSynergyResponse(
        [property: JsonPropertyName("playerPairs")] IList<PlayerPairSynergy> PlayerPairs,
        [property: JsonPropertyName("players")] IList<string> Players
    );

    /// <summary>
    /// Synergy statistics for a specific player pair within team games
    /// </summary>
    public record PlayerPairSynergy(
        [property: JsonPropertyName("player1")] string Player1,
        [property: JsonPropertyName("player2")] string Player2,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate
    );
}

