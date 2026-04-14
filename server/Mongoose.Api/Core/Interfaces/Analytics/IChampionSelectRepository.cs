using static Mongoose.Api.Application.DTOs.ChampionSelectDto;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for champion select data.
/// Provides focused data for champion recommendations without over-fetching.
/// </summary>
public interface IChampionSelectRepository
{
    /// <summary>
    /// Get champion select data for a player.
    /// Returns only the data needed for champion recommendations: main champions, games played, win rate.
    /// Supports optional queue filtering and time range filtering.
    /// Aggregates data across multiple accounts if multiple PUUIDs are provided.
    /// </summary>
    /// <param name="puuids">Player PUUIDs (one or more for multi-account aggregation)</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <returns>ChampionSelectResponse or null if no data found</returns>
    Task<ChampionSelectResponse?> GetChampionSelectDataAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
}

