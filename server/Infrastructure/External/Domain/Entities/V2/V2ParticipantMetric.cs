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
    /// <summary>
    /// Death counts bucketed by game time. These property names intentionally use a
    /// readable interval format (e.g. <c>Deaths10To20</c>) instead of mirroring the
    /// exact database column names (e.g. <c>deaths_10_20</c>). Follow this pattern
    /// for any additional time-bucketed death metrics to keep naming consistent.
    /// </summary>
    public int DeathsPre10 { get; set; }
    public int Deaths10To20 { get; set; }
    public int Deaths20To30 { get; set; }
    public int Deaths30Plus { get; set; }
    public int? FirstDeathMinute { get; set; }
    public int? FirstKillParticipationMinute { get; set; }
    public DateTime CreatedAt { get; set; }
}
