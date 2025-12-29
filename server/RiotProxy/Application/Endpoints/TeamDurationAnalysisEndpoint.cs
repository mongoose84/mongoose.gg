using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Endpoint for fetching team game duration analysis.
/// </summary>
public class TeamDurationAnalysisEndpoint : IEndpoint
{
    public string Route { get; }

    public TeamDurationAnalysisEndpoint(string basePath)
    {
        Route = basePath + "/team-duration-analysis/{userId}";
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

                if (distinctPuuIds.Length < 3)
                {
                    return Results.BadRequest("Team analytics requires at least 3 players.");
                }

                var durationStats = await participantRepo.GetTeamDurationStatsByPuuIdsAsync(distinctPuuIds);

                // Use 5-minute intervals matching the repository query
                var buckets = new List<TeamDurationAnalysisDto.DurationBucket>
                {
                    new("< 20 min", 0, 20, 0, 0, 0),
                    new("20-25 min", 20, 25, 0, 0, 0),
                    new("25-30 min", 25, 30, 0, 0, 0),
                    new("30-35 min", 30, 35, 0, 0, 0),
                    new("35-40 min", 35, 40, 0, 0, 0),
                    new("40+ min", 40, 999, 0, 0, 0)
                };

                foreach (var stat in durationStats)
                {
                    int idx = stat.DurationBucket switch
                    {
                        "under20" => 0,
                        "20-25" => 1,
                        "25-30" => 2,
                        "30-35" => 3,
                        "35-40" => 4,
                        "40+" => 5,
                        _ => -1
                    };

                    if (idx >= 0)
                    {
                        var bucket = buckets[idx];
                        double winRate = stat.GamesPlayed > 0 ? Math.Round((double)stat.Wins / stat.GamesPlayed * 100, 1) : 0;
                        buckets[idx] = bucket with
                        {
                            GamesPlayed = stat.GamesPlayed,
                            Wins = stat.Wins,
                            WinRate = winRate
                        };
                    }
                }

                // Find best duration (highest win rate with at least 2 games)
                var bestBucket = buckets
                    .Where(b => b.GamesPlayed >= 2)
                    .OrderByDescending(b => b.WinRate)
                    .ThenByDescending(b => b.GamesPlayed)
                    .FirstOrDefault();

                return Results.Ok(new TeamDurationAnalysisDto.TeamDurationAnalysisResponse(
                    Buckets: buckets,
                    BestDuration: bestBucket?.Label ?? "Not enough data",
                    BestWinRate: bestBucket?.WinRate ?? 0
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

