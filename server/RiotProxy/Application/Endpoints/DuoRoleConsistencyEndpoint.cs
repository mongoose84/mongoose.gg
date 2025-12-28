using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoRoleConsistencyDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoRoleConsistencyEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoRoleConsistencyEndpoint(string basePath)
        {
            Route = basePath + "/duo-role-consistency/{userId}";
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
                    
                    // Duo dashboard requires exactly 2 players
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo role consistency requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get the most common game mode for filtering
                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var mostCommonGameMode = duoStats?.MostCommonQueueType;

                    var playerDistributions = new List<PlayerRoleDistribution>();

                    // Get role distribution for each player in duo games
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var serverName = gamer.Tagline.ToUpperInvariant();
                        var playerName = $"{gamer.GamerName}#{serverName}";

                        // Get role distribution for this player in duo games
                        var roleDistribution = await matchParticipantRepo.GetDuoRoleDistributionByPuuIdsAsync(puuId1, puuId2, puuId, mostCommonGameMode);
                        
                        // Calculate total games and percentages
                        var totalGames = roleDistribution.Sum(r => r.GamesPlayed);
                        
                        var roles = roleDistribution
                            .Select(r => new RoleStats(
                                Position: r.Position,
                                GamesPlayed: r.GamesPlayed,
                                Percentage: totalGames > 0 ? Math.Round((double)r.GamesPlayed / totalGames * 100, 1) : 0
                            ))
                            .OrderByDescending(r => r.GamesPlayed)
                            .ToList();

                        playerDistributions.Add(new PlayerRoleDistribution(
                            PlayerName: playerName,
                            Roles: roles
                        ));
                    }

                    return Results.Ok(new DuoRoleConsistencyResponse(Players: playerDistributions));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting duo role consistency" 
                        : "Invalid operation when getting duo role consistency");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo role consistency");
                }
            });
        }
    }
}

