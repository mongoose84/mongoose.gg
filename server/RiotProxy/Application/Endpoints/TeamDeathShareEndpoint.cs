using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamDeathShareEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamDeathShareEndpoint(string basePath)
        {
            Route = basePath + "/team-death-share/{userId}";
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
                        return Results.BadRequest("Team death analysis requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;
                    var totalGames = teamStats?.GamesPlayed ?? 0;

                    // Get performance data (which includes deaths)
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

                    // Calculate total team deaths
                    var totalTeamDeaths = performanceRecords.Sum(r => r.TotalDeaths);

                    // Convert to response format
                    var players = performanceRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new PlayerDeathShare(
                            PlayerName: playerNames[r.PuuId],
                            TotalDeaths: r.TotalDeaths,
                            DeathSharePercent: totalTeamDeaths > 0 ? Math.Round((double)r.TotalDeaths / totalTeamDeaths * 100, 1) : 0,
                            AvgDeathsPerGame: r.GamesPlayed > 0 ? Math.Round((double)r.TotalDeaths / r.GamesPlayed, 1) : 0
                        ))
                        .OrderByDescending(p => p.DeathSharePercent)
                        .ToList();

                    return Results.Ok(new DeathShareResponse(
                        Players: players,
                        TotalTeamDeaths: totalTeamDeaths
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team death share");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team death share");
                }
            });
        }
    }
}

