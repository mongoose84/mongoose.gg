using System.Collections.Concurrent;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake overview stats repository for testing.
    /// </summary>
    internal sealed class FakeOverviewStatsRepository : OverviewStatsRepository
    {
        private readonly ConcurrentDictionary<string, LastMatchData> _lastMatchByPuuid = new();
        private readonly ConcurrentDictionary<string, MostPlayedChampionData> _mostPlayedChampionByPuuid = new();
        private readonly ConcurrentDictionary<string, (int GamesToday, int WinsToday, int LossesToday, int GamesThisWeek, int WinsThisWeek, int LossesThisWeek)> _sessionDataByPuuid = new();
        private SurvivalStatsData _survivalStats = new SurvivalStatsData(0, null, null, 0, 0, 0);
        private ChampionPoolData _championPool = new ChampionPoolData([], []);

        public FakeOverviewStatsRepository() : base(null!, null!) { }

        public override Task<LastMatchData?> GetLastMatchAsync(string puuid)
        {
            _lastMatchByPuuid.TryGetValue(puuid, out var lastMatch);
            return Task.FromResult(lastMatch);
        }

        public override Task<LastMatchData?> GetLastMatchAsync(IReadOnlyList<string> puuids)
        {
            var match = puuids
                .Where(puuid => _lastMatchByPuuid.TryGetValue(puuid, out _))
                .Select(puuid => _lastMatchByPuuid[puuid])
                .OrderByDescending(lastMatch => lastMatch.GameStartTime)
                .FirstOrDefault();

            return Task.FromResult(match);
        }

        public override Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid)
        {
            _mostPlayedChampionByPuuid.TryGetValue(puuid, out var mostPlayedChampion);
            return Task.FromResult(mostPlayedChampion);
        }

        public override Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(IReadOnlyList<string> puuids)
        {
            foreach (var puuid in puuids)
            {
                if (_mostPlayedChampionByPuuid.TryGetValue(puuid, out var mostPlayedChampion))
                {
                    return Task.FromResult<MostPlayedChampionData?>(mostPlayedChampion);
                }
            }

            return Task.FromResult<MostPlayedChampionData?>(null);
        }

        /// <summary>
        /// Sets the last match for a player.
        /// </summary>
        public void SetLastMatch(string puuid, string matchId, int championId, string championName,
            bool win, int kills, int deaths, int assists, long gameStartTime, int queueId = 420)
        {
            _lastMatchByPuuid[puuid] = new LastMatchData(matchId, championId, championName, win, kills, deaths, assists, gameStartTime, queueId);
        }

        /// <summary>
        /// Sets the most played champion for a player.
        /// </summary>
        public void SetMostPlayedChampion(string puuid, string championName, int gamesPlayed)
        {
            _mostPlayedChampionByPuuid[puuid] = new MostPlayedChampionData(championName, gamesPlayed);
        }

        public override Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc)
        {
            var perAccount = puuids
                .Where(puuid => _sessionDataByPuuid.TryGetValue(puuid, out _))
                .Select(puuid =>
                {
                    var data = _sessionDataByPuuid[puuid];
                    return new PerAccountSessionData(
                        Puuid: puuid,
                        GamesToday: data.GamesToday,
                        WinsToday: data.WinsToday,
                        LossesToday: data.LossesToday,
                        AvgKdaToday: null,
                        BestChampionName: null,
                        BestChampionWins: 0,
                        BestChampionLosses: 0,
                        BestChampionAvgKda: 0,
                        GamesThisWeek: data.GamesThisWeek,
                        WinsThisWeek: data.WinsThisWeek,
                        LossesThisWeek: data.LossesThisWeek,
                        AvgKdaThisWeek: null
                    );
                })
                .ToList();

            return Task.FromResult(new SessionStatsData(perAccount));
        }

        public override Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lowDeathThreshold, int highDeathThreshold, int lastNGames = 20)
        {
            return Task.FromResult(_survivalStats);
        }

        /// <summary>
        /// Configures per-account session data for tests.
        /// </summary>
        public void AddSessionData(string puuid, int gamesToday, int winsToday, int lossesToday,
            int gamesThisWeek, int winsThisWeek, int lossesThisWeek)
        {
            _sessionDataByPuuid[puuid] = (gamesToday, winsToday, lossesToday, gamesThisWeek, winsThisWeek, lossesThisWeek);
        }

        /// <summary>
        /// Overrides the survival stats returned by the fake.
        /// </summary>
        public void SetSurvivalStats(SurvivalStatsData survivalStats)
        {
            _survivalStats = survivalStats;
        }

        public override Task<ChampionPoolData> GetChampionPoolStatsAsync(IReadOnlyList<string> puuids)
        {
            return Task.FromResult(_championPool);
        }

        /// <summary>
        /// Overrides the champion pool data returned by the fake.
        /// </summary>
        public void SetChampionPool(ChampionPoolData championPool)
        {
            _championPool = championPool;
        }
    }
}
