using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamLatestGameEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamLatestGameEndpoint(string basePath)
        {
            Route = basePath + "/team-latest-game/{userId}";
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
                    
                    if (distinctPuuIds.Length < 3)
                    {
                        return Results.BadRequest("Team latest game requires at least 3 players");
                    }

                    var latestGame = await matchParticipantRepo.GetLatestGameTogetherByTeamPuuIdsAsync(distinctPuuIds);

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
                    return Results.BadRequest("Invalid argument when getting team latest game");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team latest game");
                }
            });
        }
    }
}

