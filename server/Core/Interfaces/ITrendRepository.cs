using static Mongoose.Api.Application.DTOs.TrendDto;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for trend-related statistics.
/// Provides winrate trends and match activity data.
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
    /// Get gold at 15 minutes trend data for chart display.
    /// Returns an array of data points with player gold, opponent gold, and differential at 15-minute mark.
    /// When limit is specified, returns the most recent N games at full resolution.
    /// When limit is null, returns all games with downsampling if over 100 data points.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <param name="limit">Maximum number of most recent games to return (null for all with downsampling)</param>
    /// <returns>Array of gold at 15 trend points</returns>
    Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);

    /// <summary>
    /// Get CS per minute trend data for chart display.
    /// Returns an array of data points with player's farming efficiency over time.
    /// When limit is specified, returns the most recent N games at full resolution.
    /// When limit is null, returns all games with downsampling if over 100 data points.
    /// Filters out games shorter than 15 minutes for accuracy.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <param name="limit">Maximum number of most recent games to return (null for all with downsampling)</param>
    /// <returns>Array of CS per minute trend points</returns>
    Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);

    /// <summary>
    /// Get deaths over time trend data for chart display.
    /// Returns an array of data points with death counts and rolling 10-game average.
    /// When limit is specified, returns the most recent N games at full resolution.
    /// When limit is null, returns all games with downsampling if over 100 data points.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <param name="timeRange">Time range filter (current_season, last_season, 1w, 1m, 3m, 6m)</param>
    /// <param name="limit">Maximum number of most recent games to return (null for all with downsampling)</param>
    /// <returns>Tuple containing array of deaths trend points and summary statistics</returns>
    Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);

    /// <summary>
    /// Get daily match counts for the past N days for heatmap display.
    /// Returns a dictionary keyed by date (YYYY-MM-DD) with match count values.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="daysBack">Number of days to look back (default: 91)</param>
    /// <returns>Dictionary of date strings to match counts</returns>
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91);
}

