using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Application.Endpoints
{
    public sealed class GamersEndpoint : IEndpoint
    {
        public string Route { get; }

        public GamersEndpoint(string basePath)
        {
            Route = basePath + "/gamers/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo,
                [FromServices] IRiotApiClient riotApiClient
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var lolVersion = await riotApiClient.GetLolVersionAsync();
                    var gamers = new List<Gamer>();
                    foreach (var puuId in puuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }
                        gamer.IconUrl = $"https://ddragon.leagueoflegends.com/cdn/{lolVersion}/img/profileicon/{gamer.IconId}.png";

                        // Runtime of this is a N+1 query, could be optimized if needed
                        var totalMatches = await matchParticipantRepo.GetMatchesCountByPuuIdAsync(puuId);
                        var wins = await matchParticipantRepo.GetWinsByPuuIdAsync(puuId);
                        var totalKills = await matchParticipantRepo.GetTotalKillsByPuuIdAsync(puuId);
                        var totalDeaths = await matchParticipantRepo.GetTotalDeathsByPuuIdAsync(puuId);
                        var totalAssists = await matchParticipantRepo.GetTotalAssistsByPuuIdAsync(puuId);
                        var totalCreepScore = await matchParticipantRepo.GetTotalCreepScoreByPuuIdAsync(puuId);
                        var totalGoldEarned = await matchParticipantRepo.GetTotalGoldEarnedByPuuIdAsync(puuId);
                        var totalDurationPlayedSeconds = await matchParticipantRepo.GetTotalDurationPlayedByPuuidAsync(puuId);
                        var latestGame = await matchParticipantRepo.GetLatestGameDetailsByPuuIdAsync(puuId);

                        // ARAM-excluding stats for accurate CS/min and Gold/min calculations
                        var totalCreepScoreExcludingAram = await matchParticipantRepo.GetTotalCreepScoreExcludingAramByPuuIdAsync(puuId);
                        var totalGoldEarnedExcludingAram = await matchParticipantRepo.GetTotalGoldEarnedExcludingAramByPuuIdAsync(puuId);
                        var totalDurationExcludingAramSeconds = await matchParticipantRepo.GetTotalDurationPlayedExcludingAramByPuuidAsync(puuId);

                        gamer.Stats = new GamerStats
                        {
                            Wins = wins,
                            TotalKills = totalKills,
                            TotalDeaths = totalDeaths,
                            TotalAssists = totalAssists,
                            TotalMatches = totalMatches,
                            TotalCreepScore = totalCreepScore,
                            TotalGoldEarned = totalGoldEarned,
                            TotalDurationPlayedSeconds = totalDurationPlayedSeconds,
                            TotalCreepScoreExcludingAram = totalCreepScoreExcludingAram,
                            TotalGoldEarnedExcludingAram = totalGoldEarnedExcludingAram,
                            TotalDurationPlayedExcludingAramSeconds = totalDurationExcludingAramSeconds
                        };
                        gamer.LatestGame = latestGame;
                        gamers.Add(gamer);
                    }
                
                    return Results.Ok(gamers);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid operation when getting gamers");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Invalid argument when getting gamers");
                }
                catch (Exception ex) when (
                    !(ex is OutOfMemoryException) &&
                    !(ex is StackOverflowException) &&
                    !(ex is ThreadAbortException)
                )
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return Results.BadRequest("Error when getting gamers");
                }
            });
        }
        
    }
}