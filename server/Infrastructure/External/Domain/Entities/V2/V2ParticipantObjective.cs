using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2ParticipantObjective : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public int DragonsParticipated { get; set; }
    public int HeraldsParticipated { get; set; }
    public int BaronsParticipated { get; set; }
    public int TowersParticipated { get; set; }
    public DateTime CreatedAt { get; set; }
}
