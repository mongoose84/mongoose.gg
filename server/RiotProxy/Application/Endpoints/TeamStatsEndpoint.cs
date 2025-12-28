using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamStatsDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamStatsEndpoint(string basePath)
        {
            Route = basePath + "/team-stats/{userId}";
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
                    
                    // Team dashboard requires 3+ players
                    if (distinctPuuIds.Length < 3)
                    {
                        return Results.BadRequest("Team stats requires at least 3 players");
                    }

                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);

                    if (teamStats == null || teamStats.GamesPlayed == 0)
                    {
                        return Results.Ok(new TeamStatsResponse(
                            GamesPlayed: 0,
                            Wins: 0,
                            WinRate: 0.0,
                            QueueType: "No team games found",
                            AvgKda: 0.0,
                            AvgGameDurationMinutes: 0.0,
                            PlayerCount: distinctPuuIds.Length
                        ));
                    }

                    var winRate = Math.Round((double)teamStats.Wins / teamStats.GamesPlayed * 100, 1);
                    var avgKda = teamStats.TotalDeaths > 0
                        ? Math.Round((double)(teamStats.TotalKills + teamStats.TotalAssists) / teamStats.TotalDeaths, 2)
                        : teamStats.TotalKills + teamStats.TotalAssists;
                    var avgDurationMinutes = Math.Round(teamStats.AvgDurationSeconds / 60.0, 1);
                    var queueType = MapGameModeToQueueType(teamStats.MostCommonGameMode);

                    return Results.Ok(new TeamStatsResponse(
                        GamesPlayed: teamStats.GamesPlayed,
                        Wins: teamStats.Wins,
                        WinRate: winRate,
                        QueueType: queueType,
                        AvgKda: avgKda,
                        AvgGameDurationMinutes: avgDurationMinutes,
                        PlayerCount: distinctPuuIds.Length
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting team stats"
                        : "Invalid operation when getting team stats");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team stats");
                }
            });
        }

        private static string MapGameModeToQueueType(string gameMode)
        {
            return gameMode switch
            {
                "CLASSIC" => "Normal/Ranked",
                "ARAM" => "ARAM",
                "URF" => "URF",
                "ONEFORALL" => "One for All",
                "NEXUSBLITZ" => "Nexus Blitz",
                "TUTORIAL" => "Tutorial",
                "PRACTICETOOL" => "Practice Tool",
                _ => gameMode ?? "Unknown"
            };
        }
    }
}

