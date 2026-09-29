using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>One counted death: where, when, how and what it cost.</summary>
public sealed record ClassifiedDeath(string Region, string Phase, string How, string? Cost);

/// <summary>
/// When a death happened, how, and whether it cost an objective (features/solo-trends.spec.md
/// FR 34-36). Pure and static.
/// </summary>
public static class DeathClassifier
{
    public const string Early = "early";
    public const string Mid = "mid";
    public const string Late = "late";

    public const string Teamfight = "teamfight";
    public const string Ganked = "ganked";
    public const string Alone = "alone";
    public const string Other = "other";

    // FR 34: early before 14:00, mid 14:00-24:59, late 25:00 on
    public const int MidStartSec = 14 * 60;
    public const int LateStartSec = 25 * 60;

    // FR 35: 3 or more on the kill and 2 or more allies close is a teamfight
    public const int TeamfightInvolved = 3;
    public const int TeamfightAllies = 2;

    // FR 36: an enemy objective within 60 seconds after the death
    public const int CostWindowSec = 60;

    /// <summary>The objectives a death can cost; Void Grubs and inhibitors don't count.</summary>
    public static readonly IReadOnlyList<string> CostTypes = ["dragon", "tower", "baron", "herald"];

    /// <summary>
    /// FR 31: a death counts once its detail is known (time and allies nearby, which the timeline
    /// always gives). The killer is not required: an execute has none.
    /// </summary>
    public static bool IsCounted(SoloDeathRow death) => death.TimestampSec.HasValue && death.AlliesNearby.HasValue;

    public static string Phase(int timestampSec) =>
        timestampSec < MidStartSec ? Early : timestampSec < LateStartSec ? Mid : Late;

    /// <summary>
    /// FR 35, exclusive and in order: teamfight, ganked in lane, caught alone, other.
    /// <paramref name="participants"/> are the match's participants by Riot participantId.
    /// </summary>
    public static string How(SoloDeathRow death, string region, IReadOnlyDictionary<int, MatchParticipantRole> participants)
    {
        var involved = death.AssistingParticipantIds.ToList();
        if (death.KillerParticipantId is { } killer) involved.Insert(0, killer);

        if (involved.Count >= TeamfightInvolved && death.AlliesNearby >= TeamfightAllies) return Teamfight;

        if (death.TimestampSec < MidStartSec && MapRegions.LaneOf(death.VictimRole).Contains(region))
        {
            // Someone other than the lane opponent took part: a gank
            var laneOpponent = participants.Values.FirstOrDefault(p => p.TeamId != death.VictimTeamId && p.Role == death.VictimRole);
            if (involved.Any(id => laneOpponent is null || id != laneOpponent.ParticipantId)) return Ganked;
        }

        if (death.AlliesNearby == 0) return Alone;
        return Other;
    }

    /// <summary>FR 36: the first objective the enemy took within 60 seconds after the death; null when none.</summary>
    public static string? Cost(SoloDeathRow death, IEnumerable<SoloObjectiveRow> matchObjectives)
    {
        var at = death.TimestampSec!.Value;
        return matchObjectives
            .Where(o => o.TeamId != death.VictimTeamId
                && CostTypes.Contains(o.Type)
                && o.TimestampSec >= at
                && o.TimestampSec <= at + CostWindowSec)
            .OrderBy(o => o.TimestampSec)
            .Select(o => o.Type)
            .FirstOrDefault();
    }

    public static ClassifiedDeath Classify(
        SoloDeathRow death,
        IReadOnlyDictionary<int, MatchParticipantRole> participants,
        IEnumerable<SoloObjectiveRow> matchObjectives)
    {
        var (u, v) = MapRegions.Normalize(death.PositionX, death.PositionY, death.VictimTeamId);
        var region = MapRegions.Classify(u, v);
        return new ClassifiedDeath(region, Phase(death.TimestampSec!.Value), How(death, region, participants), Cost(death, matchObjectives));
    }
}
