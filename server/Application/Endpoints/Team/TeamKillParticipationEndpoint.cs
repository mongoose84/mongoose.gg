using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamKillParticipationEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamKillParticipationEndpoint(string basePath)
        {
            Route = basePath + "/team-kill-participation/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] GamerRepository gamerRepo,
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

                    // Get performance data (which includes kills and assists)
                    var performanceRecords = await matchParticipantRepo.GetTeamPlayerPerformanceByPuuIdsAsync(distinctPuuIds, gameMode);

                    // Build player name lookup
                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    // Calculate total team kills (sum of all player kills)
                    var totalTeamKills = performanceRecords.Sum(r => r.TotalKills);

                    // Convert to response format
                    var players = performanceRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => {
                            // Kill participation = (kills + assists) / total team kills
                            var killParticipation = totalTeamKills > 0 
                                ? Math.Round((double)(r.TotalKills + r.TotalAssists) / totalTeamKills * 100, 1) 
                                : 0;
                            return new PlayerKillParticipation(
                                PlayerName: playerNames[r.PuuId],
                                Kills: r.TotalKills,
                                Assists: r.TotalAssists,
                                KillParticipation: killParticipation,
                                AvgKillsPerGame: r.GamesPlayed > 0 ? Math.Round((double)r.TotalKills / r.GamesPlayed, 1) : 0
                            );
                        })
                        .OrderByDescending(p => p.KillParticipation)
                        .ToList();

                    return Results.Ok(new KillParticipationResponse(
                        Players: players,
                        TotalTeamKills: totalTeamKills
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team kill participation");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team kill participation");
                }
            });
        }
    }
}

