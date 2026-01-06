using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.MatchDurationDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class MatchDurationEndpoint : IEndpoint
    {
        public string Route { get; }

        public MatchDurationEndpoint(string basePath)
        {
            Route = basePath + "/match-duration/{userId}";
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

                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(new MatchDurationResponse(Gamers: []));
                    }

                    var gamerDurationStats = new List<GamerDurationStats>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        // Extract server name from tagline (e.g., "EUW", "EUNE")
                        var serverName = gamer.Tagline.ToUpperInvariant();
                        var gamerName = $"{gamer.GamerName}#{serverName}";

                        // Get duration bucket statistics
                        var bucketRecords = await matchParticipantRepo.GetDurationStatsByPuuIdAsync(puuId);

                        var buckets = bucketRecords.Select(record =>
                        {
                            var winrate = record.GamesPlayed > 0
                                ? Math.Round((double)record.Wins / record.GamesPlayed * 100, 1)
                                : 0.0;

                            var durationRange = $"{record.MinMinutes}–{record.MaxMinutes}";

                            return new DurationBucket(
                                DurationRange: durationRange,
                                MinMinutes: record.MinMinutes,
                                MaxMinutes: record.MaxMinutes,
                                Winrate: winrate,
                                GamesPlayed: record.GamesPlayed
                            );
                        }).ToList();

                        gamerDurationStats.Add(new GamerDurationStats(
                            GamerName: gamerName,
                            ServerName: serverName,
                            Buckets: buckets
                        ));
                    }

                    return Results.Ok(new MatchDurationResponse(Gamers: gamerDurationStats));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting match duration stats"
                        : "Invalid operation when getting match duration stats");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting match duration stats");
                }
            });
        }
    }
}

