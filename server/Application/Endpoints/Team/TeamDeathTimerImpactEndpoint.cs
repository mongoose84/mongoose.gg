using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamDeathAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamDeathTimerImpactEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamDeathTimerImpactEndpoint(string basePath)
        {
            Route = basePath + "/team-death-timer-impact/{userId}";
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

                    // Get death timer stats per player
                    var deathTimerRecords = await matchParticipantRepo.GetTeamDeathTimerStatsByPuuIdsAsync(distinctPuuIds, gameMode);

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

                    // Convert to response format
                    var players = deathTimerRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new PlayerDeathTimerStats(
                            PlayerName: playerNames[r.PuuId],
                            AvgTimeDeadWins: r.GamesWon > 0 ? Math.Round((double)r.TotalTimeDeadInWins / r.GamesWon, 0) : 0,
                            AvgTimeDeadLosses: r.GamesLost > 0 ? Math.Round((double)r.TotalTimeDeadInLosses / r.GamesLost, 0) : 0,
                            AvgDeathsWins: r.GamesWon > 0 ? Math.Round((double)r.TotalDeathsInWins / r.GamesWon, 1) : 0,
                            AvgDeathsLosses: r.GamesLost > 0 ? Math.Round((double)r.TotalDeathsInLosses / r.GamesLost, 1) : 0
                        ))
                        .OrderByDescending(p => p.AvgTimeDeadLosses - p.AvgTimeDeadWins)
                        .ToList();

                    // Calculate team averages
                    var totalTimeDeadWins = deathTimerRecords.Sum(r => r.TotalTimeDeadInWins);
                    var totalTimeDeadLosses = deathTimerRecords.Sum(r => r.TotalTimeDeadInLosses);
                    var totalGamesWon = deathTimerRecords.Sum(r => r.GamesWon) / distinctPuuIds.Length; // Avoid counting each player
                    var totalGamesLost = deathTimerRecords.Sum(r => r.GamesLost) / distinctPuuIds.Length;

                    return Results.Ok(new DeathTimerImpactResponse(
                        Players: players,
                        TeamAvgTimeDeadWins: totalGamesWon > 0 ? Math.Round((double)totalTimeDeadWins / totalGamesWon, 0) : 0,
                        TeamAvgTimeDeadLosses: totalGamesLost > 0 ? Math.Round((double)totalTimeDeadLosses / totalGamesLost, 0) : 0
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team death timer impact");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team death timer impact");
                }
            });
        }
    }
}

