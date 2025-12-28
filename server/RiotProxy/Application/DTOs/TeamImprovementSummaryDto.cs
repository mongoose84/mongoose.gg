using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class TeamImprovementSummaryDto
{
    /// <summary>
    /// Response containing actionable improvement insights for the team
    /// </summary>
    public record TeamImprovementSummaryResponse(
        [property: JsonPropertyName("insights")] IList<Insight> Insights,
        [property: JsonPropertyName("strengths")] IList<string> Strengths,
        [property: JsonPropertyName("weaknesses")] IList<string> Weaknesses
    );

    /// <summary>
    /// A single improvement insight
    /// </summary>
    public record Insight(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("priority")] int Priority
    );
}

