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

    public record BenchmarkDto(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("value")] double Value);

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

    public static StatTrendDto ToDto(StatTrend t) => new(
        t.Key,
        t.Values,
        t.Rolling.Select(p => new RollingPointDto(p.Index, p.Value)).ToList(),
        t.Was,
        t.Now,
        t.Count,
        t.Verdict,
        t.NormalizedChange,
        t.Benchmark == null ? null : new BenchmarkDto(t.Benchmark.Kind, t.Benchmark.Value));

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
