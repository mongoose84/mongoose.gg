using System.Text.Json;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Riot
{
    public interface IRiotApiClient : IDisposable
    {
        Task<double> GetWinrateAsync(string puuid);
        Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default);
        Task<IList<LolMatch>> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default);
        Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default);
        Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default);
        Task<Summoner?> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default);
        Task<string> GetLolVersionAsync(CancellationToken ct = default);
    }
}