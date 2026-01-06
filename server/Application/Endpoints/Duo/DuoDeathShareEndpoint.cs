using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoDeathShareEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoDeathShareEndpoint(string basePath)
        {
            Route = basePath + "/duo-death-share/{userId}";
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
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();
                    
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo death share requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var deathShareRecords = await matchParticipantRepo.GetDuoDeathShareAsync(puuId1, puuId2, gameMode);

                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    var totalDeaths = deathShareRecords.Sum(r => r.TotalDeaths);

                    var players = deathShareRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new DuoPlayerDeathShare(
                            PlayerName: playerNames[r.PuuId],
                            Deaths: r.TotalDeaths,
                            DeathShare: totalDeaths > 0 
                                ? Math.Round((double)r.TotalDeaths / totalDeaths * 100, 1) 
                                : 0,
                            AvgDeathsPerGame: r.GamesPlayed > 0 
                                ? Math.Round((double)r.TotalDeaths / r.GamesPlayed, 1) 
                                : 0
                        ))
                        .OrderByDescending(p => p.Deaths)
                        .ToList();

                    return Results.Ok(new DuoDeathShareResponse(
                        Players: players,
                        TotalDeaths: totalDeaths
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo death share");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo death share");
                }
            });
        }
    }
}

