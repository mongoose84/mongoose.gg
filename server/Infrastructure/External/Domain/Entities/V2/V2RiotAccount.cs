using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2RiotAccount : EntityBase
{
    public string Puuid { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string TagLine { get; set; } = string.Empty;
    public string SummonerName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string SyncStatus { get; set; } = "pending";
    public int SyncProgress { get; set; }
    public int SyncTotal { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
