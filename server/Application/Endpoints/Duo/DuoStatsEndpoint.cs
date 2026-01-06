using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoStatsDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoStatsEndpoint(string basePath)
        {
            Route = basePath + "/duo-stats/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] DuoStatsRepository duoStatsRepo
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
                        return Results.BadRequest("Duo stats require exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await duoStatsRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);

                    if (duoStats == null)
                    {
                        // No games played together
                        return Results.Ok(new DuoStatsResponse(
                            GamesPlayed: 0,
                            Wins: 0,
                            WinRate: 0.0,
                            QueueType: "No games together"
                        ));
                    }

                    var winRate = duoStats.GamesPlayed > 0
                        ? Math.Round((double)duoStats.Wins / duoStats.GamesPlayed * 100, 1)
                        : 0.0;

                    // Map game mode to user-friendly queue type
                    var queueType = MapGameModeToQueueType(duoStats.MostCommonQueueType);

                    return Results.Ok(new DuoStatsResponse(
                        GamesPlayed: duoStats.GamesPlayed,
                        Wins: duoStats.Wins,
                        WinRate: winRate,
                        QueueType: queueType
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting duo stats"
                        : "Invalid operation when getting duo stats");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo stats");
                }
            });
        }

        private static string MapGameModeToQueueType(string? gameMode)
        {
            return gameMode switch
            {
                "CLASSIC" => "Ranked",
                "ARAM" => "ARAM",
                "URF" => "URF",
                "NEXUSBLITZ" => "Nexus Blitz",
                "ONEFORALL" => "One For All",
                "TUTORIAL" => "Tutorial",
                null => "Excluding ARAM",
                _ => gameMode
            };
        }
    }
}

