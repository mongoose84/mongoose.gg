using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class DuoVsSoloPerformanceDto
{
    /// <summary>
    /// Response containing performance comparison between duo and solo games
    /// </summary>
    public record DuoVsSoloPerformanceResponse(
        [property: JsonPropertyName("duoWinRate")] double DuoWinRate,
        [property: JsonPropertyName("soloAWinRate")] double SoloAWinRate,
        [property: JsonPropertyName("soloBWinRate")] double SoloBWinRate,
        [property: JsonPropertyName("duoGoldPerMin")] double DuoGoldPerMin,
        [property: JsonPropertyName("soloAGoldPerMin")] double SoloAGoldPerMin,
        [property: JsonPropertyName("soloBGoldPerMin")] double SoloBGoldPerMin,
        [property: JsonPropertyName("duoKda")] double DuoKda,
        [property: JsonPropertyName("soloAKda")] double SoloAKda,
        [property: JsonPropertyName("soloBKda")] double SoloBKda
    );
}

