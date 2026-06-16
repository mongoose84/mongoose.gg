using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// <see cref="ISyncProgressBroadcaster"/> decorator that fans each per-account broadcast out to
/// both the per-account channel (the inner <see cref="SyncProgressHub"/>, consumed by the Settings
/// page) and the per-user <see cref="ISyncProgressAggregator"/> (consumed by the Overview card).
/// This keeps <see cref="Jobs.MatchHistorySyncJob"/> unchanged — it still depends only on
/// <see cref="ISyncProgressBroadcaster"/>.
/// </summary>
public sealed class AggregatingSyncProgressBroadcaster : ISyncProgressBroadcaster
{
    private readonly SyncProgressHub _inner;
    private readonly ISyncProgressAggregator _aggregator;

    public AggregatingSyncProgressBroadcaster(SyncProgressHub inner, ISyncProgressAggregator aggregator)
    {
        _inner = inner;
        _aggregator = aggregator;
    }

    public async Task BroadcastProgressAsync(string puuid, int progress, int total, string? currentMatchId = null)
    {
        await _inner.BroadcastProgressAsync(puuid, progress, total, currentMatchId);
        await _aggregator.OnProgressAsync(puuid, progress, total, currentMatchId);
    }

    public async Task BroadcastCompleteAsync(string puuid, int totalSynced)
    {
        await _inner.BroadcastCompleteAsync(puuid, totalSynced);
        await _aggregator.OnCompleteAsync(puuid, totalSynced);
    }

    public async Task BroadcastErrorAsync(string puuid, string error)
    {
        await _inner.BroadcastErrorAsync(puuid, error);
        await _aggregator.OnErrorAsync(puuid, error);
    }

    public Task BroadcastRateLimitedAsync(string puuid)
    {
        // Rate-limit status is surfaced on the per-account channel only for now; the aggregate
        // view treats a rate-limited account as still "syncing" (v1).
        return _inner.BroadcastRateLimitedAsync(puuid);
    }
}
