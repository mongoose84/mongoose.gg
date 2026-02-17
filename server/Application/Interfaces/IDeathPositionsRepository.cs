using static Mongoose.Api.Application.DTOs.Solo.DeathPositionsDto;

namespace Mongoose.Api.Application.Interfaces;

/// <summary>
/// Repository for death positions data (danger zone heatmap).
/// </summary>
public interface IDeathPositionsRepository
{
    /// <summary>
    /// Gets death positions for a player across their matches.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (1w, 1m, 3m, 6m, current_season, last_season)</param>
    /// <param name="side">Map side filter (blue, red, all)</param>
    /// <returns>Death positions response with coordinates, phase summary, and metadata</returns>
    Task<DeathPositionsResponse?> GetDeathPositionsAsync(
        string puuid, 
        string? queueType = null, 
        string? timeRange = null, 
        string? side = null);
}
