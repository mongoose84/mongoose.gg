using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ChampionMatchupsDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class ChampionMatchupsEndpoint : IEndpoint
    {
        public string Route { get; }

        public ChampionMatchupsEndpoint(string basePath)
        {
            Route = basePath + "/champion-matchups/{userId}";
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
                    
                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(new ChampionMatchupsResponse(Matchups: []));
                    }

                    // Get all matchup data for the user's puuids
                    var matchupRecords = await matchParticipantRepo.GetChampionMatchupsByPuuIdsAsync(distinctPuuIds);

                    // Group by champion + role
                    var groupedMatchups = matchupRecords
                        .GroupBy(m => new { m.ChampionId, m.ChampionName, m.Role })
                        .Select(group =>
                        {
                            var totalGames = group.Sum(m => m.GamesPlayed);
                            var totalWins = group.Sum(m => m.Wins);
                            var winrate = totalGames > 0 ? Math.Round((double)totalWins / totalGames * 100, 2) : 0;

                            var opponents = group
                                .Select(m => new OpponentStats(
                                    OpponentChampionName: m.OpponentChampionName,
                                    OpponentChampionId: m.OpponentChampionId,
                                    GamesPlayed: m.GamesPlayed,
                                    Wins: m.Wins,
                                    Losses: m.GamesPlayed - m.Wins,
                                    Winrate: m.GamesPlayed > 0 ? Math.Round((double)m.Wins / m.GamesPlayed * 100, 2) : 0
                                ))
                                .OrderByDescending(o => o.GamesPlayed)
                                .ToList();

                            return new ChampionRoleMatchup(
                                ChampionName: group.Key.ChampionName,
                                ChampionId: group.Key.ChampionId,
                                Role: group.Key.Role,
                                TotalGames: totalGames,
                                TotalWins: totalWins,
                                Winrate: winrate,
                                Opponents: opponents
                            );
                        })
                        .OrderByDescending(m => m.TotalGames)
                        .ToList();

                    return Results.Ok(new ChampionMatchupsResponse(Matchups: groupedMatchups));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting champion matchups" 
                        : "Invalid operation when getting champion matchups");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting champion matchups");
                }
            });
        }
    }
}

