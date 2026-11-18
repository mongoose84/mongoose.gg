using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake implementation of ITrendRepository for testing.
    /// </summary>
    internal sealed class FakeTrendRepository : ITrendRepository
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, int>> _dailyCounts = new();

        public void SetDailyMatchCounts(string puuid, Dictionary<string, int> counts)
        {
            _dailyCounts[puuid] = counts;
        }

        public Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
        {
            _dailyCounts.TryGetValue(puuid, out var counts);
            return Task.FromResult(counts ?? new Dictionary<string, int>());
        }

        public Task<Dictionary<string, int>> GetDailyMatchCountsAsync(IReadOnlyList<string> puuids, int daysBack = 91)
        {
            var merged = new Dictionary<string, int>();
            foreach (var puuid in puuids)
            {
                if (_dailyCounts.TryGetValue(puuid, out var counts))
                {
                    foreach (var (date, count) in counts)
                    {
                        merged[date] = merged.TryGetValue(date, out var existing) ? existing + count : count;
                    }
                }
            }
            return Task.FromResult(merged);
        }
    }
}
