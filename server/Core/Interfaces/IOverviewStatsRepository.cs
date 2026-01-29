using RiotProxy.Application.DTOs.Overview;

namespace RiotProxy.Core.Interfaces;

public interface IOverviewStatsRepository
{
    Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid);
    Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId);
    Task<LastMatchData?> GetLastMatchAsync(string puuid);
    Task<int?> GetCurrentLpAsync(string puuid, int queueId);
}

