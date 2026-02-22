using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Interfaces;

public interface IOverviewStatsRepository
{
    Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid);
    Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId);
    Task<LastMatchData?> GetLastMatchAsync(string puuid);
    Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid);
    Task<int?> GetCurrentLpAsync(string puuid, int queueId);
}

