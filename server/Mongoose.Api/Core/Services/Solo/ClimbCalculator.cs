using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

public sealed record LadderPoint(int Index, int Ladder);

/// <summary>A promotion or demotion at match <see cref="Index"/>, with the rank it reached.</summary>
public sealed record LadderEvent(int Index, string Kind, string Tier, string? Division);

/// <summary>The most LP lost over a run of consecutive losses, reported at the run's last match.</summary>
public sealed record LpDrop(int Index, int Lp, int Losses);

public sealed record LpClimb(
    int Net,
    LadderRank Start,
    LadderRank End,
    IReadOnlyList<LadderPoint> Points,
    IReadOnlyList<LadderEvent> Events,
    LpDrop? BiggestDrop);

public sealed record WinRatePoint(int Index, int Rate);

public sealed record WinRateClimb(int Was, int Now, IReadOnlyList<WinRatePoint> Points);

/// <summary>One champion's row: <see cref="Value"/> is LP won or lost (LP mode) or net wins (win-rate mode).</summary>
public sealed record ChampionClimb(int ChampionId, string ChampionName, int Matches, int Wins, int Value);

public sealed record SoloClimb(
    string Mode,
    int Wins,
    int Losses,
    LpClimb? Lp,
    WinRateClimb? WinRate,
    IReadOnlyList<ChampionClimb> Champions,
    IReadOnlyList<string> ChampionsLeftOut,
    LadderRank? Rank);

/// <summary>
/// The climb card, the headline numbers and LP per champion (features/solo-trends.spec.md
/// FR 5–12, FR 24). Rows are one queue's matches in range, oldest first. Pure and static.
/// </summary>
public static class ClimbCalculator
{
    public const string Promotion = "promotion";
    public const string Demotion = "demotion";

    // FR 11: a drop needs 2 losses in a row and at least 40 LP to be worth pointing at.
    public const int MinDropLosses = 2;
    public const int MinDropLp = 40;

    // FR 12: a 10-match rolling win rate; the card needs 20 matches.
    public const int Window = 10;
    public const int MinMatchesForWinRate = 20;

    // FR 24: champions need 3 matches; the card shows 5.
    public const int MinChampionMatches = 3;
    public const int MaxChampions = 5;

    public static SoloClimb Calculate(IReadOnlyList<SoloMatchRow> rows, string queueType, bool singleAccount)
    {
        var mode = LpCoverageRule.Mode(queueType, singleAccount, rows);
        var wins = rows.Count(r => r.Win);
        var (champions, leftOut) = Champions(rows, mode);

        return new SoloClimb(
            mode,
            wins,
            rows.Count - wins,
            mode == LpCoverageRule.LpMode ? LpClimb(rows) : null,
            mode == LpCoverageRule.WinRateMode ? WinRate(rows) : null,
            champions,
            leftOut,
            CurrentRank(rows, queueType, singleAccount));
    }

    /// <summary>FR 10–11: the ladder line, net LP, promotions and the biggest drop.</summary>
    public static LpClimb? LpClimb(IReadOnlyList<SoloMatchRow> rows)
    {
        var known = rows
            .Select((row, index) => (Row: row, Index: index, Score: LpLadder.Score(row.TierAfter, row.RankAfter, row.LpAfter)))
            .Where(k => k.Score.HasValue)
            .Select(k => (k.Row, k.Index, Score: k.Score!.Value))
            .ToList();
        if (known.Count == 0) return null;

        var first = known[0];
        var startScore = first.Row.LpChange is { } change ? first.Score - change : first.Score;
        var start = first.Row.LpChange.HasValue ? RankAt(startScore, first.Row) : RankOf(first.Row);
        var last = known[^1];

        var events = new List<LadderEvent>();
        var previousKey = DivisionKey(start);
        var previousScore = startScore;
        foreach (var point in known)
        {
            var rank = RankOf(point.Row);
            var key = DivisionKey(rank);
            if (key != previousKey && point.Score != previousScore)
            {
                events.Add(new LadderEvent(point.Index, point.Score > previousScore ? Promotion : Demotion, rank.Tier, rank.Division));
            }
            previousKey = key;
            previousScore = point.Score;
        }

        return new LpClimb(
            last.Score - startScore,
            start,
            RankOf(last.Row),
            known.Select(k => new LadderPoint(k.Index, k.Score)).ToList(),
            events,
            BiggestDrop(rows));
    }

