using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Endpoint for fetching team role pair effectiveness data.
/// </summary>
public class TeamRolePairEffectivenessEndpoint : IEndpoint
{
    public string Route { get; }

    public TeamRolePairEffectivenessEndpoint(string basePath)
    {
        Route = basePath + "/team-role-pair-effectiveness/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            [FromRoute] string userId,
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] LolMatchParticipantRepository participantRepo) =>
        {
            try
            {
                var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                if (distinctPuuIds.Length < 3)
                {
                    return Results.BadRequest("Team analytics requires at least 3 players.");
                }

                var pairRecords = await participantRepo.GetTeamRolePairStatsByPuuIdsAsync(distinctPuuIds);

                var rolePairs = pairRecords
                    .Where(r => !string.IsNullOrEmpty(r.Role1) && !string.IsNullOrEmpty(r.Role2))
                    .Select(r => new TeamRolePairEffectivenessDto.RolePairStats(
                        Role1: FormatRole(r.Role1),
                        Role2: FormatRole(r.Role2),
                        GamesPlayed: r.GamesPlayed,
                        Wins: r.Wins,
                        WinRate: r.GamesPlayed > 0 ? Math.Round((double)r.Wins / r.GamesPlayed * 100, 1) : 0
                    ))
                    // Sort by win rate descending, then by games played descending as tiebreaker
                    .OrderByDescending(r => r.WinRate)
                    .ThenByDescending(r => r.GamesPlayed)
                    .ToList();

                // Filter pairs with enough games for statistical significance
                var significantPairs = rolePairs.Where(r => r.GamesPlayed >= 3).ToList();
                var bestPair = significantPairs.FirstOrDefault();
                // Only show worst pair if it's different from best pair (different win rate)
                var worstPair = significantPairs.LastOrDefault();
                // If best and worst have the same win rate, don't show worst as "needs work"
                if (bestPair != null && worstPair != null && bestPair.WinRate == worstPair.WinRate)
                {
                    worstPair = null;
                }

                return Results.Ok(new TeamRolePairEffectivenessDto.TeamRolePairEffectivenessResponse(
                    RolePairs: rolePairs,
                    BestPair: bestPair,
                    WorstPair: worstPair
                ));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Results.BadRequest(ex.Message);
            }
        });
    }

    private static string FormatRole(string role)
    {
        return role switch
        {
            "TOP" => "Top",
            "JUNGLE" => "Jungle",
            "MIDDLE" => "Mid",
            "BOTTOM" => "Bot",
            "UTILITY" => "Support",
            _ => role
        };
    }
}

