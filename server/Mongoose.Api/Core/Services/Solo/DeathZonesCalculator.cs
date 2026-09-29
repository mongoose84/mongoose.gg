using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>Deaths by phase, by how, and objectives lost by type.</summary>
public sealed record DeathBreakdown(
    IReadOnlyDictionary<string, int> Phase,
    IReadOnlyDictionary<string, int> How,
    IReadOnlyDictionary<string, int> Cost);

/// <summary>The phase holding 60% or more of a zone's deaths, with the count; null phase for "spread".</summary>
public sealed record ZoneTiming(string? Phase, int Count);

public sealed record DeathZone(
    string Key,
    int Deaths,
    int LostObjectives,
    bool Costly,
    MapRegion Anchor,
    ZoneTiming Timing);

public sealed record SoloDeathZones(
    int Deaths,
    bool Ready,
    int MissingDetail,
    IReadOnlyList<DeathZone> Zones,
    DeathBreakdown All,
    IReadOnlyDictionary<string, DeathBreakdown> ByZone);

/// <summary>
/// Where the player's deaths cost them (features/solo-trends.spec.md FR 31, FR 37). Pure and static.
/// </summary>
public static class DeathZonesCalculator
{
    // FR 31: the card needs 30 counted deaths
    public const int MinDeaths = 30;

    // FR 37: a zone needs 5 deaths; at most 5 zones; costly at 30% lost; timing at 60% in one phase
    public const int MinZoneDeaths = 5;
    public const int MaxZones = 5;
    public const double CostlyShare = 0.30;
    public const double TimingShare = 0.60;

    private static readonly string[] Phases = [DeathClassifier.Early, DeathClassifier.Mid, DeathClassifier.Late];
    private static readonly string[] Hows = [DeathClassifier.Ganked, DeathClassifier.Alone, DeathClassifier.Teamfight, DeathClassifier.Other];

    public static SoloDeathZones Calculate(
        IReadOnlyList<SoloDeathRow> deaths,
        IReadOnlyList<MatchParticipantRole> participants,
        IReadOnlyList<SoloObjectiveRow> objectives)
    {
        var participantsByMatch = participants
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<int, MatchParticipantRole>)g.ToDictionary(p => p.ParticipantId));
        var objectivesByMatch = objectives.ToLookup(o => o.MatchId);
        var none = new Dictionary<int, MatchParticipantRole>();

        var counted = deaths.Where(DeathClassifier.IsCounted).ToList();
        var classified = counted
            .Select(d => DeathClassifier.Classify(d, participantsByMatch.GetValueOrDefault(d.MatchId, none), objectivesByMatch[d.MatchId]))
            .ToList();

        var zones = classified
            .GroupBy(c => c.Region)
            .Where(g => g.Count() >= MinZoneDeaths)
            .Select(g => Zone(g.Key, g.ToList()))
            .OrderByDescending(z => z.LostObjectives)
            .ThenByDescending(z => z.Deaths)
            .ThenBy(z => MapRegions.All.ToList().FindIndex(r => r.Key == z.Key))
            .Take(MaxZones)
            .ToList();

        var byZone = zones.ToDictionary(z => z.Key, z => Breakdown(classified.Where(c => c.Region == z.Key).ToList()));

        return new SoloDeathZones(
            counted.Count,
            counted.Count >= MinDeaths,
            deaths.Count - counted.Count,
            zones,
            Breakdown(classified),
            byZone);
    }

    private static DeathZone Zone(string key, IReadOnlyList<ClassifiedDeath> deaths)
    {
        var lost = deaths.Count(d => d.Cost != null);
        return new DeathZone(key, deaths.Count, lost, (double)lost / deaths.Count >= CostlyShare, MapRegions.Get(key), Timing(deaths));
    }

    /// <summary>FR 37: the phase holding 60% or more of the zone's deaths, else "spread over the match".</summary>
    public static ZoneTiming Timing(IReadOnlyList<ClassifiedDeath> deaths)
    {
        var top = Phases
            .Select(phase => (Phase: phase, Count: deaths.Count(d => d.Phase == phase)))
            .OrderByDescending(p => p.Count)
            .First();
        return top.Count >= deaths.Count * TimingShare ? new ZoneTiming(top.Phase, top.Count) : new ZoneTiming(null, top.Count);
    }

    private static DeathBreakdown Breakdown(IReadOnlyList<ClassifiedDeath> deaths) => new(
        Phases.ToDictionary(p => p, p => deaths.Count(d => d.Phase == p)),
        Hows.ToDictionary(h => h, h => deaths.Count(d => d.How == h)),
        DeathClassifier.CostTypes.ToDictionary(t => t, t => deaths.Count(d => d.Cost == t)));
}
