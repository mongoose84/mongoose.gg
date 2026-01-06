using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoKillParticipationEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoKillParticipationEndpoint(string basePath)
        {
            Route = basePath + "/duo-kill-participation/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] DuoStatsRepository duoStatsRepo
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();
                    
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo kill participation requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await duoStatsRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var participationRecords = await duoStatsRepo.GetDuoKillParticipationAsync(puuId1, puuId2, gameMode);

                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    var totalDuoKills = participationRecords.Sum(r => r.TotalKills);

                    var players = participationRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new DuoPlayerKillParticipation(
                            PlayerName: playerNames[r.PuuId],
                            Kills: r.TotalKills,
                            Assists: r.TotalAssists,
                            KillParticipation: totalDuoKills > 0
                                ? Math.Round((double)r.TotalKills / totalDuoKills * 100, 1)
                                : 0,
                            AvgKillsPerGame: r.GamesPlayed > 0
                                ? Math.Round((double)r.TotalKills / r.GamesPlayed, 1)
                                : 0
                        ))
                        .OrderByDescending(p => p.Kills)
                        .ToList();

                    return Results.Ok(new DuoKillParticipationResponse(
                        Players: players,
                        TotalDuoKills: totalDuoKills
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo kill participation");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo kill participation");
                }
            });
        }
    }
}

