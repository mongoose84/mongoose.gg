namespace Mongoose.Api.Core.Entities;

public class ParticipantDeathEvent : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public int MinuteMark { get; set; }
    /// <summary>Seconds into the match; null for deaths synced before migration 004 until backfilled.</summary>
    public int? TimestampSec { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int? KillerChampionId { get; set; }
    /// <summary>Killer's Riot participantId (1-10); null for an execute.</summary>
    public int? KillerParticipantId { get; set; }
    /// <summary>Assisters' Riot participantIds, comma-separated ("8,9"); empty when nobody assisted.</summary>
    public string? AssistingParticipantIds { get; set; }
    /// <summary>The victim's allies within 2,000 units in the closest participant frame.</summary>
    public int? AlliesNearby { get; set; }
    public int AssistCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
