using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoTrendAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoPerformanceRadarEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoPerformanceRadarEndpoint(string basePath)
        {
            Route = basePath + "/duo-performance-radar/{userId}";
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
                        return Results.BadRequest("Duo performance radar requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var radarData = await matchParticipantRepo.GetDuoPerformanceRadarAsync(puuId1, puuId2, gameMode);

                    if (radarData == null)
                    {
                        return Results.Ok(new DuoPerformanceRadarResponse(
                            Metrics: new DuoRadarMetrics(0, 0, 0, 0, 0, 0),
                            Normalized: new DuoRadarNormalized(0, 0, 0, 0, 0, 0),
                            GamesPlayed: 0
                        ));
                    }

                    var winRate = radarData.GamesPlayed > 0
                        ? (double)radarData.Wins / radarData.GamesPlayed * 100
                        : 0;

                    var metrics = new DuoRadarMetrics(
                        AvgKills: Math.Round(radarData.AvgKills, 1),
                        AvgDeaths: Math.Round(radarData.AvgDeaths, 1),
                        AvgAssists: Math.Round(radarData.AvgAssists, 1),
                        AvgCs: Math.Round(radarData.AvgCs, 0),
                        AvgGoldEarned: Math.Round(radarData.AvgGoldEarned, 0),
                        WinRate: Math.Round(winRate, 1)
                    );

                    // Normalize metrics to 0-100 scale based on typical values
                    var normalized = new DuoRadarNormalized(
                        Kills: Math.Min(100, radarData.AvgKills / 12.0 * 100),
                        Survival: Math.Min(100, Math.Max(0, (1 - radarData.AvgDeaths / 10.0) * 100)),
                        Assists: Math.Min(100, radarData.AvgAssists / 15.0 * 100),
                        Farming: Math.Min(100, radarData.AvgCs / 250.0 * 100),
                        Gold: Math.Min(100, radarData.AvgGoldEarned / 15000.0 * 100),
                        WinRate: winRate
                    );

                    return Results.Ok(new DuoPerformanceRadarResponse(
                        Metrics: metrics,
                        Normalized: normalized,
                        GamesPlayed: radarData.GamesPlayed
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo performance radar");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo performance radar");
                }
            });
        }
    }
}

