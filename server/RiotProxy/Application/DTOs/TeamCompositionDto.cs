using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamCompositionDto
{
    /// <summary>
    /// Response containing role distribution for each player in team games
    /// </summary>
    public record TeamCompositionResponse(
        [property: JsonPropertyName("players")] IList<PlayerRoleDistribution> Players,
        [property: JsonPropertyName("roleConflicts")] IList<RoleConflict> RoleConflicts
    );

    /// <summary>
    /// Role distribution for a single player in team games
    /// </summary>
    public record PlayerRoleDistribution(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("roles")] IList<RoleStats> Roles,
        [property: JsonPropertyName("primaryRole")] string PrimaryRole
    );

    /// <summary>
    /// Statistics for a specific role
    /// </summary>
    public record RoleStats(
        [property: JsonPropertyName("position")] string Position,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("percentage")] double Percentage,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate
    );

    /// <summary>
    /// Represents a role conflict where multiple players prefer the same role
    /// </summary>
    public record RoleConflict(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("players")] IList<string> Players,
        [property: JsonPropertyName("conflictScore")] double ConflictScore
    );
}

