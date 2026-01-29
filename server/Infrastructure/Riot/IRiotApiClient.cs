using System.Text.Json;
using RiotProxy.Infrastructure.Riot.LimitHandler;

namespace RiotProxy.Infrastructure.Riot;

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

    /// <summary>
    /// TEMPORARY: Event raised when rate limiting causes a wait.
    /// Includes PUUID context to identify which account triggered the rate limit.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;
}