using System.Text.Json.Serialization;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.DTOs.Solo;

/// <summary>
/// Response shapes of the Solo trend endpoints (features/solo-trends.spec.md, API Contracts).
/// </summary>
public static class SoloTrendsDto
{
    public record RollingPointDto(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("value")] double Value);

    /// <summary>"season" or "rank"; <c>tier</c> ("EMERALD") is set for "rank".</summary>
    public record BenchmarkDto(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("value")] double Value,
        [property: JsonPropertyName("tier")] string? Tier = null);

    public record StatTrendDto(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("values")] IReadOnlyList<double?>? Values,
        [property: JsonPropertyName("rolling")] IReadOnlyList<RollingPointDto> Rolling,
        [property: JsonPropertyName("was")] double? Was,
        [property: JsonPropertyName("now")] double? Now,
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("verdict")] string? Verdict,
        [property: JsonPropertyName("normalizedChange")] double? NormalizedChange,
        [property: JsonPropertyName("benchmark")] BenchmarkDto? Benchmark);

    public record FocusDto(
        [property: JsonPropertyName("stat")] string Stat,
        [property: JsonPropertyName("factor")] string Factor,
        [property: JsonPropertyName("mark")] double? Mark,
        [property: JsonPropertyName("was")] double? Was,
        [property: JsonPropertyName("now")] double? Now,
        [property: JsonPropertyName("hitWinRate")] int HitWinRate,
        [property: JsonPropertyName("missWinRate")] int MissWinRate,
        [property: JsonPropertyName("last20")] IReadOnlyList<string?> Last20,
        [property: JsonPropertyName("hits")] int Hits);

    public record StatTrendsResponse(
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("range")] string Range,
        [property: JsonPropertyName("stats")] IReadOnlyList<StatTrendDto> Stats,
        [property: JsonPropertyName("focus")] FocusDto? Focus);

    public record WinFactorDto(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("hitWinRate")] int HitWinRate,
        [property: JsonPropertyName("missWinRate")] int MissWinRate,
        [property: JsonPropertyName("hitMatches")] int HitMatches,
        [property: JsonPropertyName("missMatches")] int MissMatches,
        [property: JsonPropertyName("gap")] int Gap,
        [property: JsonPropertyName("mark")] double? Mark);

    public record PatternGroupDto(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("winRate")] int WinRate);

    public record GroupPatternDto(
        [property: JsonPropertyName("groups")] IReadOnlyList<PatternGroupDto> Groups,
        [property: JsonPropertyName("weak")] string? Weak);

    public record AfterResultDto(
        [property: JsonPropertyName("pairs")] int Pairs,
        [property: JsonPropertyName("winRate")] int WinRate);

    public record AfterLossDto(
        [property: JsonPropertyName("afterWin")] AfterResultDto AfterWin,
        [property: JsonPropertyName("afterLoss")] AfterResultDto AfterLoss);

    public record PatternsDto(
        [property: JsonPropertyName("session")] GroupPatternDto? Session,
        [property: JsonPropertyName("afterLoss")] AfterLossDto? AfterLoss,
        [property: JsonPropertyName("length")] GroupPatternDto? Length);

    public record WinFactorsResponse(
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("range")] string Range,
        [property: JsonPropertyName("factors")] IReadOnlyList<WinFactorDto> Factors,
        [property: JsonPropertyName("patterns")] PatternsDto Patterns);

    public record RankDto(
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("division")] string? Division,
        [property: JsonPropertyName("lp")] int Lp);

    public record LadderPointDto(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("ladder")] int Ladder);

    public record LadderEventDto(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("division")] string? Division);

    public record LpDropDto(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("lp")] int Lp,
        [property: JsonPropertyName("losses")] int Losses);

    public record LpClimbDto(
        [property: JsonPropertyName("net")] int Net,
        [property: JsonPropertyName("start")] RankDto Start,
        [property: JsonPropertyName("end")] RankDto End,
        [property: JsonPropertyName("points")] IReadOnlyList<LadderPointDto> Points,
        [property: JsonPropertyName("events")] IReadOnlyList<LadderEventDto> Events,
        [property: JsonPropertyName("biggestDrop")] LpDropDto? BiggestDrop);

    public record WinRatePointDto(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("rate")] int Rate);

    public record WinRateClimbDto(
        [property: JsonPropertyName("was")] int Was,
        [property: JsonPropertyName("now")] int Now,
        [property: JsonPropertyName("points")] IReadOnlyList<WinRatePointDto> Points);

    public record ChampionClimbDto(
        [property: JsonPropertyName("championId")] int ChampionId,
        [property: JsonPropertyName("championName")] string ChampionName,
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("value")] int Value);

    public record ClimbResponse(
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("range")] string Range,
        [property: JsonPropertyName("mode")] string Mode,
        [property: JsonPropertyName("wins")] int Wins,
        [property: JsonPropertyName("losses")] int Losses,
        [property: JsonPropertyName("lp")] LpClimbDto? Lp,
        [property: JsonPropertyName("winRate")] WinRateClimbDto? WinRate,
        [property: JsonPropertyName("champions")] IReadOnlyList<ChampionClimbDto> Champions,
        [property: JsonPropertyName("championsLeftOut")] IReadOnlyList<string> ChampionsLeftOut,
        [property: JsonPropertyName("rank")] RankDto? Rank);

    public static ClimbResponse ToDto(SoloClimb c, int matches, string queueType, string range) => new(
        matches,
        queueType,
        range,
        c.Mode,
        c.Wins,
        c.Losses,
        c.Lp == null ? null : new LpClimbDto(
            c.Lp.Net,
            ToDto(c.Lp.Start),
            ToDto(c.Lp.End),
            c.Lp.Points.Select(p => new LadderPointDto(p.Index, p.Ladder)).ToList(),
            c.Lp.Events.Select(e => new LadderEventDto(e.Index, e.Kind, e.Tier, e.Division)).ToList(),
            c.Lp.BiggestDrop == null ? null : new LpDropDto(c.Lp.BiggestDrop.Index, c.Lp.BiggestDrop.Lp, c.Lp.BiggestDrop.Losses)),
        c.WinRate == null ? null : new WinRateClimbDto(
            c.WinRate.Was,
            c.WinRate.Now,
            c.WinRate.Points.Select(p => new WinRatePointDto(p.Index, p.Rate)).ToList()),
        c.Champions.Select(ch => new ChampionClimbDto(ch.ChampionId, ch.ChampionName, ch.Matches, ch.Wins, ch.Value)).ToList(),
        c.ChampionsLeftOut,
        c.Rank == null ? null : ToDto(c.Rank));

    private static RankDto ToDto(LadderRank r) => new(r.Tier, r.Division, r.Lp);

    public record AnchorDto(
        [property: JsonPropertyName("u")] double U,
        [property: JsonPropertyName("v")] double V);

    public record ZoneTimingDto(
        [property: JsonPropertyName("phase")] string? Phase,
        [property: JsonPropertyName("count")] int Count);

    public record DeathZoneDto(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("deaths")] int Deaths,
        [property: JsonPropertyName("lostObjectives")] int LostObjectives,
        [property: JsonPropertyName("costly")] bool Costly,
        [property: JsonPropertyName("anchor")] AnchorDto Anchor,
        [property: JsonPropertyName("timing")] ZoneTimingDto Timing);

    public record DeathBreakdownDto(
        [property: JsonPropertyName("phase")] IReadOnlyDictionary<string, int> Phase,
        [property: JsonPropertyName("how")] IReadOnlyDictionary<string, int> How,
        [property: JsonPropertyName("cost")] IReadOnlyDictionary<string, int> Cost);

    public record DeathBreakdownsDto(
        [property: JsonPropertyName("all")] DeathBreakdownDto All,
        [property: JsonPropertyName("byZone")] IReadOnlyDictionary<string, DeathBreakdownDto> ByZone);

    public record BackfillDto(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("done")] int Done,
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("retryAt")] DateTime? RetryAt);

    public record DeathZonesResponse(
        [property: JsonPropertyName("matches")] int Matches,
        [property: JsonPropertyName("queueType")] string QueueType,
        [property: JsonPropertyName("range")] string Range,
        [property: JsonPropertyName("deaths")] int Deaths,
        [property: JsonPropertyName("ready")] bool Ready,
        [property: JsonPropertyName("zones")] IReadOnlyList<DeathZoneDto> Zones,
        [property: JsonPropertyName("breakdowns")] DeathBreakdownsDto Breakdowns,
        [property: JsonPropertyName("backfill")] BackfillDto? Backfill);

    public static DeathZonesResponse ToDto(SoloDeathZones z, int matches, string queueType, string range, BackfillDto? backfill) => new(
        matches,
        queueType,
        range,
        z.Deaths,
        z.Ready,
        z.Zones.Select(zone => new DeathZoneDto(
            zone.Key,
            zone.Deaths,
            zone.LostObjectives,
            zone.Costly,
            new AnchorDto(zone.Anchor.U, zone.Anchor.V),
            new ZoneTimingDto(zone.Timing.Phase, zone.Timing.Count))).ToList(),
        new DeathBreakdownsDto(ToDto(z.All), z.ByZone.ToDictionary(kv => kv.Key, kv => ToDto(kv.Value))),
        backfill);

    private static DeathBreakdownDto ToDto(DeathBreakdown b) => new(b.Phase, b.How, b.Cost);

    public static StatTrendDto ToDto(StatTrend t) => new(
        t.Key,
        t.Values,
        t.Rolling.Select(p => new RollingPointDto(p.Index, p.Value)).ToList(),
        t.Was,
        t.Now,
        t.Count,
        t.Verdict,
        t.NormalizedChange,
        t.Benchmark == null ? null : new BenchmarkDto(t.Benchmark.Kind, t.Benchmark.Value, t.Benchmark.Tier));

    public static FocusDto? ToDto(SoloFocus? f) => f == null ? null : new(
        f.Stat, f.Factor, f.Mark, f.Was, f.Now, f.HitWinRate, f.MissWinRate, f.Last20, f.Hits);

    public static WinFactorDto ToDto(WinFactor f, double? mark) => new(
        f.Key, f.HitWinRate, f.MissWinRate, f.HitMatches, f.MissMatches, f.Gap, mark);

    public static PatternsDto ToDto(SoloPatterns p) => new(ToDto(p.Session), ToDto(p.AfterLoss), ToDto(p.Length));

    private static GroupPatternDto? ToDto(GroupPattern? p) => p == null ? null : new(
        p.Groups.Select(g => new PatternGroupDto(g.Key, g.Matches, g.WinRate)).ToList(), p.Weak);

    private static AfterLossDto? ToDto(AfterLossPattern? p) => p == null ? null : new(
        new AfterResultDto(p.AfterWin.Pairs, p.AfterWin.WinRate),
        new AfterResultDto(p.AfterLoss.Pairs, p.AfterLoss.WinRate));
}
