using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

public sealed record PatternGroup(string Key, int Matches, int WinRate);

/// <summary>A column-chart pattern: its groups (only those with enough matches) and the weak spot, if any.</summary>
public sealed record GroupPattern(IReadOnlyList<PatternGroup> Groups, string? Weak);

public sealed record AfterResultGroup(int Pairs, int WinRate);

public sealed record AfterLossPattern(AfterResultGroup AfterWin, AfterResultGroup AfterLoss);

/// <summary>The three pattern cards; each is null when its rules aren't met.</summary>
public sealed record SoloPatterns(GroupPattern? Session, AfterLossPattern? AfterLoss, GroupPattern? Length);

/// <summary>
/// Session, after-a-loss and match-length patterns (features/solo-trends.spec.md FR26–FR28). Pure and static.
/// </summary>
public static class PatternCalculator
{
    // FR26: a new session starts when the next match begins 30 minutes or more after the last one ended.
    public static readonly TimeSpan SessionGap = TimeSpan.FromMinutes(30);

    // FR26/FR28: a group needs 3 matches and a chart needs 2 groups.
    public const int MinMatchesPerGroup = 3;
    public const int MinGroups = 2;

    // FR26: same rule as the Matches page's START_TIME_WEAK_GAP.
    public const int WeakGapPoints = 15;

    // FR27: each side needs 5 pairs; a 10-point drop after a loss counts as tilt.
    public const int MinPairs = 5;
    public const int TiltGapPoints = 10;

    private const int TwentyFiveMinutesSec = 1500;
    private const int ThirtyFiveMinutesSec = 2100;

    public static SoloPatterns Calculate(IReadOnlyList<SoloMatchRow> rows)
    {
        var sessions = GroupSessions(rows);
        return new SoloPatterns(SessionPattern(sessions), AfterLoss(sessions), LengthPattern(rows));
    }

    /// <summary>Splits matches (oldest first) into sessions.</summary>
    public static IReadOnlyList<IReadOnlyList<SoloMatchRow>> GroupSessions(IReadOnlyList<SoloMatchRow> rows)
    {
        var sessions = new List<IReadOnlyList<SoloMatchRow>>();
        List<SoloMatchRow>? current = null;
        SoloMatchRow? previous = null;

        foreach (var row in rows.OrderBy(r => r.GameStartTime))
        {
            var startsNew = previous == null
                || row.GameStartTime - (previous.GameStartTime + previous.DurationSec * 1000L) >= (long)SessionGap.TotalMilliseconds;
            if (startsNew)
            {
                current = [];
                sessions.Add(current);
            }

            current!.Add(row);
            previous = row;
        }

        return sessions;
    }

    private static GroupPattern? SessionPattern(IReadOnlyList<IReadOnlyList<SoloMatchRow>> sessions)
    {
        var byPosition = sessions
            .SelectMany(s => s.Select((row, i) => (Key: i >= 3 ? "4plus" : (i + 1).ToString(), Row: row)))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SoloMatchRow>)g.Select(x => x.Row).ToList());

        return BuildGroupPattern(["1", "2", "3", "4plus"], byPosition);
    }

    private static GroupPattern? LengthPattern(IReadOnlyList<SoloMatchRow> rows)
    {
        var byLength = rows
            .GroupBy(r => r.DurationSec < TwentyFiveMinutesSec ? "under25" : r.DurationSec <= ThirtyFiveMinutesSec ? "25to35" : "over35")
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SoloMatchRow>)g.ToList());

        return BuildGroupPattern(["under25", "25to35", "over35"], byLength);
    }

    private static GroupPattern? BuildGroupPattern(IReadOnlyList<string> order, IReadOnlyDictionary<string, IReadOnlyList<SoloMatchRow>> groups)
    {
        var shown = order
            .Where(k => groups.TryGetValue(k, out var g) && g.Count >= MinMatchesPerGroup)
            .Select(k => (Key: k, Rows: groups[k]))
            .ToList();
        if (shown.Count < MinGroups) return null;

        return new GroupPattern(
            shown.Select(g => new PatternGroup(g.Key, g.Rows.Count, SoloMath.WinRate(g.Rows))).ToList(),
            WeakSpot(shown));
    }

    /// <summary>The single lowest group, when it is at least 15 points below the other groups taken together.</summary>
    private static string? WeakSpot(IReadOnlyList<(string Key, IReadOnlyList<SoloMatchRow> Rows)> groups)
    {
        var rates = groups.Select(g => (g.Key, Rate: g.Rows.Count(r => r.Win) * 100.0 / g.Rows.Count)).ToList();
        var lowest = rates.MinBy(r => r.Rate);
        if (rates.Count(r => r.Rate == lowest.Rate) > 1) return null;

        var others = groups.Where(g => g.Key != lowest.Key).SelectMany(g => g.Rows).ToList();
        var othersRate = others.Count(r => r.Win) * 100.0 / others.Count;
        return othersRate - lowest.Rate >= WeakGapPoints ? lowest.Key : null;
    }

    private static AfterLossPattern? AfterLoss(IReadOnlyList<IReadOnlyList<SoloMatchRow>> sessions)
    {
        var afterWin = new List<SoloMatchRow>();
        var afterLoss = new List<SoloMatchRow>();
        foreach (var session in sessions)
        {
            for (var i = 1; i < session.Count; i++)
            {
                (session[i - 1].Win ? afterWin : afterLoss).Add(session[i]);
            }
        }

        if (afterWin.Count < MinPairs || afterLoss.Count < MinPairs) return null;

        return new AfterLossPattern(
            new AfterResultGroup(afterWin.Count, SoloMath.WinRate(afterWin)),
            new AfterResultGroup(afterLoss.Count, SoloMath.WinRate(afterLoss)));
    }
}
