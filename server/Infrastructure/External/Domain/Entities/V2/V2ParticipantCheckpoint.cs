using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2ParticipantCheckpoint : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public int MinuteMark { get; set; }
    public int Gold { get; set; }
    public int Cs { get; set; }
    public int Xp { get; set; }
    public int? GoldDiffVsLane { get; set; }
    public int? CsDiffVsLane { get; set; }
    public bool? IsAhead { get; set; }
    public DateTime CreatedAt { get; set; }
}
