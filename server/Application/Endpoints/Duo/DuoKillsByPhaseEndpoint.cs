using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoKillsByPhaseEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoKillsByPhaseEndpoint(string basePath)
        {
            Route = basePath + "/duo-kills-by-phase/{userId}";
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
                    
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo kills by phase require exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var killsByDuration = await matchParticipantRepo.GetDuoKillsByDurationAsync(puuId1, puuId2, gameMode);

                    var bucketOrder = new[] { "under20", "20-25", "25-30", "30-35", "35-40", "40+" };
                    var bucketLabels = new Dictionary<string, string>
                    {
                        { "under20", "< 20 min" },
                        { "20-25", "20-25 min" },
                        { "25-30", "25-30 min" },
                        { "30-35", "30-35 min" },
                        { "35-40", "35-40 min" },
                        { "40+", "40+ min" }
                    };

                    var buckets = killsByDuration
                        .OrderBy(b => Array.IndexOf(bucketOrder, b.DurationBucket))
                        .Select(b => new DuoKillDurationBucket(
                            Label: bucketLabels.GetValueOrDefault(b.DurationBucket, b.DurationBucket),
                            GamesPlayed: b.GamesPlayed,
                            AvgKills: b.GamesPlayed > 0 ? Math.Round((double)b.TotalTeamKills / b.GamesPlayed, 1) : 0,
                            WinRate: b.GamesPlayed > 0 ? Math.Round((double)b.Wins / b.GamesPlayed * 100, 1) : 0
                        ))
                        .ToList();

                    var bestBucket = buckets.OrderByDescending(b => b.AvgKills).FirstOrDefault();

                    return Results.Ok(new DuoKillsByDurationResponse(
                        Buckets: buckets,
                        BestDuration: bestBucket?.Label ?? "N/A",
                        BestAvgKills: bestBucket?.AvgKills ?? 0
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo kills by phase");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo kills by phase");
                }
            });
        }
    }
}

