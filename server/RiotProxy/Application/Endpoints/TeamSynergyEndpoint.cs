using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamSynergyDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamSynergyEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamSynergyEndpoint(string basePath)
        {
            Route = basePath + "/team-synergy/{userId}";
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
                        return Results.BadRequest("Team synergy requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get pairwise synergy data
                    var synergyRecords = await matchParticipantRepo.GetTeamPairSynergyByPuuIdsAsync(distinctPuuIds, gameMode);

                    // Get role distribution to find most common role per player
                    var roleRecords = await matchParticipantRepo.GetTeamRoleDistributionByPuuIdsAsync(distinctPuuIds, gameMode);
                    var playerRoles = roleRecords
                        .GroupBy(r => r.PuuId)
                        .ToDictionary(
                            g => g.Key,
                            g => FormatRole(g.OrderByDescending(r => r.GamesPlayed).First().Position)
                        );

                    // Build player name lookup
                    var playerNames = new Dictionary<string, string>();
                    var playerList = new List<PlayerInfo>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            var name = $"{gamer.GamerName}#{gamer.Tagline.ToUpperInvariant()}";
                            playerNames[puuId] = name;
                            var role = playerRoles.GetValueOrDefault(puuId, "Unknown");
                            playerList.Add(new PlayerInfo(name, role));
                        }
                    }

                    // Convert to response format
                    var playerPairs = synergyRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId1) && playerNames.ContainsKey(r.PuuId2))
                        .Select(r => new PlayerPairSynergy(
                            Player1: playerNames[r.PuuId1],
                            Player1Role: playerRoles.GetValueOrDefault(r.PuuId1, "Unknown"),
                            Player2: playerNames[r.PuuId2],
                            Player2Role: playerRoles.GetValueOrDefault(r.PuuId2, "Unknown"),
                            GamesPlayed: r.GamesPlayed,
                            Wins: r.Wins,
                            WinRate: r.GamesPlayed > 0 ? Math.Round((double)r.Wins / r.GamesPlayed * 100, 1) : 0.0
                        ))
                        .OrderByDescending(p => p.WinRate)
                        .ThenByDescending(p => p.GamesPlayed)
                        .ToList();

                    return Results.Ok(new TeamSynergyResponse(
                        PlayerPairs: playerPairs,
                        Players: playerList
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting team synergy"
                        : "Invalid operation when getting team synergy");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team synergy");
                }
            });
        }

        private static string FormatRole(string position) => position?.ToUpperInvariant() switch
        {
            "TOP" => "Top",
            "JUNGLE" => "Jungle",
            "MIDDLE" => "Mid",
            "BOTTOM" => "Bot",
            "UTILITY" => "Support",
            _ => position ?? "Unknown"
        };
    }
}

