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
                        
                        // Fetch all aggregate stats in a single query
                        var stats = await matchParticipantRepo.GetAggregateStatsByPuuIdAsync(puuId);
                        
                        var totalDurationExcludingAramMinutes = stats.TotalDurationExcludingAramSeconds / 60.0;
                        var gamesPlayed = stats.TotalMatches;

                        // Calculate derived metrics from aggregate stats
                        var winrate = gamesPlayed > 0 ? (double)stats.Wins / gamesPlayed * 100 : 0;
                        winrateRecords.Add(new GamerRecord(winrate, gamerName));

                        var kda = stats.TotalDeaths == 0 
                            ? (stats.TotalKills + stats.TotalAssists) 
                            : (double)(stats.TotalKills + stats.TotalAssists) / stats.TotalDeaths;
                        kdaRecords.Add(new GamerRecord(kda, gamerName));

                        var csPrMin = totalDurationExcludingAramMinutes > 0 
                            ? stats.TotalCreepScoreExcludingAram / totalDurationExcludingAramMinutes : 0;
                        csPrMinRecords.Add(new GamerRecord(csPrMin, gamerName));

                        var goldPrMin = totalDurationExcludingAramMinutes > 0 
                            ? stats.TotalGoldEarnedExcludingAram / totalDurationExcludingAramMinutes : 0;
                        goldPrMinRecords.Add(new GamerRecord(goldPrMin, gamerName));

                        gamesPlayedRecords.Add(new GamerRecord(gamesPlayed, gamerName));

                        // Average metrics per game
                        var avgKills = gamesPlayed > 0 ? Math.Round((double)stats.TotalKills / gamesPlayed, 1) : 0;
                        avgKillsRecords.Add(new GamerRecord(avgKills, gamerName));

                        var avgDeaths = gamesPlayed > 0 ? Math.Round((double)stats.TotalDeaths / gamesPlayed, 1) : 0;
                        avgDeathsRecords.Add(new GamerRecord(avgDeaths, gamerName));

                        var avgAssists = gamesPlayed > 0 ? Math.Round((double)stats.TotalAssists / gamesPlayed, 1) : 0;
                        avgAssistsRecords.Add(new GamerRecord(avgAssists, gamerName));

                        var avgTimeDeadSeconds = gamesPlayed > 0 ? Math.Round((double)stats.TotalTimeBeingDeadSeconds / gamesPlayed, 1) : 0;
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
    }
}