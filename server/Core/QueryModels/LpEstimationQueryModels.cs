namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Lightweight model for LP estimation - only the fields needed for the algorithm.
/// </summary>
public class LpEstimationMatch
{
    public string MatchId { get; set; } = string.Empty;
    public string Puuid { get; set; } = string.Empty;
    public bool Win { get; set; }
    public int GameDurationSec { get; set; }
    public int? LpAfter { get; set; }
    public string? TierAfter { get; set; }
    public string? RankAfter { get; set; }
    public bool IsLpEstimated { get; set; }
}

