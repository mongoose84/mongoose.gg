using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake implementation of IMatchupRepository for testing.
    /// </summary>
    internal sealed class FakeMatchupRepository : IMatchupRepository
    {
        private readonly ConcurrentDictionary<string, ChampionMatchupsResponse> _matchupData = new();

        public void SetMatchupData(string puuid, ChampionMatchupsResponse data)
        {
            _matchupData[puuid] = data;
        }

        public void Clear()
        {
            _matchupData.Clear();
        }

        public Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
        {
            // For testing, return data from the first PUUID that has data
            foreach (var puuid in puuids)
            {
                if (_matchupData.TryGetValue(puuid, out var data))
                    return Task.FromResult(data);
            }

            // Return empty response if no data
            return Task.FromResult(new ChampionMatchupsResponse(
                Matchups: Array.Empty<ChampionMatchup>(),
                QueueType: queueType ?? "all",
                TimeRange: timeRange ?? "all"
            ));
        }
    }
}
