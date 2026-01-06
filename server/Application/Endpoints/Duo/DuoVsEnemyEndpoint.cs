using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoVsEnemyDto;

namespace RiotProxy.Application.Endpoints;

public sealed class DuoVsEnemyEndpoint : IEndpoint
{
    public string Route { get; }

    public DuoVsEnemyEndpoint(string basePath)
    {
        Route = basePath + "/duo-vs-enemy/{userId}";
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
                    return Results.BadRequest("Duo vs enemy requires exactly 2 players");
                }

                var puuId1 = distinctPuuIds[0];
                var puuId2 = distinctPuuIds[1];

                // Get the most common game mode for filtering
                var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                var mostCommonGameMode = duoStats?.MostCommonQueueType;

                // Get duo vs enemy data
                var matchupRecords = await matchParticipantRepo.GetDuoVsEnemyByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);

                // Convert to response format
                var matchups = matchupRecords
                    .Select(m => new DuoVsEnemyStats(
                        DuoChampionId1: m.DuoChampionId1,
                        DuoChampionName1: m.DuoChampionName1,
                        DuoLane1: m.DuoLane1,
                        DuoChampionId2: m.DuoChampionId2,
                        DuoChampionName2: m.DuoChampionName2,
                        DuoLane2: m.DuoLane2,
                        EnemyLane: m.EnemyLane,
                        EnemyChampionId: m.EnemyChampionId,
                        EnemyChampionName: m.EnemyChampionName,
                        GamesPlayed: m.GamesPlayed,
                        Wins: m.Wins,
                        Losses: m.GamesPlayed - m.Wins,
                        Winrate: m.GamesPlayed > 0 ? Math.Round((double)m.Wins / m.GamesPlayed * 100, 1) : 0.0
                    ))
                    .ToList();

                return Results.Ok(new DuoVsEnemyResponse(Matchups: matchups));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest(ex is ArgumentException
                    ? "Invalid argument when getting duo vs enemy"
                    : "Invalid operation when getting duo vs enemy");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest("Error when getting duo vs enemy");
            }
        });
    }
}

