using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Picks the stat that decided a match, by scoring five early-game and involvement stats against
/// the player's own usual in that role (see <c>features/what-decided-it.spec.md</c> FR1, FR3–FR9).
/// Pure and static; replaces the retired TrendBadgeCalculator.
/// </summary>
public static class DecidingStatCalculator
{
    // FR5: a stat "stands out" once it is a full standard deviation from usual.
    private const double StandOutThreshold = 1.0;

    // FR8: only worth fixing when the shortfall is at least half a standard deviation — small dips
    // are normal variance, not something to change next match.
    private const double FixThreshold = -0.5;

    // FR3: fewer than 5 matches makes the average/stddev too noisy to name with confidence.
    private const int MinUsualMatches = 5;

    // UI/UX: the card shows at most three meters.
    private const int MaxMeters = 3;

    // FR1: a match needs to reach 10 minutes before gold/CS/deaths-before-10 mean anything.
    private const int MinDurationSecForEarlyStats = 600;

    // FR1: kill participation is meaningless when the game barely had any kills.
    private const int MinTeamKillsForKillParticipation = 5;

    // FR1 spread floors: stop a very steady stat (deaths before 10 that are almost always 0) from
    // producing huge scores off tiny variance.
    private const double GoldLeadAt10Floor = 400;
    private const double CsAt10Floor = 8;
    private const double DeathsBefore10Floor = 0.7;
    private const double KillParticipationFloor = 8;
    private const double VisionPerMinFloor = 0.25;

    private const string Utility = "UTILITY";
    private const string UnknownRole = "UNKNOWN";

    // FR1 table order — also FR6's tie-break order.
    private static readonly string[] StatOrder =
    [
        "goldLeadAt10",
        "csAt10",
        "deathsBefore10",
        "killParticipation",
        "visionPerMin"
    ];

    public static DecidingStat? Compute(DecidingStatInput input, IReadOnlyDictionary<string, StatUsual> usuals)
    {
        // FR9: the card is left out entirely for a remake, a non-Summoner's-Rift queue or an unknown role.
        if (input.IsRemake) return null;
        if (!GameConstants.SummonersRiftQueueIds.Contains(input.QueueId)) return null;
        if (string.Equals(input.Role, UnknownRole, StringComparison.Ordinal)) return null;

        var scored = ComputeScores(input, usuals);
        if (scored.Count == 0) return null; // FR9: no stat eligible

        var usualMatches = usuals.Values.Select(u => u.Matches).DefaultIfEmpty(0).Max();
        var chosen = ChooseDecidingStat(scored, input.Win);

        var outcome = chosen == null ? "none" : (chosen.Score > 0 ? "strength" : "shortfall");
        var meters = BuildMeters(scored, chosen?.Stat);
        var fix = BuildFix(scored);

        return new DecidingStat(outcome, chosen?.Stat, meters, fix, usualMatches);
    }

    /// <summary>FR1, FR3, FR4: eligibility gates plus the score for every candidate stat that qualifies.</summary>
    private static List<ScoredStat> ComputeScores(DecidingStatInput input, IReadOnlyDictionary<string, StatUsual> usuals)
    {
        var result = new List<ScoredStat>();

        if (input.GameDurationSec >= MinDurationSecForEarlyStats)
        {
            if (TryScore("goldLeadAt10", input.GoldLeadAt10, usuals, GoldLeadAt10Floor, lowerIsBetter: false, out var gold))
                result.Add(gold!);

            if (!string.Equals(input.Role, Utility, StringComparison.Ordinal)
                && TryScore("csAt10", input.CsAt10, usuals, CsAt10Floor, lowerIsBetter: false, out var cs))
                result.Add(cs!);

            if (TryScore("deathsBefore10", input.DeathsBefore10, usuals, DeathsBefore10Floor, lowerIsBetter: true, out var deaths))
                result.Add(deaths!);
        }

        if (input.TeamKills >= MinTeamKillsForKillParticipation
            && TryScore("killParticipation", input.KillParticipation, usuals, KillParticipationFloor, lowerIsBetter: false, out var kp))
            result.Add(kp!);

        if (TryScore("visionPerMin", input.VisionPerMin, usuals, VisionPerMinFloor, lowerIsBetter: false, out var vision))
            result.Add(vision!);

        return result;
    }

