using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// DTOs for ranked information display in solo dashboard
/// </summary>
public static class RankInfoDto
{
    /// <summary>
    /// Individual rank data for a single queue (Solo/Duo or Flex)
    /// </summary>
    public record QueueRankInfo(
        [property: JsonPropertyName("tier")] string? Tier,
        [property: JsonPropertyName("division")] string? Division,
        [property: JsonPropertyName("lp")] int? Lp,
        [property: JsonPropertyName("hasRank")] bool HasRank
    );

    /// <summary>
    /// Combined rank info containing both Solo/Duo and Flex ranks
    /// </summary>
    public record RankInfo(
        [property: JsonPropertyName("soloDuoRank")] QueueRankInfo SoloDuoRank,
        [property: JsonPropertyName("flexRank")] QueueRankInfo FlexRank
    );
}
