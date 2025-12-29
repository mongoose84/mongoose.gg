namespace RiotProxy.Application.DTOs;

public static class TeamWinRateTrendDto
{
    /// <summary>
    /// Response containing team win rate trend data over recent games.
    /// </summary>
    public record TeamWinRateTrendResponse(
        IList<TrendDataPoint> DataPoints,
        double OverallWinRate,
        double RecentWinRate,
        string TrendDirection
    );

    /// <summary>
    /// A single data point in the win rate trend.
    /// </summary>
    public record TrendDataPoint(
        int GameNumber,
        bool Win,
        double RollingWinRate,
        DateTime GameDate
    );
}

