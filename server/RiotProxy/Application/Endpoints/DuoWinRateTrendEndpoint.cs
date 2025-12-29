using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoTrendAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoWinRateTrendEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoWinRateTrendEndpoint(string basePath)
        {
            Route = basePath + "/duo-win-rate-trend/{userId}";
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
                        return Results.BadRequest("Duo win rate trend requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var trendRecords = await matchParticipantRepo.GetDuoWinRateTrendAsync(puuId1, puuId2, gameMode, 50);

                    if (trendRecords.Count == 0)
                    {
                        return Results.Ok(new DuoWinRateTrendResponse(
                            DataPoints: [],
                            OverallWinRate: 0,
                            RecentWinRate: 0,
                            TrendDirection: "neutral",
                            TotalGames: 0,
                            TotalWins: 0
                        ));
                    }

                    const int rollingWindow = 10;
                    var dataPoints = new List<DuoWinRateTrendDataPoint>();
                    var winsHistory = new List<bool>();

                    for (int i = 0; i < trendRecords.Count; i++)
                    {
                        var record = trendRecords[i];
                        winsHistory.Add(record.Win);
                        
                        var windowStart = Math.Max(0, winsHistory.Count - rollingWindow);
                        var windowWins = winsHistory.Skip(windowStart).Take(rollingWindow);
                        var rollingWinRate = windowWins.Count(w => w) / (double)windowWins.Count() * 100;

                        dataPoints.Add(new DuoWinRateTrendDataPoint(
                            GameNumber: i + 1,
                            Win: record.Win,
                            RollingWinRate: Math.Round(rollingWinRate, 1),
                            GameDate: record.GameDate
                        ));
                    }

                    var totalWins = trendRecords.Count(r => r.Win);
                    var totalGames = trendRecords.Count;
                    var overallWinRate = (double)totalWins / totalGames * 100;

                    var recentCount = Math.Min(10, trendRecords.Count);
                    var recentWins = trendRecords.TakeLast(recentCount).Count(r => r.Win);
                    var recentWinRate = (double)recentWins / recentCount * 100;

                    var trendDirection = recentWinRate > overallWinRate + 5 ? "up" 
                        : recentWinRate < overallWinRate - 5 ? "down" 
                        : "neutral";

                    return Results.Ok(new DuoWinRateTrendResponse(
                        DataPoints: dataPoints,
                        OverallWinRate: Math.Round(overallWinRate, 1),
                        RecentWinRate: Math.Round(recentWinRate, 1),
                        TrendDirection: trendDirection,
                        TotalGames: totalGames,
                        TotalWins: totalWins
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo win rate trend");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo win rate trend");
                }
            });
        }
    }
}

