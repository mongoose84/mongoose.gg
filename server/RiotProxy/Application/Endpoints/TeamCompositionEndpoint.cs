using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamCompositionDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamCompositionEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamCompositionEndpoint(string basePath)
        {
            Route = basePath + "/team-composition/{userId}";
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
                        return Results.BadRequest("Team composition requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get role distribution for each player
                    var roleRecords = await matchParticipantRepo.GetTeamRoleDistributionByPuuIdsAsync(distinctPuuIds, gameMode);

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

                    // Group by player and calculate percentages
                    var playerDistributions = new List<PlayerRoleDistribution>();
                    var rolePreferences = new Dictionary<string, List<(string playerName, double percentage)>>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        if (!playerNames.TryGetValue(puuId, out var playerName)) continue;

                        var playerRoles = roleRecords.Where(r => r.PuuId == puuId).ToList();
                        var totalGames = playerRoles.Sum(r => r.GamesPlayed);

                        var roles = playerRoles
                            .Select(r => new RoleStats(
                                Position: MapPosition(r.Position),
                                GamesPlayed: r.GamesPlayed,
                                Percentage: totalGames > 0 ? Math.Round((double)r.GamesPlayed / totalGames * 100, 1) : 0,
                                Wins: r.Wins,
                                WinRate: r.GamesPlayed > 0 ? Math.Round((double)r.Wins / r.GamesPlayed * 100, 1) : 0
                            ))
                            .OrderByDescending(r => r.GamesPlayed)
                            .ToList();

                        var primaryRole = roles.FirstOrDefault()?.Position ?? "Unknown";
                        var primaryPercentage = roles.FirstOrDefault()?.Percentage ?? 0;

                        playerDistributions.Add(new PlayerRoleDistribution(
                            PlayerName: playerName,
                            Roles: roles,
                            PrimaryRole: primaryRole
                        ));

                        // Track role preferences for conflict detection
                        if (!rolePreferences.ContainsKey(primaryRole))
                        {
                            rolePreferences[primaryRole] = new List<(string, double)>();
                        }
                        rolePreferences[primaryRole].Add((playerName, primaryPercentage));
                    }

                    // Detect role conflicts (multiple players preferring same role)
                    var roleConflicts = rolePreferences
                        .Where(kv => kv.Value.Count > 1)
                        .Select(kv => new RoleConflict(
                            Role: kv.Key,
                            Players: kv.Value.Select(v => v.playerName).ToList(),
                            ConflictScore: Math.Round(kv.Value.Average(v => v.percentage), 1)
                        ))
                        .OrderByDescending(c => c.ConflictScore)
                        .ToList();

                    return Results.Ok(new TeamCompositionResponse(
                        Players: playerDistributions,
                        RoleConflicts: roleConflicts
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting team composition"
                        : "Invalid operation when getting team composition");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team composition");
                }
            });
        }

        private static string MapPosition(string position)
        {
            return position?.ToUpperInvariant() switch
            {
                "TOP" => "Top",
                "JUNGLE" => "Jungle",
                "MIDDLE" or "MID" => "Mid",
                "BOTTOM" or "ADC" => "ADC",
                "UTILITY" or "SUPPORT" => "Support",
                _ => position ?? "Unknown"
            };
        }
    }
}

