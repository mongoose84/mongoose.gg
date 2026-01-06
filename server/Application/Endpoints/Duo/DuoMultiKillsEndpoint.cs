using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoMultiKillsEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoMultiKillsEndpoint(string basePath)
        {
            Route = basePath + "/duo-multi-kills/{userId}";
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
                        return Results.BadRequest("Duo multi-kills require exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var multiKillRecords = await matchParticipantRepo.GetDuoMultiKillsAsync(puuId1, puuId2, gameMode);

                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    var players = multiKillRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new DuoPlayerMultiKills(
                            PlayerName: playerNames[r.PuuId],
                            DoubleKills: r.DoubleKills,
                            TripleKills: r.TripleKills,
                            QuadraKills: r.QuadraKills,
                            PentaKills: r.PentaKills,
                            TotalMultiKills: r.DoubleKills + r.TripleKills + r.QuadraKills + r.PentaKills
                        ))
                        .OrderByDescending(p => p.PentaKills)
                        .ThenByDescending(p => p.QuadraKills)
                        .ThenByDescending(p => p.TotalMultiKills)
                        .ToList();

                    var duoTotals = new DuoMultiKillTotals(
                        DoubleKills: multiKillRecords.Sum(r => r.DoubleKills),
                        TripleKills: multiKillRecords.Sum(r => r.TripleKills),
                        QuadraKills: multiKillRecords.Sum(r => r.QuadraKills),
                        PentaKills: multiKillRecords.Sum(r => r.PentaKills)
                    );

                    return Results.Ok(new DuoMultiKillsResponse(
                        Players: players,
                        DuoTotals: duoTotals
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo multi-kills");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo multi-kills");
                }
            });
        }
    }
}

