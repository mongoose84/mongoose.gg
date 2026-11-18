using System.Collections.Concurrent;
using System.Text.Json;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake Riot API client for testing.
    /// </summary>
    internal sealed class FakeRiotApiClient : IRiotApiClient
    {
        private readonly ConcurrentDictionary<string, string> _puuidByRiotId = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _notFoundRiotIds = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;

        private void NotifyRateLimitWaitStarted(RateLimitWaitEventArgs args)
        {
            RateLimitWaitStarted?.Invoke(this, args);
        }

        public Task<double> GetWinrateAsync(string puuid)
        {
            return Task.FromResult(50.0);
        }

        public Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default)
        {
            var key = BuildRiotIdKey(gameName, tagLine);
            if (_notFoundRiotIds.ContainsKey(key))
            {
                throw new HttpRequestException("Not Found", null, System.Net.HttpStatusCode.NotFound);
            }

            if (_puuidByRiotId.TryGetValue(key, out var mappedPuuid))
            {
                return Task.FromResult(mappedPuuid);
            }

            var generated = $"test-puuid-{gameName.Trim().ToLowerInvariant()}-{tagLine.Trim().ToLowerInvariant()}";
            _puuidByRiotId[key] = generated;
            return Task.FromResult(generated);
        }

        public Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<string> GetLolVersionAsync(CancellationToken ct = default)
        {
            return Task.FromResult("14.1.1");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void MapRiotIdToPuuid(string gameName, string tagLine, string puuid)
        {
            _puuidByRiotId[BuildRiotIdKey(gameName, tagLine)] = puuid;
        }

        public void SimulateRiotNotFound(string gameName, string tagLine)
        {
            _notFoundRiotIds[BuildRiotIdKey(gameName, tagLine)] = 0;
        }

        private static string BuildRiotIdKey(string gameName, string tagLine)
        {
            return $"{gameName.Trim().ToLowerInvariant()}#{tagLine.Trim().ToLowerInvariant()}";
        }
    }
}
