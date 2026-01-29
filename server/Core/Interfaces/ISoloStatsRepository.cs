using static RiotProxy.Application.DTOs.SoloSummaryDto;
using static RiotProxy.Application.DTOs.SoloMatchupsDto;

namespace RiotProxy.Core.Interfaces;

public interface ISoloStatsRepository
{
    Task<SoloDashboardResponse?> GetSoloDashboardAsync(string puuid, string? queueType = null, string? timeRange = null);
    Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null);
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91);
    Task<IList<LpTrendPoint>> GetLpTrendAsync(string puuid, string? queueType = null, int limit = 100);
    Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(string puuid, string? queueType = null, string? timeRange = null);
}