    /// <summary>FR2–FR4: a stat scores only with a value and a usual sample of at least <see cref="MinUsualMatches"/>.</summary>
    private static bool TryScore(
        string stat,
        double? value,
        IReadOnlyDictionary<string, StatUsual> usuals,
        double floor,
        bool lowerIsBetter,
        out ScoredStat? scored)
    {
        scored = null;
        if (value is not double v) return false;
        if (!usuals.TryGetValue(stat, out var usual) || usual.Matches < MinUsualMatches) return false;

        var spread = Math.Max(usual.StdDev, floor);
        var rawScore = (v - usual.Average) / spread;
        var score = lowerIsBetter ? -rawScore : rawScore; // FR4: deaths' sign is flipped so positive always means "better"

        scored = new ScoredStat(stat, v, usual.Average, score);
        return true;
    }

    /// <summary>FR6: win picks the top strength (or the worst shortfall, "won despite"); loss the mirror.</summary>
    private static ScoredStat? ChooseDecidingStat(IReadOnlyList<ScoredStat> scored, bool win)
    {
        var standoutStrengths = scored.Where(s => s.Score >= StandOutThreshold)
            .OrderByDescending(s => s.Score).ThenBy(StatOrderIndex).ToList();
        var standoutShortfalls = scored.Where(s => s.Score <= -StandOutThreshold)
            .OrderBy(s => s.Score).ThenBy(StatOrderIndex).ToList();

        if (win)
        {
            if (standoutStrengths.Count > 0) return standoutStrengths[0];
            if (standoutShortfalls.Count > 0) return standoutShortfalls[0]; // "won despite"
            return null;
        }

        if (standoutShortfalls.Count > 0) return standoutShortfalls[0];
        if (standoutStrengths.Count > 0) return standoutStrengths[0]; // "you did your part"
        return null;
    }

    /// <summary>FR7: the deciding stat first, then the rest by |score| descending, capped at three.</summary>
    private static IReadOnlyList<DecidingStatMeter> BuildMeters(IReadOnlyList<ScoredStat> scored, string? chosenStat)
    {
        var ordered = new List<ScoredStat>();
        var chosen = chosenStat != null ? scored.FirstOrDefault(s => s.Stat == chosenStat) : null;
        if (chosen != null) ordered.Add(chosen);

        var rest = scored
            .Where(s => chosen == null || s.Stat != chosen.Stat)
            .OrderByDescending(s => Math.Abs(s.Score))
            .ThenBy(StatOrderIndex);
        ordered.AddRange(rest);

        return ordered
            .Take(MaxMeters)
            .Select(s => new DecidingStatMeter(s.Stat, Round(s.Value), Round(s.Usual), Round(s.Score)))
            .ToList();
    }

    /// <summary>FR8: the lowest-scoring eligible stat, only when it fell far enough short to fix.</summary>
    private static DecidingStatFix? BuildFix(IReadOnlyList<ScoredStat> scored)
    {
        var worst = scored.OrderBy(s => s.Score).ThenBy(StatOrderIndex).FirstOrDefault();
        if (worst == null || worst.Score > FixThreshold) return null;
        return new DecidingStatFix(worst.Stat, Round(worst.Value), Round(worst.Usual), Round(worst.Score));
    }

    private static int StatOrderIndex(ScoredStat stat) => Array.IndexOf(StatOrder, stat.Stat);

    private static double Round(double value) => Math.Round(value, 2);

    private sealed record ScoredStat(string Stat, double Value, double Usual, double Score);
}
