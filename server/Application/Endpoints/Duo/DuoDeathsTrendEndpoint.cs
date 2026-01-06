using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoDeathsTrendEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoDeathsTrendEndpoint(string basePath)
        {
            Route = basePath + "/duo-deaths-trend/{userId}";
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
                        return Results.BadRequest("Duo deaths trend requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var trendRecords = await matchParticipantRepo.GetDuoDeathsTrendAsync(puuId1, puuId2, gameMode, 50);

                    if (trendRecords.Count == 0)
                    {
                        return Results.Ok(new DuoDeathsTrendResponse(
                            DataPoints: [],
                            OverallAvgDeaths: 0,
                            RecentAvgDeaths: 0,
                            TrendDirection: "neutral"
                        ));
                    }

                    const int rollingWindow = 5;
                    var dataPoints = new List<DuoDeathTrendDataPoint>();
                    var deathsHistory = new List<int>();

                    for (int i = 0; i < trendRecords.Count; i++)
                    {
                        var record = trendRecords[i];
                        deathsHistory.Add(record.TeamDeaths);
                        
                        var windowStart = Math.Max(0, deathsHistory.Count - rollingWindow);
                        var windowDeaths = deathsHistory.Skip(windowStart).Take(rollingWindow);
                        var rollingAvg = windowDeaths.Average();

                        dataPoints.Add(new DuoDeathTrendDataPoint(
                            GameNumber: i + 1,
                            DuoDeaths: record.TeamDeaths,
                            RollingAvgDeaths: Math.Round(rollingAvg, 1),
                            Win: record.Win,
                            GameDate: record.GameDate
                        ));
                    }

                    var overallAvg = trendRecords.Average(r => r.TeamDeaths);
                    var recentCount = Math.Min(10, trendRecords.Count);
                    var recentAvg = trendRecords.TakeLast(recentCount).Average(r => r.TeamDeaths);

                    // For deaths, lower is better, so trend is reversed
                    var trendDirection = recentAvg < overallAvg * 0.9 ? "up" 
                        : recentAvg > overallAvg * 1.1 ? "down" 
                        : "neutral";

                    return Results.Ok(new DuoDeathsTrendResponse(
                        DataPoints: dataPoints,
                        OverallAvgDeaths: Math.Round(overallAvg, 1),
                        RecentAvgDeaths: Math.Round(recentAvg, 1),
                        TrendDirection: trendDirection
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo deaths trend");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo deaths trend");
                }
            });
        }
    }
}

