using System.Text.Json.Serialization;

namespace Mongoose.Api.Core.QueryModels;

public record RadarAxis(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("value")] double Value,
    [property: JsonPropertyName("rawValue")] double RawValue,
    [property: JsonPropertyName("rawUnit")] string RawUnit
);

public record RadarChartResponse(
    [property: JsonPropertyName("axes")] RadarAxis[] Axes,
    [property: JsonPropertyName("gamesAnalyzed")] int GamesAnalyzed
);
