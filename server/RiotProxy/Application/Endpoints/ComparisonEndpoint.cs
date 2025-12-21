using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ComparisionDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class OverallStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        public OverallStatsEndpoint(string basePath)
        {
            Route = basePath + "/comparison/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo
                ) =>
            {
                try
                {
                    var emptyComparisonRequest = new ComparisonRequest(
                        Winrate: new Winrate(0, ""),
                        Kda: new Kda(0, ""),
                        CsPrMin: new CsPrMin(0, ""),
                        GoldPrMin: new GoldPrMin(0, ""),
                        GamesPlayed: new GamesPlayed(0, "")
                    );

                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuids = await userGamerRepo.GetGamersPuuidByUserIdAsync(userIdInt);
                    var distinctPuuids = (puuids ?? []).Distinct().ToArray();
                    
                    if (distinctPuuids.Length == 0)
                    {
                        return Results.Ok(emptyComparisonRequest);
                    }

                    var winrateTracker = new MetricTracker<double>();
                    var kdaTracker = new MetricTracker<double>();
                    var csPrMinTracker = new MetricTracker<double>();
                    var goldPrMinTracker = new MetricTracker<double>();
                    var gamesPlayedTracker = new MetricTracker<int>();

                    foreach (var puuid in distinctPuuids)
                    {
                        var gamer = await gamerRepo.GetByPuuidAsync(puuid);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuid} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var totalDurationMinutes = await matchParticipantRepo.GetTotalDurationPlayedByPuuidAsync(puuid) / 60.0;

                        winrateTracker.Update(await GetWinrateAsync(matchParticipantRepo, puuid), gamerName);
                        kdaTracker.Update(await GetKdaAsync(matchParticipantRepo, puuid), gamerName);
                        csPrMinTracker.Update(await GetPerMinuteStatAsync(matchParticipantRepo.GetTotalCreepScoreByPuuidAsync, puuid, totalDurationMinutes), gamerName);
                        goldPrMinTracker.Update(await GetPerMinuteStatAsync(matchParticipantRepo.GetTotalGoldEarnedByPuuidAsync, puuid, totalDurationMinutes), gamerName);
                        gamesPlayedTracker.Update(await matchParticipantRepo.GetMatchesCountByPuuidAsync(puuid), gamerName);
                    }

                    var comparisonRequest = new ComparisonRequest(
                        Winrate: new Winrate(winrateTracker.Difference, winrateTracker.HighestGamer),
                        Kda: new Kda(kdaTracker.Difference, kdaTracker.HighestGamer),
                        CsPrMin: new CsPrMin(csPrMinTracker.Difference, csPrMinTracker.HighestGamer),
                        GoldPrMin: new GoldPrMin(goldPrMinTracker.Difference, goldPrMinTracker.HighestGamer),
                        GamesPlayed: new GamesPlayed((int)gamesPlayedTracker.Difference, gamesPlayedTracker.HighestGamer)
                    );

                    return Results.Ok(comparisonRequest);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting gamers" 
                        : "Invalid operation when getting gamers");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting gamers");
                }
            });
        }

        private static async Task<double> GetWinrateAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalMatches = await repo.GetMatchesCountByPuuidAsync(puuId);
            if (totalMatches == 0) return 0;
            
            var wins = await repo.GetWinsByPuuidAsync(puuId);
            return (double)wins / totalMatches * 100;
        }

        private static async Task<double> GetKdaAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var (kills, deaths, assists) = (
                await repo.GetTotalKillsByPuuidAsync(puuId),
                await repo.GetTotalDeathsByPuuidAsync(puuId),
                await repo.GetTotalAssistsByPuuidAsync(puuId)
            );
            
            return deaths == 0 ? 0 : (double)(kills + assists) / deaths;
        }

        private static async Task<double> GetPerMinuteStatAsync(
            Func<string, Task<int>> getStatAsync, 
            string puuId, 
            double totalDurationMinutes)
        {
            if (totalDurationMinutes == 0) return 0;
            
            var totalStat = await getStatAsync(puuId);
            return totalStat / totalDurationMinutes;
        }

        /// <summary>
        /// Generic tracker for finding highest/lowest values with associated gamer names.
        /// </summary>
        private sealed class MetricTracker<T> where T : struct, IComparable<T>
        {
            private T? _highest;
            private T? _lowest;
            
            public string HighestGamer { get; private set; } = "";
            
            public double Difference => _highest.HasValue && _lowest.HasValue
                ? Convert.ToDouble(_highest.Value) - Convert.ToDouble(_lowest.Value)
                : 0;

            public void Update(T value, string gamerName)
            {
                if (!_highest.HasValue || value.CompareTo(_highest.Value) > 0)
                {
                    _highest = value;
                    HighestGamer = gamerName;
                }
                
                if (!_lowest.HasValue || value.CompareTo(_lowest.Value) < 0)
                {
                    _lowest = value;
                }
            }
        }
    }
}