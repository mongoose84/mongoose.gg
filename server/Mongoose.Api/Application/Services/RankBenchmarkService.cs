using Microsoft.Extensions.Caching.Memory;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// The rank-average pool for the Solo stat trends (features/solo-trends.spec.md FR 16, 5g). A pool
/// changes slowly, so it is read once an hour per queue, tier and role and shared by every player
/// who asks; the player's own matches are left out afterwards.
/// </summary>
public sealed class RankBenchmarkService
{
    /// <summary>The newest pool matches read; plenty for an average, and it bounds the query.</summary>
    public const int PoolLimit = 2000;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly ISoloTrendsRepository _repository;
    private readonly IMemoryCache _cache;

    public RankBenchmarkService(ISoloTrendsRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    /// <summary>
    /// The pool for <paramref name="target"/> without <paramref name="ownPuuids"/>' matches, or null
    /// when it is too small to be fair (<see cref="RankBenchmarkRule.Qualifies"/>).
    /// </summary>
    public async Task<RankBenchmarkPool?> GetPoolAsync(RankBenchmarkTarget target, IReadOnlyList<string> ownPuuids)
    {
        var key = $"solo-rank-pool:{target.QueueId}:{target.Tier}:{target.Role}";
        var pool = await _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return _repository.GetRankPoolRowsAsync(target.QueueId, target.Tier, target.Role, PoolLimit);
        }) ?? [];

        var own = ownPuuids.ToHashSet(StringComparer.Ordinal);
        var others = pool.Where(p => !own.Contains(p.Puuid)).ToList();

        return RankBenchmarkRule.Qualifies(others)
            ? new RankBenchmarkPool(target.Tier, others.Select(p => p.Row).ToList())
            : null;
    }
}
