namespace RiotProxy.External.Domain.Entities;

public class Participant : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public string Puuid { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string? Role { get; set; }
    public string? Lane { get; set; }
    public int ChampionId { get; set; }
    public string ChampionName { get; set; } = string.Empty;
    public bool Win { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int CreepScore { get; set; }
    public int GoldEarned { get; set; }
    public int TimeDeadSec { get; set; }
    /// <summary>LP after game completion (ranked matches only, null otherwise)</summary>
    public int? LpAfter { get; set; }
    /// <summary>Tier after game completion (e.g., "SILVER") - ranked matches only</summary>
    public string? TierAfter { get; set; }
    /// <summary>Rank/division after game (e.g., "IV") - ranked matches only</summary>
    public string? RankAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}
