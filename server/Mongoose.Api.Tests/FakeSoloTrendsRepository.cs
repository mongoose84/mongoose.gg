using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Tests;

/// <summary>
/// In-memory Solo match rows for HTTP tests. Mirrors <c>SoloTrendsRepository</c>'s contract: queue scope,
/// Last 20 / Last 50 as the most recent matches across seasons, Season as the matches flagged in season,
/// and rows returned oldest first.
/// </summary>
public sealed class FakeSoloTrendsRepository : ISoloTrendsRepository
{
    public List<(string Puuid, SoloMatchRow Row, bool InSeason)> Matches { get; } = new();

    /// <summary>Every <see cref="GetMatchRowsAsync"/> call, in order.</summary>
    public List<(IReadOnlyList<string> Puuids, string QueueType, SoloRange Range)> RowQueries { get; } = new();

    public void Add(string puuid, SoloMatchRow row, bool inSeason = true) => Matches.Add((puuid, row, inSeason));

    public void AddRange(string puuid, IEnumerable<SoloMatchRow> rows, bool inSeason = true)
    {
        foreach (var row in rows) Add(puuid, row, inSeason);
    }

    public Task<IReadOnlyList<SoloMatchRow>> GetMatchRowsAsync(IReadOnlyList<string> puuids, string queueType, SoloRange range)
    {
        RowQueries.Add((puuids, queueType, range));

        var queueIds = queueType switch
        {
            SoloScope.RankedSolo => new[] { 420 },
            SoloScope.RankedFlex => new[] { 440 },
            _ => GameConstants.SummonersRiftQueueIds
        };

        var newestFirst = Matches
            .Where(m => puuids.Contains(m.Puuid) && queueIds.Contains(m.Row.QueueId))
            .Where(m => range != SoloRange.Season || m.InSeason)
            .Select(m => m.Row)
            .OrderByDescending(r => r.GameStartTime);

        var limit = SoloScope.RangeLimit(range);
        var rows = (limit.HasValue ? newestFirst.Take(limit.Value) : newestFirst).Reverse().ToList();
        return Task.FromResult<IReadOnlyList<SoloMatchRow>>(rows);
    }

    public Task<SoloQueueCounts> GetSeasonQueueCountsAsync(IReadOnlyList<string> puuids)
    {
        var season = Matches.Where(m => puuids.Contains(m.Puuid) && m.InSeason).ToList();
        return Task.FromResult(new SoloQueueCounts(
            season.Count(m => m.Row.QueueId == 420),
            season.Count(m => m.Row.QueueId == 440)));
    }
}

/// <summary>
/// Builds <see cref="SoloMatchRow"/>s for Solo rule and endpoint tests. The defaults hit every win-factor
/// mark (so no factor row has 5 misses unless a test adds them), and matches are two hours apart, so each
/// one is its own session.
/// </summary>
internal static class SoloRows
{
    public const long BaseStartMs = 1_767_225_600_000; // 2026-01-01T00:00:00Z
    public const long TwoHoursMs = 2 * 60 * 60 * 1000;

    public static SoloMatchRow Make(
        int index,
        bool win = true,
        int deaths = 3,
        string role = "MIDDLE",
        int durationSec = 1800,
        int creepScore = 270,
        int? goldDiffAt15 = 100,
        int? dragonsParticipated = 2,
        int? teamDragons = 2,
        double? visionPerMin = 1.0,
        double? killParticipationPct = 50,
        int teamKills = 20,
        int queueId = 420,
        long? startMs = null)
        => new(
            MatchId: $"EUW1_{index}",
            GameStartTime: startMs ?? BaseStartMs + index * TwoHoursMs,
            DurationSec: durationSec,
            QueueId: queueId,
            Role: role,
            ChampionId: 103,
            ChampionName: "Ahri",
            Win: win,
            Deaths: deaths,
            CreepScore: creepScore,
            GoldDiffAt15: goldDiffAt15,
            DragonsParticipated: dragonsParticipated,
            TeamDragons: teamDragons,
            VisionPerMin: visionPerMin,
            KillParticipationPct: killParticipationPct,
            TeamKills: teamKills);

    /// <summary><paramref name="count"/> rows built by <paramref name="build"/> from their index.</summary>
    public static List<SoloMatchRow> Many(int count, Func<int, SoloMatchRow>? build = null)
        => Enumerable.Range(0, count).Select(i => build?.Invoke(i) ?? Make(i)).ToList();
}
