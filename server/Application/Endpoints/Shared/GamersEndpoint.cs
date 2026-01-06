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

                        // Fetch all aggregate stats in a single query
                        var stats = await matchParticipantRepo.GetAggregateStatsByPuuIdAsync(puuId);
                        var latestGame = await matchParticipantRepo.GetLatestGameDetailsByPuuIdAsync(puuId);

                        gamer.Stats = new GamerStats
                        {
                            Wins = stats.Wins,
                            TotalKills = stats.TotalKills,
                            TotalDeaths = stats.TotalDeaths,
                            TotalAssists = stats.TotalAssists,
                            TotalMatches = stats.TotalMatches,
                            TotalCreepScore = stats.TotalCreepScore,
                            TotalGoldEarned = stats.TotalGoldEarned,
                            TotalDurationPlayedSeconds = stats.TotalDurationPlayedSeconds,
                            TotalCreepScoreExcludingAram = stats.TotalCreepScoreExcludingAram,
                            TotalGoldEarnedExcludingAram = stats.TotalGoldEarnedExcludingAram,
                            TotalDurationPlayedExcludingAramSeconds = stats.TotalDurationExcludingAramSeconds
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