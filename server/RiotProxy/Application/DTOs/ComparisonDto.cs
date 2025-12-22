using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public class ComparisonDto
{
    public record ComparisonRequest(
       
        [property: JsonPropertyName("winrate")] IList<GamerRecord> Winrate,
        [property: JsonPropertyName("kda")] IList<GamerRecord> Kda,
        [property: JsonPropertyName("csPrMin")] IList<GamerRecord> CsPrMin,
        [property: JsonPropertyName("goldPrMin")] IList<GamerRecord> GoldPrMin,
        [property: JsonPropertyName("gamesPlayed")] IList<GamerRecord> GamesPlayed
     );

    public record GamerRecord(
        [property: JsonPropertyName("value")] double Value,
        [property: JsonPropertyName("gamerName")] string GamerName
    );


            
    
}
