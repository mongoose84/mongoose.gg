using System.Text.Json.Serialization;

namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Focused response for champion select containing only the data needed for champion recommendations.
/// </summary>
public record ChampionSelectResponse(
    [property: JsonPropertyName("mainChampions")] MainChampionRoleGroup[] MainChampions,
    [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
    [property: JsonPropertyName("winRate")] double WinRate
);
