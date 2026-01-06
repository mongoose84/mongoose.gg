using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs
{
    public class RoleDistributionDto
    {
        /// <summary>
        /// Response containing role distribution data for all gamers
        /// </summary>
        public record RoleDistributionResponse(
            [property: JsonPropertyName("gamers")] IList<GamerRoleDistribution> Gamers
        );

        /// <summary>
        /// Role distribution for a specific gamer
        /// </summary>
        public record GamerRoleDistribution(
            [property: JsonPropertyName("gamerName")] string GamerName,
            [property: JsonPropertyName("serverName")] string ServerName,
            [property: JsonPropertyName("roles")] IList<RoleStats> Roles
        );

        /// <summary>
        /// Statistics for a specific role/position
        /// </summary>
        public record RoleStats(
            [property: JsonPropertyName("position")] string Position,
            [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
            [property: JsonPropertyName("percentage")] double Percentage
        );
    }
}

