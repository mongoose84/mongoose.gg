using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

/// <summary>
/// In-memory rank snapshots for service, job and HTTP tests. Due accounts, candidate matches and
/// the active-user rule are seeded by the test (<see cref="DuePuuids"/>, <see cref="MatchEnds"/>).
/// </summary>
public sealed class FakeRankSnapshotsRepository : IRankSnapshotsRepository
{
    private long _nextId = 1;

    public List<RankSnapshotRecord> Snapshots { get; } = new();

    /// <summary>Stored matches: (puuid, queueId, matchId, end time in epoch ms).</summary>
    public List<(string Puuid, int QueueId, string MatchId, long EndMs)> MatchEnds { get; } = new();

    /// <summary>What <see cref="GetDueAccountPuuidsAsync"/> returns (capped by its limit).</summary>
    public List<string> DuePuuids { get; } = new();

    public Dictionary<string, DateTime> RankCheckedAt { get; } = new();

    public (DateTime ActiveSince, DateTime CheckedBefore, int Limit)? LastDueQuery { get; private set; }

    public DateTime? LastRetentionCutoff { get; private set; }

    public Task<RankSnapshotRecord?> GetLatestAsync(string puuid, int queueId) =>
        Task.FromResult(Snapshots
            .Where(s => s.Puuid == puuid && s.QueueId == queueId)
            .OrderByDescending(s => s.CapturedAt).ThenByDescending(s => s.Id)
            .FirstOrDefault());

    public Task<long> InsertAsync(RankSnapshotRecord snapshot)
    {
        snapshot.Id = _nextId++;
        Snapshots.Add(snapshot);
        return Task.FromResult(snapshot.Id);
    }

    public Task<IList<RankSnapshotRecord>> GetPendingAsync(string? puuid, int limit) =>
        Task.FromResult<IList<RankSnapshotRecord>>(Snapshots
            .Where(s => s.Status == RankSnapshotStatus.Pending && (puuid is null || s.Puuid == puuid))
            .OrderBy(s => s.CapturedAt).ThenBy(s => s.Id)
            .Take(limit)
            .ToList());

    public Task<IList<string>> GetMatchIdsEndedBetweenAsync(string puuid, int queueId, long fromMs, long toMs) =>
        Task.FromResult<IList<string>>(MatchEnds
            .Where(m => m.Puuid == puuid && m.QueueId == queueId && m.EndMs >= fromMs && m.EndMs <= toMs)
            .Select(m => m.MatchId)
            .ToList());

    public Task UpdateStatusAsync(long id, string status, string? matchId)
    {
        var snapshot = Snapshots.Single(s => s.Id == id);
        snapshot.Status = status;
        snapshot.MatchId = matchId;
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetDueAccountPuuidsAsync(DateTime activeSinceUtc, DateTime checkedBeforeUtc, int limit)
    {
        LastDueQuery = (activeSinceUtc, checkedBeforeUtc, limit);
        return Task.FromResult<IList<string>>(DuePuuids.Take(limit).ToList());
    }

    public Task MarkRankCheckedAsync(string puuid, DateTime checkedAtUtc)
    {
        RankCheckedAt[puuid] = checkedAtUtc;
        return Task.CompletedTask;
    }

    public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc)
    {
        LastRetentionCutoff = cutoffUtc;
        var newest = Snapshots
            .GroupBy(s => (s.Puuid, s.QueueId))
            .Select(g => g.MaxBy(s => s.Id)!.Id)
            .ToHashSet();
        return Task.FromResult(Snapshots.RemoveAll(s => s.CapturedAt < cutoffUtc && !newest.Contains(s.Id)));
    }
}
