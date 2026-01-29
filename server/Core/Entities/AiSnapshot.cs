namespace RiotProxy.Core.Entities;

public class AiSnapshot : EntityBase
{
    public long Id { get; set; }
    public string Puuid { get; set; } = string.Empty;
    public string ContextType { get; set; } = "solo";
    public string? ContextPuuidsJson { get; set; }
    public int? QueueId { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public string? GoalsJson { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

