using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamKillsByPhaseEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamKillsByPhaseEndpoint(string basePath)
        {
            Route = basePath + "/team-kills-by-phase/{userId}";
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

                    // Get kills by duration
                    var killsByDuration = await matchParticipantRepo.GetTeamKillsByDurationAsync(distinctPuuIds, gameMode);

                    // Use 5-minute intervals matching the duration analysis
                    var buckets = new List<KillDurationBucket>
                    {
                        new("< 20 min", 0, 20, 0, 0, 0),
                        new("20-25 min", 20, 25, 0, 0, 0),
                        new("25-30 min", 25, 30, 0, 0, 0),
                        new("30-35 min", 30, 35, 0, 0, 0),
                        new("35-40 min", 35, 40, 0, 0, 0),
                        new("40+ min", 40, 999, 0, 0, 0)
                    };

                    foreach (var stat in killsByDuration)
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
                            double avgKills = stat.GamesPlayed > 0 ? Math.Round((double)stat.TotalTeamKills / stat.GamesPlayed, 1) : 0;
                            double winRate = stat.GamesPlayed > 0 ? Math.Round((double)stat.Wins / stat.GamesPlayed * 100, 1) : 0;
                            buckets[idx] = bucket with
                            {
                                GamesPlayed = stat.GamesPlayed,
                                AvgKills = avgKills,
                                WinRate = winRate
                            };
                        }
                    }

                    // Find best duration (highest avg kills with at least 2 games)
                    var bestBucket = buckets
                        .Where(b => b.GamesPlayed >= 2)
                        .OrderByDescending(b => b.AvgKills)
                        .ThenByDescending(b => b.GamesPlayed)
                        .FirstOrDefault();

                    return Results.Ok(new KillsByDurationResponse(
                        Buckets: buckets,
                        BestDuration: bestBucket?.Label ?? "Not enough data",
                        BestAvgKills: bestBucket?.AvgKills ?? 0
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team kills by duration");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team kills by duration");
                }
            });
        }
    }
}

