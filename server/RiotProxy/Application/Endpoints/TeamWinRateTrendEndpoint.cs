using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Endpoint for fetching team win rate trend data.
/// </summary>
public class TeamWinRateTrendEndpoint : IEndpoint
{
    public string Route { get; }

    public TeamWinRateTrendEndpoint(string basePath)
    {
        Route = basePath + "/team-win-rate-trend/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            [FromRoute] string userId,
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] LolMatchParticipantRepository participantRepo) =>
        {
            try
            {
                var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                Console.WriteLine($"[TeamWinRateTrend] userId={userIdInt}, puuIds count={distinctPuuIds.Length}");

                if (distinctPuuIds.Length < 3)
                {
                    return Results.BadRequest("Team analytics requires at least 3 players.");
                }
                var matchResults = await participantRepo.GetTeamMatchResultsByPuuIdsAsync(distinctPuuIds, 50);
                Console.WriteLine($"[TeamWinRateTrend] matchResults count={matchResults.Count}");

                if (matchResults.Count == 0)
                {
                    return Results.Ok(new TeamWinRateTrendDto.TeamWinRateTrendResponse(
                        DataPoints: new List<TeamWinRateTrendDto.TrendDataPoint>(),
                        OverallWinRate: 0,
                        RecentWinRate: 0,
                        TrendDirection: "neutral"
                    ));
                }

                // Reverse to get chronological order (oldest first)
                var chronological = matchResults.Reverse().ToList();

                // Calculate rolling win rate (5-game window)
                var dataPoints = new List<TeamWinRateTrendDto.TrendDataPoint>();
                int wins = 0;
                for (int i = 0; i < chronological.Count; i++)
                {
                    if (chronological[i].Win) wins++;

                    // Calculate rolling win rate for the last 5 games
                    int windowStart = Math.Max(0, i - 4);
                    int windowWins = 0;
                    for (int j = windowStart; j <= i; j++)
                    {
                        if (chronological[j].Win) windowWins++;
                    }
                    double rollingWinRate = Math.Round((double)windowWins / (i - windowStart + 1) * 100, 1);

                    dataPoints.Add(new TeamWinRateTrendDto.TrendDataPoint(
                        GameNumber: i + 1,
                        Win: chronological[i].Win,
                        RollingWinRate: rollingWinRate,
                        GameDate: chronological[i].GameEndTimestamp
                    ));
                }

                double overallWinRate = Math.Round((double)wins / chronological.Count * 100, 1);

                // Calculate recent win rate (last 10 games)
                var recentGames = chronological.TakeLast(10).ToList();
                int recentWins = recentGames.Count(g => g.Win);
                double recentWinRate = Math.Round((double)recentWins / recentGames.Count * 100, 1);

                // Determine trend direction
                string trendDirection = "neutral";
                if (chronological.Count >= 10)
                {
                    var firstHalf = chronological.Take(chronological.Count / 2).ToList();
                    var secondHalf = chronological.Skip(chronological.Count / 2).ToList();
                    double firstHalfWr = (double)firstHalf.Count(g => g.Win) / firstHalf.Count;
                    double secondHalfWr = (double)secondHalf.Count(g => g.Win) / secondHalf.Count;

                    if (secondHalfWr > firstHalfWr + 0.05) trendDirection = "improving";
                    else if (secondHalfWr < firstHalfWr - 0.05) trendDirection = "declining";
                }

                return Results.Ok(new TeamWinRateTrendDto.TeamWinRateTrendResponse(
                    DataPoints: dataPoints,
                    OverallWinRate: overallWinRate,
                    RecentWinRate: recentWinRate,
                    TrendDirection: trendDirection
                ));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Results.BadRequest(ex.Message);
            }
        });
    }
}

