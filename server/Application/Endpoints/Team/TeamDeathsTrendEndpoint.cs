using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamDeathsTrendEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamDeathsTrendEndpoint(string basePath)
        {
            Route = basePath + "/team-deaths-trend/{userId}";
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
                        return Results.BadRequest("Team death analysis requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get match death records
                    var matchRecords = await matchParticipantRepo.GetTeamMatchDeathsAsync(distinctPuuIds, 50, gameMode);

                    if (matchRecords.Count == 0)
                    {
                        return Results.Ok(new DeathsTrendResponse(
                            DataPoints: new List<DeathTrendDataPoint>(),
                            OverallAvgDeaths: 0,
                            RecentAvgDeaths: 0,
                            TrendDirection: "neutral"
                        ));
                    }

                    // Calculate rolling average (5-game window)
                    const int rollingWindow = 5;
                    var dataPoints = new List<DeathTrendDataPoint>();
                    var runningSum = 0;

                    for (int i = 0; i < matchRecords.Count; i++)
                    {
                        var match = matchRecords[i];
                        runningSum += match.TeamDeaths;

                        // Calculate rolling average
                        var windowStart = Math.Max(0, i - rollingWindow + 1);
                        var windowDeaths = matchRecords.Skip(windowStart).Take(i - windowStart + 1).Sum(m => m.TeamDeaths);
                        var windowSize = i - windowStart + 1;
                        var rollingAvg = Math.Round((double)windowDeaths / windowSize, 1);

                        dataPoints.Add(new DeathTrendDataPoint(
                            GameNumber: i + 1,
                            TeamDeaths: match.TeamDeaths,
                            RollingAvgDeaths: rollingAvg,
                            Win: match.Win,
                            GameDate: match.GameEndTimestamp
                        ));
                    }

                    // Calculate overall and recent averages
                    var overallAvg = Math.Round((double)matchRecords.Sum(m => m.TeamDeaths) / matchRecords.Count, 1);
                    var recentGames = matchRecords.TakeLast(10).ToList();
                    var recentAvg = recentGames.Count > 0 
                        ? Math.Round((double)recentGames.Sum(m => m.TeamDeaths) / recentGames.Count, 1) 
                        : 0;

                    // Determine trend direction
                    string trendDirection;
                    var diff = recentAvg - overallAvg;
                    if (diff <= -1.5)
                        trendDirection = "improving"; // Dying less = improving
                    else if (diff >= 1.5)
                        trendDirection = "declining"; // Dying more = declining
                    else
                        trendDirection = "neutral";

                    return Results.Ok(new DeathsTrendResponse(
                        DataPoints: dataPoints,
                        OverallAvgDeaths: overallAvg,
                        RecentAvgDeaths: recentAvg,
                        TrendDirection: trendDirection
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team deaths trend");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team deaths trend");
                }
            });
        }
    }
}