    /// <summary>FR 11: the most negative LP sum over a run of 2 or more losses with known LP.</summary>
    public static LpDrop? BiggestDrop(IReadOnlyList<SoloMatchRow> rows)
    {
        LpDrop? worst = null;
        var runLp = 0;
        var runLosses = 0;

        for (var i = 0; i <= rows.Count; i++)
        {
            var row = i < rows.Count ? rows[i] : null;
            if (row is { Win: false, LpChange: { } change })
            {
                runLp += change;
                runLosses++;
                continue;
            }

            // The run ended at the match before this one
            if (runLosses >= MinDropLosses && runLp <= -MinDropLp && (worst is null || runLp < worst.Lp))
            {
                worst = new LpDrop(i - 1, runLp, runLosses);
            }
            runLp = 0;
            runLosses = 0;
        }

        return worst;
    }

    /// <summary>FR 12: the 10-match rolling win rate; null under 20 matches.</summary>
    public static WinRateClimb? WinRate(IReadOnlyList<SoloMatchRow> rows)
    {
        if (rows.Count < MinMatchesForWinRate) return null;

        var points = new List<WinRatePoint>();
        for (var i = Window - 1; i < rows.Count; i++)
        {
            points.Add(new WinRatePoint(i, SoloMath.WinRate(rows.Skip(i - Window + 1).Take(Window).ToList())));
        }

        return new WinRateClimb(
            SoloMath.WinRate(rows.Take(Window).ToList()),
            SoloMath.WinRate(rows.TakeLast(Window).ToList()),
            points);
    }

    /// <summary>
    /// FR 24: champions with 3 or more matches, largest value first (5 at most), and the names
    /// of those left out, most played first.
    /// </summary>
    public static (IReadOnlyList<ChampionClimb> Champions, IReadOnlyList<string> LeftOut) Champions(
        IReadOnlyList<SoloMatchRow> rows, string mode)
    {
        var groups = rows
            .GroupBy(r => r.ChampionId)
            .Select(g =>
            {
                var wins = g.Count(r => r.Win);
                var value = mode == LpCoverageRule.LpMode
                    ? g.Sum(r => r.LpChange ?? 0)
                    : wins - (g.Count() - wins);
                return new ChampionClimb(g.Key, g.First().ChampionName, g.Count(), wins, value);
            })
            .ToList();

        var shown = groups
            .Where(c => c.Matches >= MinChampionMatches)
            .OrderByDescending(c => c.Value)
            .ThenByDescending(c => c.Matches)
            .ThenBy(c => c.ChampionName, StringComparer.Ordinal)
            .Take(MaxChampions)
            .ToList();

        var leftOut = groups
            .Where(c => c.Matches < MinChampionMatches)
            .OrderByDescending(c => c.Matches)
            .ThenBy(c => c.ChampionName, StringComparer.Ordinal)
            .Select(c => c.ChampionName)
            .ToList();

        return (shown, leftOut);
    }

    /// <summary>FR 8: the rank after the latest match with one, for one ranked queue of one account.</summary>
    public static LadderRank? CurrentRank(IReadOnlyList<SoloMatchRow> rows, string queueType, bool singleAccount)
    {
        if (queueType is not (SoloScope.RankedSolo or SoloScope.RankedFlex) || !singleAccount) return null;

        var latest = rows.LastOrDefault(r => LpLadder.Score(r.TierAfter, r.RankAfter, r.LpAfter).HasValue);
        return latest is null ? null : RankOf(latest);
    }

    private static LadderRank RankOf(SoloMatchRow row)
    {
        var tier = row.TierAfter!.Trim().ToUpperInvariant();
        return new LadderRank(tier, LpLadder.IsApex(tier) ? null : row.RankAfter?.Trim().ToUpperInvariant(), row.LpAfter!.Value);
    }

    /// <summary>The rank at a score; above the Master floor the tier comes from the match itself.</summary>
    private static LadderRank RankAt(int score, SoloMatchRow row)
    {
        var rank = LpLadder.FromScore(score);
        return score >= LpLadder.ApexFloor && LpLadder.IsApex(row.TierAfter)
            ? rank with { Tier = row.TierAfter!.Trim().ToUpperInvariant() }
            : rank;
    }

    private static string DivisionKey(LadderRank rank) => $"{rank.Tier} {rank.Division}";
}
