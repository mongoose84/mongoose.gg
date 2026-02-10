using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for trend-related statistics.
/// Provides winrate trends, LP trends, and match activity data.
/// </summary>
public interface ITrendRepository
{
    /// <summary>
    /// Get winrate trend data as a rolling 20-game average for chart display.
    /// Returns an array of data points with gameIndex, winRate, and timestamp.
    /// When limit is specified, returns the most recent N games at full resolution.
    /// When limit is null, returns all games with downsampling if over 100 data points.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <param name="limit">Maximum number of most recent games to return (null for all with downsampling)</param>
    /// <returns>Array of winrate trend points</returns>
    Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);

    /// <summary>
    /// Get LP trend data for ranked matches with LP data available.
    /// Returns LP points ordered from oldest to newest for chart visualization.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, or null for both)</param>
    /// <param name="limit">Maximum number of data points to return</param>
    /// <returns>List of LP trend points ordered oldest to newest</returns>
    Task<IList<LpTrendPoint>> GetLpTrendAsync(string puuid, string? queueType = null, int limit = 100);

    /// <summary>
    /// Get LP trend data for both ranked solo and ranked flex queues separately.
    /// Returns a tuple with separate arrays for each queue type.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="limit">Maximum number of data points to return per queue</param>
    /// <returns>Tuple containing (rankedSolo, rankedFlex) LP trend arrays</returns>
    Task<(LpTrendPoint[] RankedSolo, LpTrendPoint[] RankedFlex)> GetLpTrendBothQueuesAsync(string puuid, int limit = 100);

    /// <summary>
    /// Get daily match counts for the past N days for heatmap display.
    /// Returns a dictionary keyed by date (YYYY-MM-DD) with match count values.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="daysBack">Number of days to look back (default: 91)</param>
    /// <returns>Dictionary of date strings to match counts</returns>
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91);
}

