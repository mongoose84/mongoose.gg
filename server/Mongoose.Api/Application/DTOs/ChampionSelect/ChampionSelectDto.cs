using System.Text.Json.Serialization;
using static Mongoose.Api.Application.DTOs.MainChampionDto;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// DTOs for champion select functionality.
/// Contains only the data needed for champion recommendations during champion select.
/// </summary>
public static class ChampionSelectDto
{
    /// <summary>
    /// Focused response for champion select containing only the data needed
    /// for champion recommendations. This avoids over-fetching the full
    /// SoloPerformanceResponse which contains many unneeded fields.
    /// </summary>
    public record ChampionSelectResponse(
        [property: JsonPropertyName("mainChampions")] MainChampionRoleGroup[] MainChampions,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("winRate")] double WinRate
    );
}

