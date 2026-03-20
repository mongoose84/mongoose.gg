using System.Text.Json.Serialization;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;

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

    /// <summary>
    /// Rank info for a single account, used when displaying all accounts' ranks in Overall mode.
    /// </summary>
    public record AccountRankInfo(
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("soloDuoRank")] QueueRankInfo SoloDuoRank,
        [property: JsonPropertyName("flexRank")] QueueRankInfo FlexRank
    );

    /// <summary>
    /// Enhanced solo performance response that includes rank information.
    /// Wraps the base SoloPerformanceResponse to avoid field-by-field duplication.
    /// </summary>
    public class SoloPerformanceWithRankResponse
    {
        [JsonPropertyName("gamesPlayed")]
        public int GamesPlayed { get; init; }

        [JsonPropertyName("wins")]
        public int Wins { get; init; }

        [JsonPropertyName("winRate")]
        public double WinRate { get; init; }

        [JsonPropertyName("avgKda")]
        public double AvgKda { get; init; }

        [JsonPropertyName("avgGameDurationMinutes")]
        public double AvgGameDurationMinutes { get; init; }

        [JsonPropertyName("avgKills")]
        public double AvgKills { get; init; }

        [JsonPropertyName("avgDeaths")]
        public double AvgDeaths { get; init; }

        [JsonPropertyName("avgAssists")]
        public double AvgAssists { get; init; }

        [JsonPropertyName("overallWinRate")]
        public double OverallWinRate { get; init; }

        [JsonPropertyName("overallAvgKills")]
        public double OverallAvgKills { get; init; }

        [JsonPropertyName("overallAvgDeaths")]
        public double OverallAvgDeaths { get; init; }

        [JsonPropertyName("overallAvgAssists")]
        public double OverallAvgAssists { get; init; }

        [JsonPropertyName("overallAvgKda")]
        public double OverallAvgKda { get; init; }

        [JsonPropertyName("sideStats")]
        public SideWinDistribution SideStats { get; init; } = null!;

        [JsonPropertyName("uniqueChampsPlayedCount")]
        public int UniqueChampsPlayedCount { get; init; }

        [JsonPropertyName("mainChampion")]
        public ChampionSummary? MainChampion { get; init; }

        [JsonPropertyName("mainChampions")]
        public MainChampionDto.MainChampionRoleGroup[] MainChampions { get; init; } = Array.Empty<MainChampionDto.MainChampionRoleGroup>();

        [JsonPropertyName("last10Games")]
        public TrendMetric? Last10Games { get; init; }

        [JsonPropertyName("last20Games")]
        public TrendMetric? Last20Games { get; init; }

        [JsonPropertyName("performanceByPhase")]
        public PerformancePhase[] PerformanceByPhase { get; init; } = Array.Empty<PerformancePhase>();

        [JsonPropertyName("roleBreakdown")]
        public RolePerformance[] RoleBreakdown { get; init; } = Array.Empty<RolePerformance>();

        [JsonPropertyName("deathEfficiency")]
        public DeathEfficiency DeathEfficiency { get; init; } = null!;

        [JsonPropertyName("queueType")]
        public string QueueType { get; init; } = string.Empty;

        [JsonPropertyName("rankInfo")]
        public RankInfo RankInfo { get; init; } = null!;

        /// <summary>
        /// Number of accounts included in this response. Greater than 1 when account=all.
        /// </summary>
        [JsonPropertyName("accountCount")]
        public int AccountCount { get; init; } = 1;

        /// <summary>
        /// Rank info for each linked account. Populated when AccountCount > 1 (Overall mode).
        /// </summary>
        [JsonPropertyName("allAccountRanks")]
        public AccountRankInfo[] AllAccountRanks { get; init; } = Array.Empty<AccountRankInfo>();

        /// <summary>
        /// Creates an enhanced response from a base performance response and rank info.
        /// </summary>
        public static SoloPerformanceWithRankResponse FromPerformanceAndRank(
            SoloPerformanceResponse performance,
            RankInfo rankInfo,
            int accountCount = 1,
            AccountRankInfo[]? allAccountRanks = null)
        {
            return new SoloPerformanceWithRankResponse
            {
                GamesPlayed = performance.GamesPlayed,
                Wins = performance.Wins,
                WinRate = performance.WinRate,
                AvgKda = performance.AvgKda,
                AvgGameDurationMinutes = performance.AvgGameDurationMinutes,
                AvgKills = performance.AvgKills,
                AvgDeaths = performance.AvgDeaths,
                AvgAssists = performance.AvgAssists,
                OverallWinRate = performance.OverallWinRate,
                OverallAvgKills = performance.OverallAvgKills,
                OverallAvgDeaths = performance.OverallAvgDeaths,
                OverallAvgAssists = performance.OverallAvgAssists,
                OverallAvgKda = performance.OverallAvgKda,
                SideStats = performance.SideStats,
                UniqueChampsPlayedCount = performance.UniqueChampsPlayedCount,
                MainChampion = performance.MainChampion,
                MainChampions = performance.MainChampions,
                Last10Games = performance.Last10Games,
                Last20Games = performance.Last20Games,
                PerformanceByPhase = performance.PerformanceByPhase,
                RoleBreakdown = performance.RoleBreakdown,
                DeathEfficiency = performance.DeathEfficiency,
                QueueType = performance.QueueType,
                RankInfo = rankInfo,
                AccountCount = accountCount,
                AllAccountRanks = allAccountRanks ?? Array.Empty<AccountRankInfo>()
            };
        }
    }
}
