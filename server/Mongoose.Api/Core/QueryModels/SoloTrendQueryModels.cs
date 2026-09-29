namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// The match range of the Solo page (features/solo-trends.spec.md FR3). Ranges count matches, never days.
/// </summary>
public enum SoloRange
{
    Last20,
    Last50,
    Season
}

/// <summary>
/// One match of the player on the Solo page, with everything the Solo trend rules read.
/// Nullable fields are missing when the source row doesn't exist (no checkpoint at 15, no metrics row).
/// <see cref="LpChange"/> is known only when this match and the previous one in its queue both have a rank.
/// </summary>
public sealed record SoloMatchRow(
    string MatchId,
    long GameStartTime,
    int DurationSec,
    int QueueId,
    string Role,
    int ChampionId,
    string ChampionName,
    bool Win,
    int Deaths,
    int CreepScore,
    int? GoldDiffAt15,
    int? DragonsParticipated,
    int? TeamDragons,
    double? VisionPerMin,
    double? KillParticipationPct,
    int TeamKills,
    int? LpAfter = null,
    string? TierAfter = null,
    string? RankAfter = null,
    int? LpChange = null);

/// <summary>
/// Matches this season per ranked queue, used to pick the page's default queue (FR2).
/// </summary>
public sealed record SoloQueueCounts(int RankedSolo, int RankedFlex);

/// <summary>
/// One death of the player (features/solo-trends.spec.md FR 31-36). Detail fields are null for
/// deaths synced before migration 004 until the backfill reaches them.
/// </summary>
public sealed record SoloDeathRow(
    string MatchId,
    int VictimTeamId,
    string VictimRole,
    int? VictimParticipantId,
    int PositionX,
    int PositionY,
    int? TimestampSec,
    int? KillerParticipantId,
    IReadOnlyList<int> AssistingParticipantIds,
    int? AlliesNearby);

/// <summary>A participant of a match by Riot participantId, for "was it the lane opponent" (FR 35).</summary>
public sealed record MatchParticipantRole(string MatchId, int ParticipantId, int TeamId, string Role);

/// <summary>An objective a team took, for "did the death cost an objective" (FR 36).</summary>
public sealed record SoloObjectiveRow(string MatchId, int TeamId, string Type, int TimestampSec);

/// <summary>The player's deaths in a set of matches, with those matches' participants and objectives.</summary>
public sealed record SoloDeathData(
    IReadOnlyList<SoloDeathRow> Deaths,
    IReadOnlyList<MatchParticipantRole> Participants,
    IReadOnlyList<SoloObjectiveRow> Objectives);
