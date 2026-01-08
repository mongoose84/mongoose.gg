using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2Subscription : EntityBase
{
    public long SubscriptionId { get; set; }
    public long UserId { get; set; }
    public string Tier { get; set; } = "free";
    public string Status { get; set; } = "active";
    public string? MollieSubscriptionId { get; set; }
    public string? MolliePlanId { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public bool IsFoundingMember { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
