using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Interfaces;

public interface IOverviewStatsRepository
{
    Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid);
    Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(IReadOnlyList<string> puuids);
    Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId);
    Task<List<MatchResultData>> GetLast20MatchesAsync(IReadOnlyList<string> puuids, int queueId);
    Task<LastMatchData?> GetLastMatchAsync(string puuid);
    Task<LastMatchData?> GetLastMatchAsync(IReadOnlyList<string> puuids);
    Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid);
    Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(IReadOnlyList<string> puuids);
    Task<int?> GetCurrentLpAsync(string puuid, int queueId);
    Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
    Task<SurvivalStatsData> GetSurvivalStatsAsync(
        IReadOnlyList<string> puuids,
        int lowDeathThreshold,
        int highDeathThreshold,
        int lastNGames = 20);
}

