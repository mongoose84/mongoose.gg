using RiotProxy.External.Domain.Entities;

namespace RiotProxy.External.Domain.Entities.V2;

public class V2Season : EntityBase
{
    public string SeasonCode { get; set; } = string.Empty;
    public string PatchVersion { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
