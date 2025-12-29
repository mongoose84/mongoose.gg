using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamKillsTrendEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamKillsTrendEndpoint(string basePath)
        {
            Route = basePath + "/team-kills-trend/{userId}";
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
                    
                    if (distinctPuuIds.Length < 3)
                    {
                        return Results.BadRequest("Team kill analysis requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get match kills for trend
                    var matchKills = await matchParticipantRepo.GetTeamMatchKillsAsync(distinctPuuIds, 50, gameMode);

                    if (matchKills.Count == 0)
                    {
                        return Results.Ok(new KillsTrendResponse(
                            DataPoints: new List<KillTrendDataPoint>(),
                            OverallAvgKills: 0,
                            RecentAvgKills: 0,
                            TrendDirection: "stable"
                        ));
                    }

                    // Calculate rolling average (5-game window)
                    const int windowSize = 5;
                    var dataPoints = new List<KillTrendDataPoint>();
                    
                    for (int i = 0; i < matchKills.Count; i++)
                    {
                        var windowStart = Math.Max(0, i - windowSize + 1);
                        var windowMatches = matchKills.Skip(windowStart).Take(i - windowStart + 1).ToList();
                        var rollingAvg = windowMatches.Average(m => m.TeamKills);

                        dataPoints.Add(new KillTrendDataPoint(
                            GameNumber: i + 1,
                            TeamKills: matchKills[i].TeamKills,
                            RollingAvgKills: Math.Round(rollingAvg, 1),
                            Win: matchKills[i].Win,
                            GameDate: matchKills[i].GameEndTimestamp
                        ));
                    }

                    // Calculate overall and recent averages
                    var overallAvg = matchKills.Average(m => m.TeamKills);
                    var recentCount = Math.Min(10, matchKills.Count);
                    var recentAvg = matchKills.TakeLast(recentCount).Average(m => m.TeamKills);

                    // Determine trend direction
                    var trendDirection = recentAvg > overallAvg * 1.1 ? "improving" 
                        : recentAvg < overallAvg * 0.9 ? "declining" 
                        : "stable";

                    return Results.Ok(new KillsTrendResponse(
                        DataPoints: dataPoints,
                        OverallAvgKills: Math.Round(overallAvg, 1),
                        RecentAvgKills: Math.Round(recentAvg, 1),
                        TrendDirection: trendDirection
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team kills trend");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team kills trend");
                }
            });
        }
    }
}

