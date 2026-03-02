using static Mongoose.Api.Application.DTOs.Solo.RadarChartDto;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for solo radar chart data.
/// Provides normalized and raw values for key solo performance dimensions.
/// </summary>
public interface IRadarChartRepository
{
    /// <summary>
    /// Gets radar chart data for a player.
    /// Supports optional queue and time range filtering.
    /// </summary>
    /// <param name="puuid">Player PUUID.</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all).</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m).</param>
    /// <returns>Radar chart response or null when the player has no matching games.</returns>
    Task<RadarChartResponse?> GetRadarChartAsync(string puuid, string? queueType = null, string? timeRange = null);

    /// <summary>
    /// Gets radar chart data for one or more players.
    /// Supports optional queue and time range filtering.
    /// </summary>
    Task<RadarChartResponse?> GetRadarChartAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
}