namespace Mongoose.Api.Core.Entities;

/// <summary>
/// An objective a team took, with its time: dragon, baron, herald, grubs, tower or inhibitor
/// (match_objective_events, from the timeline).
/// </summary>
public class MatchObjectiveEvent : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    /// <summary>The team that took it (100 or 200).</summary>
    public int TeamId { get; set; }
    public string Type { get; set; } = string.Empty;
    /// <summary>Dragon kind or tower lane.</summary>
    public string? Subtype { get; set; }
    public int TimestampSec { get; set; }
    /// <summary>Killer's Riot participantId (1-10), when a champion.</summary>
    public int? KillerParticipantId { get; set; }
    public DateTime CreatedAt { get; set; }
}
