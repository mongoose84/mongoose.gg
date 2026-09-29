using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Reads the per-match rows behind the Solo page (features/solo-trends.spec.md). The rules that turn
/// them into trends, factors and patterns live in <c>Core/Services/Solo</c>.
/// </summary>
public interface ISoloTrendsRepository
{
    /// <summary>
    /// The player's Summoner's Rift matches in scope, oldest first. <paramref name="queueType"/> is a
    /// validated Solo queue (<c>ranked_solo</c>, <c>ranked_flex</c> or <c>all</c> = the Summoner's Rift set).
    /// </summary>
    Task<IReadOnlyList<SoloMatchRow>> GetMatchRowsAsync(IReadOnlyList<string> puuids, string queueType, SoloRange range);

    /// <summary>Ranked Solo/Duo and Flex match counts in the current season.</summary>
    Task<SoloQueueCounts> GetSeasonQueueCountsAsync(IReadOnlyList<string> puuids);
}
