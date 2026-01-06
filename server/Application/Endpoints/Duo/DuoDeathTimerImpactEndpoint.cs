using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoDeathTimerImpactEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoDeathTimerImpactEndpoint(string basePath)
        {
            Route = basePath + "/duo-death-timer-impact/{userId}";
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
                        return Results.BadRequest("Duo death timer impact requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await duoStatsRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var deathTimerRecords = await duoStatsRepo.GetDuoDeathTimerStatsAsync(puuId1, puuId2, gameMode);

                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    var players = deathTimerRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new DuoPlayerDeathTimer(
                            PlayerName: playerNames[r.PuuId],
                            AvgTimeDeadWins: Math.Round(r.AvgTimeDeadWins, 0),
                            AvgTimeDeadLosses: Math.Round(r.AvgTimeDeadLosses, 0),
                            DeathTimeDifference: Math.Round(r.AvgTimeDeadLosses - r.AvgTimeDeadWins, 0)
                        ))
                        .ToList();

                    var avgDifference = players.Count > 0 
                        ? players.Average(p => p.DeathTimeDifference) 
                        : 0;

                    var insight = avgDifference > 30 
                        ? "You spend significantly more time dead in losses. Try to play safer to minimize death time."
                        : avgDifference > 10 
                            ? "Death time is somewhat higher in losses. Consider trading more efficiently."
                            : "Your death timers are relatively consistent between wins and losses.";

                    return Results.Ok(new DuoDeathTimerResponse(
                        Players: players,
                        Insight: insight
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo death timer impact");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo death timer impact");
                }
            });
        }
    }
}

