using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.RoleDistributionDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class RoleDistributionEndpoint : IEndpoint
    {
        public string Route { get; }

        public RoleDistributionEndpoint(string basePath)
        {
            Route = basePath + "/role-distribution/{userId}";
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
                    
                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(new RoleDistributionResponse(Gamers: []));
                    }

                    var gamerDistributions = new List<GamerRoleDistribution>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        // Extract server name from tagline (e.g., "EUW", "EUNE")
                        var serverName = gamer.Tagline.ToUpperInvariant();
                        var gamerName = $"{gamer.GamerName}#{serverName}";

                        // Get role distribution for this puuid
                        var roleDistribution = await matchParticipantRepo.GetRoleDistributionByPuuIdAsync(puuId);
                        
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

                        gamerDistributions.Add(new GamerRoleDistribution(
                            GamerName: gamerName,
                            ServerName: serverName,
                            Roles: roles
                        ));
                    }

                    return Results.Ok(new RoleDistributionResponse(Gamers: gamerDistributions));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting role distribution" 
                        : "Invalid operation when getting role distribution");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting role distribution");
                }
            });
        }
    }
}

