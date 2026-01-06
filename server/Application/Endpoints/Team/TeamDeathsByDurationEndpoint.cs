using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamDeathsByDurationEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamDeathsByDurationEndpoint(string basePath)
        {
            Route = basePath + "/team-deaths-by-duration/{userId}";
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

                    // Get deaths by duration
                    var durationRecords = await matchParticipantRepo.GetTeamDeathsByDurationAsync(distinctPuuIds, gameMode);

                    // Define bucket metadata (5-minute intervals)
                    var bucketMeta = new Dictionary<string, (string Label, int Min, int Max)>
                    {
                        ["under20"] = ("< 20 min", 0, 20),
                        ["20-25"] = ("20-25 min", 20, 25),
                        ["25-30"] = ("25-30 min", 25, 30),
                        ["30-35"] = ("30-35 min", 30, 35),
                        ["35-40"] = ("35-40 min", 35, 40),
                        ["40+"] = ("40+ min", 40, 999)
                    };

                    // Convert to response format with all buckets (even empty ones)
                    var buckets = bucketMeta.Select(meta =>
                    {
                        var record = durationRecords.FirstOrDefault(r => r.DurationBucket == meta.Key);
                        var gamesPlayed = record?.GamesPlayed ?? 0;
                        var totalDeaths = record?.TotalTeamDeaths ?? 0;
                        var wins = record?.Wins ?? 0;

                        return new DeathDurationBucket(
                            Label: meta.Value.Label,
                            MinMinutes: meta.Value.Min,
                            MaxMinutes: meta.Value.Max,
                            GamesPlayed: gamesPlayed,
                            AvgDeaths: gamesPlayed > 0 ? Math.Round((double)totalDeaths / gamesPlayed, 1) : 0,
                            WinRate: gamesPlayed > 0 ? Math.Round((double)wins / gamesPlayed * 100, 1) : 0
                        );
                    }).ToList();

                    return Results.Ok(new DeathsByDurationResponse(Buckets: buckets));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team deaths by duration");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team deaths by duration");
                }
            });
        }
    }
}

