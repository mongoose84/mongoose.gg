using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ChampionSynergyDto;

namespace RiotProxy.Application.Endpoints;

public sealed class ChampionSynergyEndpoint : IEndpoint
{
    public string Route { get; }

    public ChampionSynergyEndpoint(string basePath)
    {
        Route = basePath + "/champion-synergy/{userId}";
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
                    return Results.BadRequest("Champion synergy requires exactly 2 players");
                }

                var puuId1 = distinctPuuIds[0];
                var puuId2 = distinctPuuIds[1];

                // Get the most common game mode for filtering
                var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                var mostCommonGameMode = duoStats?.MostCommonQueueType;

                // Get champion synergy data
                var synergyRecords = await matchParticipantRepo.GetChampionSynergyByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);

                // Convert to response format
                var synergies = synergyRecords
                    .Select(s => new ChampionPairStats(
                        ChampionId1: s.ChampionId1,
                        ChampionName1: s.ChampionName1,
                        ChampionId2: s.ChampionId2,
                        ChampionName2: s.ChampionName2,
                        GamesPlayed: s.GamesPlayed,
                        Wins: s.Wins,
                        Losses: s.GamesPlayed - s.Wins,
                        Winrate: s.GamesPlayed > 0 ? Math.Round((double)s.Wins / s.GamesPlayed * 100, 1) : 0.0
                    ))
                    .ToList();

                return Results.Ok(new ChampionSynergyResponse(Synergies: synergies));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest(ex is ArgumentException
                    ? "Invalid argument when getting champion synergy"
                    : "Invalid operation when getting champion synergy");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest("Error when getting champion synergy");
            }
        });
    }
}

