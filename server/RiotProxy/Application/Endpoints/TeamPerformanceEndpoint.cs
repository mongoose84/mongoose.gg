using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamPerformanceDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamPerformanceEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamPerformanceEndpoint(string basePath)
        {
            Route = basePath + "/team-performance/{userId}";
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
                        return Results.BadRequest("Team performance requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get performance data for each player
                    var performanceRecords = await matchParticipantRepo.GetTeamPlayerPerformanceByPuuIdsAsync(distinctPuuIds, gameMode);

                    // Get team totals for kill participation calculation
                    var teamKillsDeaths = await matchParticipantRepo.GetTeamKillsDeathsByPuuIdsAsync(distinctPuuIds, gameMode);

                    // Build player name lookup
                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = $"{gamer.GamerName}#{gamer.Tagline.ToUpperInvariant()}";
                        }
                    }

                    // Calculate team totals
                    var totalTeamKills = performanceRecords.Sum(r => r.TotalKills);
                    var totalTeamDeaths = performanceRecords.Sum(r => r.TotalDeaths);
                    var totalTeamAssists = performanceRecords.Sum(r => r.TotalAssists);
                    var totalTeamGold = performanceRecords.Sum(r => r.TotalGoldEarned);
                    var totalTeamDuration = performanceRecords.Sum(r => r.TotalDurationSeconds);

                    // Convert to response format
                    var players = performanceRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => {
                            var durationMinutes = r.TotalDurationSeconds / 60.0;
                            var killParticipation = totalTeamKills > 0 
                                ? Math.Round((double)(r.TotalKills + r.TotalAssists) / totalTeamKills * 100, 1) 
                                : 0.0;
                            var deathShare = totalTeamDeaths > 0 
                                ? Math.Round((double)r.TotalDeaths / totalTeamDeaths * 100, 1) 
                                : 0.0;

                            return new PlayerPerformance(
                                PlayerName: playerNames[r.PuuId],
                                GamesPlayed: r.GamesPlayed,
                                Wins: r.Wins,
                                WinRate: r.GamesPlayed > 0 ? Math.Round((double)r.Wins / r.GamesPlayed * 100, 1) : 0.0,
                                AvgKills: r.GamesPlayed > 0 ? Math.Round((double)r.TotalKills / r.GamesPlayed, 1) : 0.0,
                                AvgDeaths: r.GamesPlayed > 0 ? Math.Round((double)r.TotalDeaths / r.GamesPlayed, 1) : 0.0,
                                AvgAssists: r.GamesPlayed > 0 ? Math.Round((double)r.TotalAssists / r.GamesPlayed, 1) : 0.0,
                                Kda: r.TotalDeaths > 0 ? Math.Round((double)(r.TotalKills + r.TotalAssists) / r.TotalDeaths, 2) : r.TotalKills + r.TotalAssists,
                                GoldPerMin: durationMinutes > 0 ? Math.Round(r.TotalGoldEarned / durationMinutes, 0) : 0.0,
                                CsPerMin: durationMinutes > 0 ? Math.Round(r.TotalCreepScore / durationMinutes, 1) : 0.0,
                                KillParticipation: killParticipation,
                                DeathShare: deathShare
                            );
                        })
                        .OrderByDescending(p => p.Kda)
                        .ToList();

                    var teamTotals = new TeamTotals(
                        TotalKills: totalTeamKills,
                        TotalDeaths: totalTeamDeaths,
                        TotalAssists: totalTeamAssists,
                        AvgTeamKda: totalTeamDeaths > 0 ? Math.Round((double)(totalTeamKills + totalTeamAssists) / totalTeamDeaths, 2) : totalTeamKills + totalTeamAssists,
                        AvgTeamGoldPerMin: totalTeamDuration > 0 ? Math.Round(totalTeamGold / (totalTeamDuration / 60.0), 0) : 0.0
                    );

                    return Results.Ok(new TeamPerformanceResponse(
                        Players: players,
                        TeamTotals: teamTotals
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting team performance"
                        : "Invalid operation when getting team performance");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team performance");
                }
            });
        }
    }
}

