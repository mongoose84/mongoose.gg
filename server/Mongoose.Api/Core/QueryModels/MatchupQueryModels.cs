using System.Text.Json.Serialization;

namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Response containing champion matchups data.
/// </summary>
public record ChampionMatchupsResponse(
    [property: JsonPropertyName("matchups")] ChampionMatchup[] Matchups,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("timeRange")] string TimeRange
);

/// <summary>
/// Matchup data for a specific champion the player uses.
/// </summary>
public record ChampionMatchup(
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("totalGames")] int TotalGames,
    [property: JsonPropertyName("wins")] int Wins,
    [property: JsonPropertyName("winRate")] double WinRate,
    [property: JsonPropertyName("opponents")] OpponentMatchup[] Opponents
);

/// <summary>
/// Matchup data against a specific opponent champion.
/// </summary>
public record OpponentMatchup(
    [property: JsonPropertyName("opponentChampionId")] int OpponentChampionId,
    [property: JsonPropertyName("opponentChampionName")] string OpponentChampionName,
    [property: JsonPropertyName("inLaneWins")] int InLaneWins,
    [property: JsonPropertyName("inLaneLosses")] int InLaneLosses,
    [property: JsonPropertyName("outOfLaneWins")] int OutOfLaneWins,
    [property: JsonPropertyName("outOfLaneLosses")] int OutOfLaneLosses
);
