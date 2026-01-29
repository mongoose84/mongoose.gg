namespace RiotProxy.Core.Entities;

/// <summary>
/// Represents a point-in-time snapshot of a player's LP/rank.
/// Recorded during each sync to track rank progression over time.
/// Not tied to specific matches since Riot API only provides current rank.
/// </summary>
public class LpSnapshot : EntityBase
{
    public long Id { get; set; }
    public string Puuid { get; set; } = string.Empty;
    /// <summary>Queue type: RANKED_SOLO_5x5 or RANKED_FLEX_SR</summary>
    public string QueueType { get; set; } = string.Empty;
    /// <summary>Tier (e.g., IRON, BRONZE, SILVER, GOLD, PLATINUM, EMERALD, DIAMOND, MASTER, GRANDMASTER, CHALLENGER)</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Division within tier (I, II, III, IV) - not applicable for Master+</summary>
    public string Division { get; set; } = string.Empty;
    /// <summary>League points (0-100, or higher in Master+)</summary>
    public int Lp { get; set; }
    /// <summary>When this LP value was recorded (sync time)</summary>
    public DateTime RecordedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

