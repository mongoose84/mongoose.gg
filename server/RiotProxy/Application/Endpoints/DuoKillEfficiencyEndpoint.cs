using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoKillEfficiencyDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoKillEfficiencyEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoKillEfficiencyEndpoint(string basePath)
        {
            Route = basePath + "/duo-kill-efficiency/{userId}";
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
                    
                    // Duo dashboard requires exactly 2 players
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo kill efficiency requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get the most common game mode for filtering
                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var mostCommonGameMode = duoStats?.MostCommonQueueType;

                    var playerEfficiencies = new List<PlayerEfficiency>();

                    // Get kill efficiency for each player
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var serverName = gamer.Tagline.ToUpperInvariant();
                        var playerName = $"{gamer.GamerName}#{serverName}";

                        // Get kill efficiency stats
                        var efficiencyRecord = await matchParticipantRepo.GetDuoKillEfficiencyByPuuIdsAsync(puuId1, puuId2, puuId, mostCommonGameMode);

                        if (efficiencyRecord == null)
                        {
                            playerEfficiencies.Add(new PlayerEfficiency(
                                PlayerName: playerName,
                                KillParticipation: 0.0,
                                DeathShareInLosses: 0.0
                            ));
                            continue;
                        }

                        // Calculate kill participation: (kills + assists) / team kills
                        var killParticipation = efficiencyRecord.TeamKills > 0
                            ? Math.Round((double)(efficiencyRecord.TotalKills + efficiencyRecord.TotalAssists) / efficiencyRecord.TeamKills * 100, 1)
                            : 0.0;

                        // Calculate death share in losses: deaths in losses / team deaths in losses
                        var deathShareInLosses = efficiencyRecord.TeamDeathsInLosses > 0
                            ? Math.Round((double)efficiencyRecord.DeathsInLosses / efficiencyRecord.TeamDeathsInLosses * 100, 1)
                            : 0.0;

                        playerEfficiencies.Add(new PlayerEfficiency(
                            PlayerName: playerName,
                            KillParticipation: killParticipation,
                            DeathShareInLosses: deathShareInLosses
                        ));
                    }

                    return Results.Ok(new DuoKillEfficiencyResponse(Players: playerEfficiencies));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting duo kill efficiency" 
                        : "Invalid operation when getting duo kill efficiency");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo kill efficiency");
                }
            });
        }
    }
}

