namespace Mongoose.Api.Core.Entities;

public class ParticipantDeathEvent : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public int MinuteMark { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int? KillerChampionId { get; set; }
    public int AssistCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
