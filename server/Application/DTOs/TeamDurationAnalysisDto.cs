namespace RiotProxy.Application.DTOs;

public static class TeamDurationAnalysisDto
{
    /// <summary>
    /// Response containing team win rate by game duration buckets.
    /// </summary>
    public record TeamDurationAnalysisResponse(
        IList<DurationBucket> Buckets,
        string BestDuration,
        double BestWinRate
    );

    /// <summary>
    /// Win rate data for a specific duration range.
    /// </summary>
    public record DurationBucket(
        string Label,
        int MinMinutes,
        int MaxMinutes,
        int GamesPlayed,
        int Wins,
        double WinRate
    );
}

