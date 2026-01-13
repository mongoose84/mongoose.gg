namespace RiotProxy.External.Domain.Entities;

public class Match : EntityBase
{
    public string MatchId { get; set; } = string.Empty;
    public int QueueId { get; set; }
    public int GameDurationSec { get; set; }
    public long GameStartTime { get; set; }
    public string PatchVersion { get; set; } = string.Empty;
    public string? SeasonCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
