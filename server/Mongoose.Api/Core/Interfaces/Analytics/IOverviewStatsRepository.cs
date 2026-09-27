using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Interfaces;

public interface IOverviewStatsRepository
{
    Task<LastMatchData?> GetLastMatchAsync(string puuid);
    Task<LastMatchData?> GetLastMatchAsync(IReadOnlyList<string> puuids);
    Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid);
    Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(IReadOnlyList<string> puuids);
    Task<ChampionPoolData> GetChampionPoolStatsAsync(IReadOnlyList<string> puuids);
    Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
    Task<SurvivalStatsData> GetSurvivalStatsAsync(
        IReadOnlyList<string> puuids,
        int lowDeathThreshold,
        int highDeathThreshold,
        int lastNGames = 20);
}

