using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2ParticipantMetric : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public decimal KillParticipationPct { get; set; }
    public decimal DamageSharePct { get; set; }
    public int DamageTaken { get; set; }
    public int DamageMitigated { get; set; }
    public int VisionScore { get; set; }
    public decimal VisionPerMin { get; set; }
    public int DeathsPre10 { get; set; }
    public int Deaths10To20 { get; set; }
    public int Deaths20To30 { get; set; }
    public int Deaths30Plus { get; set; }
    public int? FirstDeathMinute { get; set; }
    public int? FirstKillParticipationMinute { get; set; }
    public DateTime CreatedAt { get; set; }
}
