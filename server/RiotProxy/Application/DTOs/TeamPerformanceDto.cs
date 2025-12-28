using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamPerformanceDto
{
    /// <summary>
    /// Response containing individual performance metrics for each player in team games
    /// </summary>
    public record TeamPerformanceResponse(
        [property: JsonPropertyName("players")] IList<PlayerPerformance> Players,
        [property: JsonPropertyName("teamTotals")] TeamTotals TeamTotals
    );

    /// <summary>
    /// Performance metrics for a single player in team games
    /// </summary>
    public record PlayerPerformance(
        [property: JsonPropertyName("playerName")] string PlayerName,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("avgKills")] double AvgKills,
        [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
        [property: JsonPropertyName("avgAssists")] double AvgAssists,
        [property: JsonPropertyName("kda")] double Kda,
        [property: JsonPropertyName("goldPerMin")] double GoldPerMin,
        [property: JsonPropertyName("csPerMin")] double CsPerMin,
        [property: JsonPropertyName("killParticipation")] double KillParticipation,
        [property: JsonPropertyName("deathShare")] double DeathShare
    );

    /// <summary>
    /// Aggregated totals for the team
    /// </summary>
    public record TeamTotals(
        [property: JsonPropertyName("totalKills")] int TotalKills,
        [property: JsonPropertyName("totalDeaths")] int TotalDeaths,
        [property: JsonPropertyName("totalAssists")] int TotalAssists,
        [property: JsonPropertyName("avgTeamKda")] double AvgTeamKda,
        [property: JsonPropertyName("avgTeamGoldPerMin")] double AvgTeamGoldPerMin
    );
}

