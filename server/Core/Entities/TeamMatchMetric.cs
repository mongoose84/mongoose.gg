namespace RiotProxy.Core.Entities;

public class TeamMatchMetric : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int? GoldLeadAt15 { get; set; }
    public int? LargestGoldLead { get; set; }
    public int? GoldSwingPost20 { get; set; }
    public bool? WinWhenAheadAt20 { get; set; }
    public DateTime CreatedAt { get; set; }
}

