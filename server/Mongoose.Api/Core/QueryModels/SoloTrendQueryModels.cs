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
    string? RankAfter = null);

/// <summary>
/// Matches this season per ranked queue, used to pick the page's default queue (FR2).
/// </summary>
public sealed record SoloQueueCounts(int RankedSolo, int RankedFlex);
