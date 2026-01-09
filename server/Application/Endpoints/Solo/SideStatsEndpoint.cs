using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Records;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.SideStatsDto;

namespace RiotProxy.Application.Endpoints;

public sealed class SideStatsEndpoint : IEndpoint
{
    public string Route { get; }

    public SideStatsEndpoint(string basePath)
    {
        Route = basePath + "/side-stats/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            [FromRoute] string userId,
            [FromQuery] string? mode,
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] SoloStatsRepository soloStatsRepo,
            [FromServices] DuoStatsRepository duoStatsRepo,
            [FromServices] TeamStatsRepository teamStatsRepo
        ) =>
        {
            try
            {
                var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                if (distinctPuuIds.Length == 0)
                {
                    return Results.Ok(CreateEmptyResponse());
                }

                SideStatsRecord sideStats;

                // Determine which mode to use based on query parameter or player count
                var effectiveMode = mode?.ToLowerInvariant() ?? (distinctPuuIds.Length >= 3 ? "team" : distinctPuuIds.Length == 2 ? "duo" : "solo");

                switch (effectiveMode)
                {
                    case "team" when distinctPuuIds.Length >= 3:
                        sideStats = await teamStatsRepo.GetTeamSideStatsByPuuIdsAsync(distinctPuuIds);
                        break;
                    case "duo" when distinctPuuIds.Length >= 2:
                        sideStats = await duoStatsRepo.GetDuoSideStatsByPuuIdsAsync(distinctPuuIds[0], distinctPuuIds[1]);
                        break;
                    default:
                        // Solo mode: aggregate stats from all puuIds
                        var aggregatedBlueGames = 0;
                        var aggregatedBlueWins = 0;
                        var aggregatedRedGames = 0;
                        var aggregatedRedWins = 0;

                        foreach (var puuId in distinctPuuIds)
                        {
                            var stats = await soloStatsRepo.GetSideStatsByPuuIdAsync(puuId);
                            aggregatedBlueGames += stats.BlueGames;
                            aggregatedBlueWins += stats.BlueWins;
                            aggregatedRedGames += stats.RedGames;
                            aggregatedRedWins += stats.RedWins;
                        }

                        sideStats = new SideStatsRecord(aggregatedBlueGames, aggregatedBlueWins, aggregatedRedGames, aggregatedRedWins);
                        break;
                }

                var totalGames = sideStats.BlueGames + sideStats.RedGames;
                var totalWins = sideStats.BlueWins + sideStats.RedWins;
                
                // Win distribution: what percentage of total wins came from each side
                var blueWinDistribution = totalWins > 0 ? Math.Round((double)sideStats.BlueWins / totalWins * 100, 1) : 0;
                var redWinDistribution = totalWins > 0 ? Math.Round((double)sideStats.RedWins / totalWins * 100, 1) : 0;
                
                // Game distribution: what percentage of games were on each side
                var blueGamePercentage = totalGames > 0 ? Math.Round((double)sideStats.BlueGames / totalGames * 100, 1) : 0;
                var redGamePercentage = totalGames > 0 ? Math.Round((double)sideStats.RedGames / totalGames * 100, 1) : 0;

                return Results.Ok(new SideStatsResponse(
                    BlueGames: sideStats.BlueGames,
                    BlueWins: sideStats.BlueWins,
                    BlueWinRate: blueWinDistribution,
                    RedGames: sideStats.RedGames,
                    RedWins: sideStats.RedWins,
                    RedWinRate: redWinDistribution,
                    TotalGames: totalGames,
                    BluePercentage: blueGamePercentage,
                    RedPercentage: redGamePercentage
                ));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest(ex is ArgumentException
                    ? "Invalid argument when getting side stats"
                    : "Invalid operation when getting side stats");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                return Results.BadRequest("Error when getting side stats");
            }
        });
    }

    private static SideStatsResponse CreateEmptyResponse()
    {
        return new SideStatsResponse(
            BlueGames: 0,
            BlueWins: 0,
            BlueWinRate: 0,
            RedGames: 0,
            RedWins: 0,
            RedWinRate: 0,
            TotalGames: 0,
            BluePercentage: 0,
            RedPercentage: 0
        );
    }
}

