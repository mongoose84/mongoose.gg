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
        [property: JsonPropertyName("players")] IList<PlayerInfo> Players
    );

    /// <summary>
    /// Player info with name and most common role
    /// </summary>
    public record PlayerInfo(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("role")] string Role
    );

    /// <summary>
    /// Synergy statistics for a specific player pair within team games
    /// </summary>
    public record PlayerPairSynergy(
        [property: JsonPropertyName("player1")] string Player1,
        [property: JsonPropertyName("player1Role")] string Player1Role,
        [property: JsonPropertyName("player2")] string Player2,
        [property: JsonPropertyName("player2Role")] string Player2Role,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate
    );
}

