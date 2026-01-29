namespace RiotProxy.Core.Entities;

public class RiotAccount : EntityBase
{
    public string Puuid { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string TagLine { get; set; } = string.Empty;
    public string SummonerName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? SummonerId { get; set; }
    public bool IsPrimary { get; set; }
    public string SyncStatus { get; set; } = "pending";
    public int SyncProgress { get; set; }
    public int SyncTotal { get; set; }
    public int? ProfileIconId { get; set; }
    public int? SummonerLevel { get; set; }
    public string? SoloTier { get; set; }
    public string? SoloRank { get; set; }
    public int? SoloLp { get; set; }
    public string? FlexTier { get; set; }
    public string? FlexRank { get; set; }
    public int? FlexLp { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

