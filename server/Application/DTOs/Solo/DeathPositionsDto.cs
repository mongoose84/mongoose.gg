using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs.Solo;

public static class DeathPositionsDto
{
    public record DeathPosition(
        [property: JsonPropertyName("x")] int X,
        [property: JsonPropertyName("y")] int Y,
        [property: JsonPropertyName("minuteMark")] int MinuteMark,
        [property: JsonPropertyName("phase")] string Phase,
        [property: JsonPropertyName("killerChampionId")] int? KillerChampionId,
        [property: JsonPropertyName("assistCount")] int AssistCount,
        [property: JsonIgnore] string MatchId = ""
    );

    public record PhaseSummary(
        [property: JsonPropertyName("early")] int Early,
        [property: JsonPropertyName("mid")] int Mid,
        [property: JsonPropertyName("late")] int Late,
        [property: JsonPropertyName("veryLate")] int VeryLate
    );

    public record DeathPositionsResponse(
        [property: JsonPropertyName("deaths")] DeathPosition[] Deaths,
        [property: JsonPropertyName("totalDeaths")] int TotalDeaths,
        [property: JsonPropertyName("matchesAnalyzed")] int MatchesAnalyzed,
        [property: JsonPropertyName("phaseSummary")] PhaseSummary PhaseSummary
    );
}
