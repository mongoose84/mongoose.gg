using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Riot
{
    public interface IRiotApiClient : IDisposable
    {
        Task<double> GetWinrateAsync(string puuid);
        Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default);
        Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default);
        Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default);
        Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default);
        Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default);
        Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default);
        Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default);
        Task<string> GetLolVersionAsync(CancellationToken ct = default);
    }
}