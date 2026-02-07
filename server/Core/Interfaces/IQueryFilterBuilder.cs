using MySqlConnector;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Represents the result of resolving a time range filter.
/// Contains all information needed to build SQL filters and bind parameters.
/// </summary>
public record TimeRangeFilter(
    /// <summary>The start DateTime for relative time ranges (1w, 1m, 3m, 6m), null for season-based or "all"</summary>
    DateTime? TimeRangeStart,
    /// <summary>The season code for season-based filters (current_season, last_season), null otherwise</summary>
    string? SeasonCode,
    /// <summary>The normalized time range string (e.g., "1w", "current_season", "all")</summary>
    string NormalizedTimeRange
);

/// <summary>
/// Service for building SQL query filters for queue types and time ranges.
/// Centralizes filter logic to eliminate duplication across repositories.
/// </summary>
public interface IQueryFilterBuilder
{
    /// <summary>
    /// Validates and normalizes a queue type string.
    /// Returns "all" for invalid or null inputs.
    /// </summary>
    /// <param name="queueType">Raw queue type (ranked_solo, ranked_flex, normal, aram, all)</param>
    /// <returns>Normalized queue type string</returns>
    string ValidateQueueType(string? queueType);

    /// <summary>
    /// Builds a SQL WHERE clause fragment for queue filtering.
    /// Assumes the matches table is aliased as 'm'.
    /// </summary>
    /// <param name="queueType">Normalized queue type from ValidateQueueType</param>
    /// <returns>SQL fragment like "AND m.queue_id = 420" or empty string for "all"</returns>
    string BuildQueueFilter(string queueType);

    /// <summary>
    /// Resolves a time range string into filter components.
    /// Supports relative ranges (1w, 1m, 3m, 6m) and seasonal ranges (current_season, last_season).
    /// </summary>
    /// <param name="timeRange">Raw time range string</param>
    /// <returns>TimeRangeFilter with resolved components</returns>
    Task<TimeRangeFilter> ResolveTimeRangeAsync(string? timeRange);

    /// <summary>
    /// Builds a SQL WHERE clause fragment for time range filtering.
    /// Assumes the matches table is aliased as 'm'.
    /// </summary>
    /// <param name="filter">TimeRangeFilter from ResolveTimeRangeAsync</param>
    /// <returns>SQL fragment like "AND m.game_start_time >= @startTime" or empty string</returns>
    string BuildTimeRangeFilter(TimeRangeFilter filter);

    /// <summary>
    /// Adds time range parameters to a MySqlCommand based on the filter.
    /// Adds @startTime for relative ranges and @season for seasonal ranges.
    /// </summary>
    /// <param name="cmd">The MySqlCommand to add parameters to</param>
    /// <param name="filter">TimeRangeFilter from ResolveTimeRangeAsync</param>
    void AddTimeRangeParameters(MySqlCommand cmd, TimeRangeFilter filter);
}

