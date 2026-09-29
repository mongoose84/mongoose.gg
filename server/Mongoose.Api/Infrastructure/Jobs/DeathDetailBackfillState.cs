using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// In-memory backfill state (singleton): each account's progress and the accounts players asked for
/// most recently. Lost on restart by design; the job rebuilds it from the database.
/// </summary>
public sealed class DeathDetailBackfillState : IDeathDetailBackfillState
{
    private readonly ConcurrentDictionary<string, DeathDetailBackfillProgress> _progress = new();
    private readonly ConcurrentDictionary<string, long> _priority = new();
    private long _sequence;

    public DeathDetailBackfillProgress? Get(string puuid) => _progress.TryGetValue(puuid, out var p) ? p : null;

    public void Prioritize(string puuid) => _priority[puuid] = Interlocked.Increment(ref _sequence);

    public void Set(string puuid, DeathDetailBackfillProgress progress) => _progress[puuid] = progress;

    public void Clear(string puuid)
    {
        _progress.TryRemove(puuid, out _);
        _priority.TryRemove(puuid, out _);
    }

    /// <summary>The prioritized accounts, the most recent request first.</summary>
    public IReadOnlyList<string> Prioritized() =>
        _priority.OrderByDescending(p => p.Value).Select(p => p.Key).ToList();
}
