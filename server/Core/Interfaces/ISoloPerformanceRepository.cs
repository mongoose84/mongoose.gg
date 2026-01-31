using static RiotProxy.Application.DTOs.SoloPerformanceDto;

namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Repository for solo performance statistics.
/// Provides comprehensive performance data including overall stats, champion pool,
/// performance by phase, role breakdown, and death efficiency.
/// </summary>
public interface ISoloPerformanceRepository
{
    /// <summary>
    /// Get comprehensive solo performance data for a player.
    /// Includes: overall stats, champion pool, performance by phase, role breakdown, death efficiency.
    /// Supports optional queue filtering and time range filtering.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <returns>Performance response or null if no data found</returns>
    Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(string puuid, string? queueType = null, string? timeRange = null);
}

