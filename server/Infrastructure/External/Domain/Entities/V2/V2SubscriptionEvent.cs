using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2SubscriptionEvent : EntityBase
{
    public long EventId { get; set; }
    public long SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OldTier { get; set; }
    public string? NewTier { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? MollieEventId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
