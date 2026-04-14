using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for champion matchup statistics.
/// Provides data about how a player performs with specific champions against specific opponents.
/// </summary>
public interface IMatchupRepository
{
    /// <summary>
    /// Get champion matchups data for a player.
    /// Returns top 5 most-played champions with their opponent matchup details.
    /// Supports optional queue filtering and time range filtering.
    /// Aggregates data across multiple accounts if multiple PUUIDs are provided.
    /// </summary>
    /// <param name="puuids">Player PUUIDs (one or more for multi-account aggregation)</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <returns>Champion matchups response</returns>
    Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
}

