using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2TeamObjective : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int DragonsTaken { get; set; }
    public int HeraldsTaken { get; set; }
    public int BaronsTaken { get; set; }
    public int TowersTaken { get; set; }
    public DateTime CreatedAt { get; set; }
}
