using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2DuoMetric : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public long ParticipantId1 { get; set; }
    public long ParticipantId2 { get; set; }
    public int? EarlyGoldDelta10 { get; set; }
    public int? EarlyGoldDelta15 { get; set; }
    public decimal? AssistSynergyPct { get; set; }
    public decimal? SharedObjectiveParticipationPct { get; set; }
    public bool? WinWhenAheadAt15 { get; set; }
    public DateTime CreatedAt { get; set; }
}
