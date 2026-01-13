namespace RiotProxy.External.Domain.Entities;

public class Season : EntityBase
{
    public string SeasonCode { get; set; } = string.Empty;
    public string PatchVersion { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
