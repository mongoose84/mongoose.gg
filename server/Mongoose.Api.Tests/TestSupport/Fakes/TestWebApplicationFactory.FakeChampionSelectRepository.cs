using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake implementation of IChampionSelectRepository for testing.
    /// </summary>
    internal sealed class FakeChampionSelectRepository : IChampionSelectRepository
    {
        private readonly ConcurrentDictionary<string, ChampionSelectResponse> _championSelectData = new();

        public void SetChampionSelectData(string puuid, ChampionSelectResponse data)
        {
            _championSelectData[puuid] = data;
        }

        public void Clear()
        {
            _championSelectData.Clear();
        }

        public Task<ChampionSelectResponse?> GetChampionSelectDataAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
        {
            // Returns the first matching PUUID's data, or null if none exist
            foreach (var puuid in puuids)
            {
                if (_championSelectData.TryGetValue(puuid, out var data))
                    return Task.FromResult<ChampionSelectResponse?>(data);
            }
            return Task.FromResult<ChampionSelectResponse?>(null);
        }
    }
}
