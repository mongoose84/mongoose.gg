using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoLatestGameEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoLatestGameEndpoint(string basePath)
        {
            Route = basePath + "/duo-latest-game/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] GamerRepository gamerRepo,
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
                        return Results.BadRequest("Duo latest game requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var latestGame = await matchParticipantRepo.GetLatestGameTogetherByDuoPuuIdsAsync(puuId1, puuId2);

                    if (latestGame == null)
                    {
                        return Results.Ok(new { hasGame = false });
                    }

                    // Get gamer names for each player
                    var playersWithNames = new List<object>();
                    foreach (var player in latestGame.Players)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(player.Puuid);
                        playersWithNames.Add(new
                        {
                            puuid = player.Puuid,
                            gamerName = gamer?.GamerName ?? "Unknown",
                            win = player.Win,
                            role = player.Role,
                            championId = player.ChampionId,
                            championName = player.ChampionName,
                            kills = player.Kills,
                            deaths = player.Deaths,
                            assists = player.Assists
                        });
                    }

                    return Results.Ok(new
                    {
                        hasGame = true,
                        gameEndTimestamp = latestGame.GameEndTimestamp,
                        win = latestGame.Win,
                        players = playersWithNames
                    });
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid argument when getting duo latest game");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo latest game");
                }
            });
        }
    }
}

