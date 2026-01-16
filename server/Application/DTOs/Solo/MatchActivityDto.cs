namespace RiotProxy.Application.DTOs.Solo;

/// <summary>
/// Response DTO for match activity heatmap data.
/// Returns daily match counts for the past 3 months.
/// </summary>
public record MatchActivityResponse(
    /// <summary>Daily match counts keyed by date (YYYY-MM-DD format)</summary>
    Dictionary<string, int> DailyMatchCounts,
    /// <summary>Start date of the activity period (YYYY-MM-DD)</summary>
    string StartDate,
    /// <summary>End date of the activity period (YYYY-MM-DD)</summary>
    string EndDate,
    /// <summary>Total matches in the period</summary>
    int TotalMatches
);

