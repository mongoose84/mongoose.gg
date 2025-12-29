using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class SideStatsDto
{
    /// <summary>
    /// Response containing side (blue/red) win statistics
    /// </summary>
    public record SideStatsResponse(
        [property: JsonPropertyName("blueGames")] int BlueGames,
        [property: JsonPropertyName("blueWins")] int BlueWins,
        [property: JsonPropertyName("blueWinRate")] double BlueWinRate,
        [property: JsonPropertyName("redGames")] int RedGames,
        [property: JsonPropertyName("redWins")] int RedWins,
        [property: JsonPropertyName("redWinRate")] double RedWinRate,
        [property: JsonPropertyName("totalGames")] int TotalGames,
        [property: JsonPropertyName("bluePercentage")] double BluePercentage,
        [property: JsonPropertyName("redPercentage")] double RedPercentage
    );
}

