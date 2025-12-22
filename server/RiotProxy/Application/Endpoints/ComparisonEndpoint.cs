using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ComparisonDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class ComparisonEndpoint : IEndpoint
    {
        public string Route { get; }

        public ComparisonEndpoint(string basePath)
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
                        Winrate: [],
                        Kda: [],
                        CsPrMin: [],
                        GoldPrMin: [],
                        GamesPlayed: []
                    );

                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();
                    
                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(emptyComparisonRequest);
                    }
                    var winrateRecords = new List<GamerRecord>();
                    var kdaRecords = new List<GamerRecord>();
                    var csPrMinRecords = new List<GamerRecord>();
                    var goldPrMinRecords = new List<GamerRecord>();
                    var gamesPlayedRecords = new List<GamerRecord>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var totalDurationMinutes = await matchParticipantRepo.GetTotalDurationPlayedByPuuidAsync(puuId) / 60.0;

                        var winrate = await GetWinrateAsync(matchParticipantRepo, puuId);
                        winrateRecords.Add(new GamerRecord(winrate, gamerName));
                        var kda = await GetKdaAsync(matchParticipantRepo, puuId);
                        kdaRecords.Add(new GamerRecord(kda, gamerName));
                        var csPrMin = await GetCsPrMinAsync(matchParticipantRepo, puuId, totalDurationMinutes);
                        csPrMinRecords.Add(new GamerRecord(csPrMin, gamerName));
                        var goldPrMin = await GetGoldPrMinAsync(matchParticipantRepo, puuId, totalDurationMinutes);
                        goldPrMinRecords.Add(new GamerRecord(goldPrMin, gamerName));
                        var gamesPlayed = await matchParticipantRepo.GetMatchesCountByPuuIdAsync(puuId);
                        gamesPlayedRecords.Add(new GamerRecord(gamesPlayed, gamerName));
                    }
                    
                    var comparisonRequest = new ComparisonRequest(
                        Winrate: winrateRecords.OrderByDescending(r => r.Value).ToList(),
                        Kda: kdaRecords.OrderByDescending(r => r.Value).ToList(),
                        CsPrMin:  csPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GoldPrMin: goldPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GamesPlayed: gamesPlayedRecords.OrderByDescending(r => r.Value).ToList()
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

        private async Task<double> GetWinrateAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var totalMatches = await repo.GetMatchesCountByPuuIdAsync(puuId);
            if (totalMatches == 0) return 0;
            
            var wins = await repo.GetWinsByPuuIdAsync(puuId);
            return (double)wins / totalMatches * 100;
        }

        private async Task<double> GetKdaAsync(LolMatchParticipantRepository repo, string puuId)
        {
            var (kills, deaths, assists) = (
                await repo.GetTotalKillsByPuuIdAsync(puuId),
                await repo.GetTotalDeathsByPuuIdAsync(puuId),
                await repo.GetTotalAssistsByPuuIdAsync(puuId)
            );

            if (deaths == 0 && (kills + assists) == 0) return 0;

            if (deaths == 0) return kills + assists;
            
            return deaths == 0 ? 0 : (double)(kills + assists) / deaths;
        }
        private async Task<double> GetCsPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationMinutes)
        {
            var totalCreepScore = await repo.GetTotalCreepScoreByPuuIdAsync(puuId);
            return totalDurationMinutes == 0 ? 0 : totalCreepScore / totalDurationMinutes;
        }

        private async Task<double> GetGoldPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationMinutes)
        {
            var totalGoldEarned = await repo.GetTotalGoldEarnedByPuuIdAsync(puuId);
            return totalDurationMinutes == 0 ? 0 : totalGoldEarned / totalDurationMinutes;
        }
        
    }
}