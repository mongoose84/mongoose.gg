using static RiotProxy.Application.DTOs.SoloMatchupsDto;

namespace RiotProxy.Core.Interfaces;

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
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <returns>Champion matchups response</returns>
    Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(string puuid, string? queueType = null, string? timeRange = null);
}

