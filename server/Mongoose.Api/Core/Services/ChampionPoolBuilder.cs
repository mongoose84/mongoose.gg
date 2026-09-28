using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Builds the Overview "Your champions" pool: champions ranked by M-Score, the top three as cards
/// with one unique strength tag each, the next three as "Also played".
/// See .github/specs/features/overview-champion-pool.spec.md.
/// </summary>
public static class ChampionPoolBuilder
{
    public record PoolEntry(
        int ChampionId,
        string ChampionName,
        string Role,
        int Matches,
        int Wins,
        double WinRate,
        double AvgKda,
        double MScore,
        string? StrengthTag
    );

    public record ChampionPool(
        IReadOnlyList<PoolEntry> Champions,
        IReadOnlyList<PoolEntry> AlsoPlayed
    );

    public const int CardCount = 3;
    public const int AlsoPlayedCount = 3;

    // A tag needs a real sample: fewer matches than this and the champion gets no tag.
    public const int MinMatchesForTag = 5;

    // A metric must be at least 10% above the player's own average (or +100 gold at 15) to earn a tag.
    public const double MinLeadForTag = 0.10;

    // Gold diff can be zero or negative, so its lead is measured in thousands of gold, not relative.
    private const double GoldDiffLeadScale = 1000.0;

    private enum Metric { Laning, Damage, Kda, Farming, Vision, Involvement }

    private static readonly IReadOnlyDictionary<Metric, string> TagLabels = new Dictionary<Metric, string>
    {
        [Metric.Laning] = "Best laning",
        [Metric.Damage] = "Most damage",
        [Metric.Kda] = "Best KDA",
        [Metric.Farming] = "Best farming",
        [Metric.Vision] = "Best vision",
        [Metric.Involvement] = "Most involved",
    };

    public static ChampionPool Build(
        IReadOnlyList<ChampionPoolStatsData> champions,
        IReadOnlyList<ChampionRoleCountData> roleCounts)
    {
        ArgumentNullException.ThrowIfNull(champions);
        ArgumentNullException.ThrowIfNull(roleCounts);

        var primaryRoles = roleCounts
            .GroupBy(r => r.ChampionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Games).ThenByDescending(r => r.LastPlayed).First().Role);

        var ranked = champions
            .Where(c => c.Games > 0)
            .Select(c => (Stats: c, Entry: BuildEntry(c, primaryRoles.GetValueOrDefault(c.ChampionId, "UNKNOWN"))))
            .OrderByDescending(x => x.Entry.MScore)
            .ThenByDescending(x => x.Entry.Matches)
            .ThenBy(x => x.Entry.ChampionName, StringComparer.Ordinal)
            .ToList();

        var cards = ranked.Take(CardCount).ToList();
        var tags = AssignStrengthTags(cards.Select(x => x.Stats).ToList(), BuildBaseline(champions));

        return new ChampionPool(
            Champions: cards.Select(x => x.Entry with { StrengthTag = tags.GetValueOrDefault(x.Stats.ChampionId) }).ToArray(),
            AlsoPlayed: ranked.Skip(CardCount).Take(AlsoPlayedCount).Select(x => x.Entry).ToArray());
    }

    private static PoolEntry BuildEntry(ChampionPoolStatsData c, string role)
    {
        var winRate = Math.Round((double)c.Wins / c.Games * 100, 1);
        var mScore = MainChampionRecommender.ComputeMScore(
            winRate, c.Games,
            c.AvgKills, c.AvgDeaths, c.AvgAssists,
            c.AvgGoldDiff15, c.AvgDeathsPre10, c.AvgVisionPerMin,
            role);

        return new PoolEntry(
            ChampionId: c.ChampionId,
            ChampionName: c.ChampionName,
            Role: role,
            Matches: c.Games,
            Wins: c.Wins,
            WinRate: winRate,
            AvgKda: Math.Round(Kda(c), 2),
            MScore: mScore,
            StrengthTag: null);
    }

    /// <summary>
    /// Gives each card champion at most one tag, greedily by largest lead, never the same tag twice.
    /// </summary>
    private static Dictionary<int, string> AssignStrengthTags(
        IReadOnlyList<ChampionPoolStatsData> cards,
        IReadOnlyDictionary<Metric, double> baseline)
    {
        var candidates = cards
            .Where(c => c.Games >= MinMatchesForTag)
            .SelectMany(c => baseline
                .Select(b => (Champion: c, Metric: b.Key, Lead: Lead(b.Key, MetricValue(c, b.Key), b.Value))))
            .Where(x => x.Lead is >= MinLeadForTag)
            .OrderByDescending(x => x.Lead)
            .ThenBy(x => x.Metric)
            .ThenBy(x => x.Champion.ChampionName, StringComparer.Ordinal);

        var tags = new Dictionary<int, string>();
        var usedMetrics = new HashSet<Metric>();
        foreach (var (champion, metric, _) in candidates)
        {
            if (tags.ContainsKey(champion.ChampionId) || !usedMetrics.Add(metric))
                continue;
            tags[champion.ChampionId] = TagLabels[metric];
        }
        return tags;
    }

    /// <summary>
    /// The player's own average per metric across every champion in the window,
    /// weighted by the matches that carry the metric. Metrics without data are left out.
    /// </summary>
    private static Dictionary<Metric, double> BuildBaseline(IReadOnlyList<ChampionPoolStatsData> champions)
    {
        var baseline = new Dictionary<Metric, double>();

        void Add(Metric metric, Func<ChampionPoolStatsData, double?> value, Func<ChampionPoolStatsData, int> samples)
        {
            var weighted = champions
                .Select(c => (Value: value(c), Samples: samples(c)))
                .Where(x => x.Value.HasValue && x.Samples > 0)
                .ToList();
            var total = weighted.Sum(x => x.Samples);
            if (total > 0)
                baseline[metric] = weighted.Sum(x => x.Value!.Value * x.Samples) / total;
        }

        Add(Metric.Laning, c => c.AvgGoldDiff15, c => c.GoldDiff15Samples);
        Add(Metric.Damage, c => c.AvgDamageSharePct, c => c.MetricSamples);
        Add(Metric.Kda, c => Kda(c), c => c.Games);
        Add(Metric.Farming, c => c.AvgCsPerMin, c => c.Games);
        Add(Metric.Vision, c => c.AvgVisionPerMin, c => c.MetricSamples);
        Add(Metric.Involvement, c => c.AvgKillParticipationPct, c => c.MetricSamples);
        return baseline;
    }

    private static double? MetricValue(ChampionPoolStatsData c, Metric metric) => metric switch
    {
        Metric.Laning => c.AvgGoldDiff15,
        Metric.Damage => c.AvgDamageSharePct,
        Metric.Kda => Kda(c),
        Metric.Farming => c.AvgCsPerMin,
        Metric.Vision => c.AvgVisionPerMin,
        Metric.Involvement => c.AvgKillParticipationPct,
        _ => null
    };

    private static double? Lead(Metric metric, double? value, double baseline)
    {
        if (!value.HasValue)
            return null;
        if (metric == Metric.Laning)
            return (value.Value - baseline) / GoldDiffLeadScale;
        if (baseline <= 0)
            return null;
        return (value.Value - baseline) / baseline;
    }

    private static double Kda(ChampionPoolStatsData c) => (c.AvgKills + c.AvgAssists) / Math.Max(1.0, c.AvgDeaths);
}
