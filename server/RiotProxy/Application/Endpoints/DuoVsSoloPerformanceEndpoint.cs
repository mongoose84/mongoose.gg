using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoVsSoloPerformanceDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoVsSoloPerformanceEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoVsSoloPerformanceEndpoint(string basePath)
        {
            Route = basePath + "/duo-vs-solo-performance/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();
                    
                    // Duo dashboard requires exactly 2 players
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo vs solo performance requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get duo performance for both players
                    var duoPerf1 = await matchParticipantRepo.GetDuoPerformanceByPuuIdsAsync(puuId1, puuId2, puuId1);
                    var duoPerf2 = await matchParticipantRepo.GetDuoPerformanceByPuuIdsAsync(puuId1, puuId2, puuId2);

                    // Get solo performance for both players
                    var soloPerf1 = await matchParticipantRepo.GetSoloPerformanceByPuuIdAsync(puuId1, puuId2);
                    var soloPerf2 = await matchParticipantRepo.GetSoloPerformanceByPuuIdAsync(puuId2, puuId1);

                    // Calculate duo averages (combined performance when playing together)
                    var duoWinRate = CalculateWinRate(duoPerf1, duoPerf2);
                    var duoGoldPerMin = CalculateGoldPerMin(duoPerf1, duoPerf2);
                    var duoKda = CalculateKda(duoPerf1, duoPerf2);

                    // Calculate solo averages for player 1
                    var soloAWinRate = soloPerf1 != null ? CalculateWinRate(soloPerf1) : 0.0;
                    var soloAGoldPerMin = soloPerf1 != null ? CalculateGoldPerMin(soloPerf1) : 0.0;
                    var soloAKda = soloPerf1 != null ? CalculateKda(soloPerf1) : 0.0;

                    // Calculate solo averages for player 2
                    var soloBWinRate = soloPerf2 != null ? CalculateWinRate(soloPerf2) : 0.0;
                    var soloBGoldPerMin = soloPerf2 != null ? CalculateGoldPerMin(soloPerf2) : 0.0;
                    var soloBKda = soloPerf2 != null ? CalculateKda(soloPerf2) : 0.0;

                    return Results.Ok(new DuoVsSoloPerformanceResponse(
                        DuoWinRate: duoWinRate,
                        SoloAWinRate: soloAWinRate,
                        SoloBWinRate: soloBWinRate,
                        DuoGoldPerMin: duoGoldPerMin,
                        SoloAGoldPerMin: soloAGoldPerMin,
                        SoloBGoldPerMin: soloBGoldPerMin,
                        DuoKda: duoKda,
                        SoloAKda: soloAKda,
                        SoloBKda: soloBKda
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting duo vs solo performance"
                        : "Invalid operation when getting duo vs solo performance");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo vs solo performance");
                }
            });
        }

        private static double CalculateWinRate(PlayerPerformanceRecord? perf1, PlayerPerformanceRecord? perf2 = null)
        {
            if (perf1 == null && perf2 == null) return 0.0;
            
            var totalGames = (perf1?.GamesPlayed ?? 0) + (perf2?.GamesPlayed ?? 0);
            var totalWins = (perf1?.Wins ?? 0) + (perf2?.Wins ?? 0);
            
            return totalGames > 0 ? Math.Round((double)totalWins / totalGames * 100, 1) : 0.0;
        }

        private static double CalculateGoldPerMin(PlayerPerformanceRecord? perf1, PlayerPerformanceRecord? perf2 = null)
        {
            if (perf1 == null && perf2 == null) return 0.0;
            
            var totalGold = (perf1?.TotalGoldEarned ?? 0) + (perf2?.TotalGoldEarned ?? 0);
            var totalDuration = (perf1?.TotalDurationSeconds ?? 0) + (perf2?.TotalDurationSeconds ?? 0);
            
            return totalDuration > 0 ? Math.Round(totalGold / (totalDuration / 60.0), 1) : 0.0;
        }

        private static double CalculateKda(PlayerPerformanceRecord? perf1, PlayerPerformanceRecord? perf2 = null)
        {
            if (perf1 == null && perf2 == null) return 0.0;
            
            var totalKills = (perf1?.TotalKills ?? 0) + (perf2?.TotalKills ?? 0);
            var totalDeaths = (perf1?.TotalDeaths ?? 0) + (perf2?.TotalDeaths ?? 0);
            var totalAssists = (perf1?.TotalAssists ?? 0) + (perf2?.TotalAssists ?? 0);
            
            return totalDeaths > 0 
                ? Math.Round((totalKills + totalAssists) / (double)totalDeaths, 2) 
                : totalKills + totalAssists; // Perfect KDA if no deaths
        }
    }
}

