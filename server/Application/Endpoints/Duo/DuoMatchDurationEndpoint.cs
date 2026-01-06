using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoMatchDurationDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoMatchDurationEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoMatchDurationEndpoint(string basePath)
        {
            Route = basePath + "/duo-match-duration/{userId}";
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
                        return Results.BadRequest("Duo match duration requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get the most common game mode for filtering
                    var duoStats = await duoStatsRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var mostCommonGameMode = duoStats?.MostCommonQueueType;

                    // Get duration statistics for duo games
                    var durationRecords = await duoStatsRepo.GetDuoDurationStatsByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);

                    // Convert to response format
                    var buckets = durationRecords.Select(record =>
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

                    return Results.Ok(new DuoMatchDurationResponse(Buckets: buckets));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting duo match duration" 
                        : "Invalid operation when getting duo match duration");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo match duration");
                }
            });
        }
    }
}

