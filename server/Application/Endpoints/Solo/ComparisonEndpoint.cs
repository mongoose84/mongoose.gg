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
                        GamesPlayed: [],
                        AvgKills: [],
                        AvgDeaths: [],
                        AvgAssists: [],
                        AvgTimeDeadSeconds: []
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
                    var avgKillsRecords = new List<GamerRecord>();
                    var avgDeathsRecords = new List<GamerRecord>();
                    var avgAssistsRecords = new List<GamerRecord>();
                    var avgTimeDeadSecondsRecords = new List<GamerRecord>();

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
                        var gamesPlayed = await matchParticipantRepo.GetMatchesCountByPuuIdAsync(puuId);

                        // Use ARAM-excluding duration for CS/min and Gold/min calculations
                        var totalDurationExcludingAramMinutes = await matchParticipantRepo.GetTotalDurationPlayedExcludingAramByPuuidAsync(puuId) / 60.0;

                        var winrate = await GetWinrateAsync(matchParticipantRepo, puuId);
                        winrateRecords.Add(new GamerRecord(winrate, gamerName));
                        var kda = await GetKdaAsync(matchParticipantRepo, puuId);
                        kdaRecords.Add(new GamerRecord(kda, gamerName));
                        var csPrMin = await GetCsPrMinAsync(matchParticipantRepo, puuId, totalDurationExcludingAramMinutes);
                        csPrMinRecords.Add(new GamerRecord(csPrMin, gamerName));
                        var goldPrMin = await GetGoldPrMinAsync(matchParticipantRepo, puuId, totalDurationExcludingAramMinutes);
                        goldPrMinRecords.Add(new GamerRecord(goldPrMin, gamerName));
                        gamesPlayedRecords.Add(new GamerRecord(gamesPlayed, gamerName));

                        // New metrics for radar chart
                        var avgKills = await GetAvgKillsAsync(matchParticipantRepo, puuId, gamesPlayed);
                        avgKillsRecords.Add(new GamerRecord(avgKills, gamerName));
                        var avgDeaths = await GetAvgDeathsAsync(matchParticipantRepo, puuId, gamesPlayed);
                        avgDeathsRecords.Add(new GamerRecord(avgDeaths, gamerName));
                        var avgAssists = await GetAvgAssistsAsync(matchParticipantRepo, puuId, gamesPlayed);
                        avgAssistsRecords.Add(new GamerRecord(avgAssists, gamerName));
                        var avgTimeDeadSeconds = await GetAvgTimeDeadSecondsAsync(matchParticipantRepo, puuId, gamesPlayed);
                        avgTimeDeadSecondsRecords.Add(new GamerRecord(avgTimeDeadSeconds, gamerName));
                    }
                    
                    var comparisonRequest = new ComparisonRequest(
                        Winrate: winrateRecords.OrderByDescending(r => r.Value).ToList(),
                        Kda: kdaRecords.OrderByDescending(r => r.Value).ToList(),
                        CsPrMin:  csPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GoldPrMin: goldPrMinRecords.OrderByDescending(r => r.Value).ToList(),
                        GamesPlayed: gamesPlayedRecords.OrderByDescending(r => r.Value).ToList(),
                        AvgKills: avgKillsRecords.OrderByDescending(r => r.Value).ToList(),
                        AvgDeaths: avgDeathsRecords.OrderBy(r => r.Value).ToList(), // Lower is better
                        AvgAssists: avgAssistsRecords.OrderByDescending(r => r.Value).ToList(),
                        AvgTimeDeadSeconds: avgTimeDeadSecondsRecords.OrderBy(r => r.Value).ToList() // Lower is better
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
        private async Task<double> GetCsPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationExcludingAramMinutes)
        {
            // Use ARAM-excluding creep score for accurate CS/min calculation
            var totalCreepScore = await repo.GetTotalCreepScoreExcludingAramByPuuIdAsync(puuId);
            return totalDurationExcludingAramMinutes == 0 ? 0 : totalCreepScore / totalDurationExcludingAramMinutes;
        }

        private async Task<double> GetGoldPrMinAsync(LolMatchParticipantRepository repo, string puuId, double totalDurationExcludingAramMinutes)
        {
            // Use ARAM-excluding gold earned for accurate Gold/min calculation
            var totalGoldEarned = await repo.GetTotalGoldEarnedExcludingAramByPuuIdAsync(puuId);
            return totalDurationExcludingAramMinutes == 0 ? 0 : totalGoldEarned / totalDurationExcludingAramMinutes;
        }

        private async Task<double> GetAvgKillsAsync(LolMatchParticipantRepository repo, string puuId, int gamesPlayed)
        {
            if (gamesPlayed == 0) return 0;
            var totalKills = await repo.GetTotalKillsByPuuIdAsync(puuId);
            return Math.Round((double)totalKills / gamesPlayed, 1);
        }

        private async Task<double> GetAvgDeathsAsync(LolMatchParticipantRepository repo, string puuId, int gamesPlayed)
        {
            if (gamesPlayed == 0) return 0;
            var totalDeaths = await repo.GetTotalDeathsByPuuIdAsync(puuId);
            return Math.Round((double)totalDeaths / gamesPlayed, 1);
        }

        private async Task<double> GetAvgAssistsAsync(LolMatchParticipantRepository repo, string puuId, int gamesPlayed)
        {
            if (gamesPlayed == 0) return 0;
            var totalAssists = await repo.GetTotalAssistsByPuuIdAsync(puuId);
            return Math.Round((double)totalAssists / gamesPlayed, 1);
        }

        private async Task<double> GetAvgTimeDeadSecondsAsync(LolMatchParticipantRepository repo, string puuId, int gamesPlayed)
        {
            if (gamesPlayed == 0) return 0;
            var totalTimeDeadSeconds = await repo.GetTotalTimeBeingDeadSecondsByPuuIdAsync(puuId);
            return Math.Round((double)totalTimeDeadSeconds / gamesPlayed, 1);
        }

    }
}