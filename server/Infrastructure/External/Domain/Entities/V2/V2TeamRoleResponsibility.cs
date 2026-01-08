using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2TeamRoleResponsibility : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal DeathsSharePct { get; set; }
    public decimal GoldSharePct { get; set; }
    public decimal DamageSharePct { get; set; }
    public DateTime CreatedAt { get; set; }
}
