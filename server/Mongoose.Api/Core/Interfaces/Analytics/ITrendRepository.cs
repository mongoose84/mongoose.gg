namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Match activity for the heatmap: matches played per day.
/// </summary>
public interface ITrendRepository
{
    /// <summary>
    /// Get daily match counts for the past N days for heatmap display.
    /// Returns a dictionary keyed by date (YYYY-MM-DD) with match count values.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="daysBack">Number of days to look back (default: 91)</param>
    /// <returns>Dictionary of date strings to match counts</returns>
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91);
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(IReadOnlyList<string> puuids, int daysBack = 91);
}

