using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoKillsTrendEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoKillsTrendEndpoint(string basePath)
        {
            Route = basePath + "/duo-kills-trend/{userId}";
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
                        return Results.BadRequest("Duo kills trend requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var trendRecords = await matchParticipantRepo.GetDuoKillsTrendAsync(puuId1, puuId2, gameMode, 50);

                    if (trendRecords.Count == 0)
                    {
                        return Results.Ok(new DuoKillsTrendResponse(
                            DataPoints: [],
                            OverallAvgKills: 0,
                            RecentAvgKills: 0,
                            TrendDirection: "neutral"
                        ));
                    }

                    const int rollingWindow = 5;
                    var dataPoints = new List<DuoKillTrendDataPoint>();
                    var killsHistory = new List<int>();

                    for (int i = 0; i < trendRecords.Count; i++)
                    {
                        var record = trendRecords[i];
                        killsHistory.Add(record.TeamKills);
                        
                        var windowStart = Math.Max(0, killsHistory.Count - rollingWindow);
                        var windowKills = killsHistory.Skip(windowStart).Take(rollingWindow);
                        var rollingAvg = windowKills.Average();

                        dataPoints.Add(new DuoKillTrendDataPoint(
                            GameNumber: i + 1,
                            DuoKills: record.TeamKills,
                            RollingAvgKills: Math.Round(rollingAvg, 1),
                            Win: record.Win,
                            GameDate: record.GameDate
                        ));
                    }

                    var overallAvg = trendRecords.Average(r => r.TeamKills);
                    var recentCount = Math.Min(10, trendRecords.Count);
                    var recentAvg = trendRecords.TakeLast(recentCount).Average(r => r.TeamKills);

                    var trendDirection = recentAvg > overallAvg * 1.1 ? "up" 
                        : recentAvg < overallAvg * 0.9 ? "down" 
                        : "neutral";

                    return Results.Ok(new DuoKillsTrendResponse(
                        DataPoints: dataPoints,
                        OverallAvgKills: Math.Round(overallAvg, 1),
                        RecentAvgKills: Math.Round(recentAvg, 1),
                        TrendDirection: trendDirection
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo kills trend");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo kills trend");
                }
            });
        }
    }
}

