using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake implementation of ISoloPerformanceRepository for testing.
    /// </summary>
    internal sealed class FakeSoloPerformanceRepository : ISoloPerformanceRepository
    {
        private readonly ConcurrentDictionary<string, SoloPerformanceResponse> _performanceData = new();

        public void SetPerformanceData(string puuid, SoloPerformanceResponse data)
        {
            _performanceData[puuid] = data;
        }

        public void Clear()
        {
            _performanceData.Clear();
        }

        public Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(string puuid, string? queueType = null, string? timeRange = null)
        {
            _performanceData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }

        public Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
        {
            var aggregate = puuids
                .Select(puuid => _performanceData.TryGetValue(puuid, out var data) ? data : null)
                .FirstOrDefault(data => data != null);

            return Task.FromResult(aggregate);
        }
    }
}
