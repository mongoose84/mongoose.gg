using Microsoft.AspNetCore.Mvc;
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
                    return Results.Ok(CreateEmptyResponse());
                }

                SideStatsRecord sideStats;

                // Determine which mode to use based on query parameter or player count
                var effectiveMode = mode?.ToLowerInvariant() ?? (distinctPuuIds.Length >= 3 ? "team" : distinctPuuIds.Length == 2 ? "duo" : "solo");

                switch (effectiveMode)
                {
                    case "team" when distinctPuuIds.Length >= 3:
                        sideStats = await matchParticipantRepo.GetTeamSideStatsByPuuIdsAsync(distinctPuuIds);
                        break;
                    case "duo" when distinctPuuIds.Length >= 2:
                        sideStats = await matchParticipantRepo.GetDuoSideStatsByPuuIdsAsync(distinctPuuIds[0], distinctPuuIds[1]);
                        break;
                    default:
                        // Solo mode: aggregate stats from all puuIds
                        var aggregatedBlueGames = 0;
                        var aggregatedBlueWins = 0;
                        var aggregatedRedGames = 0;
                        var aggregatedRedWins = 0;

                        foreach (var puuId in distinctPuuIds)
                        {
                            var stats = await matchParticipantRepo.GetSideStatsByPuuIdAsync(puuId);
                            aggregatedBlueGames += stats.BlueGames;
                            aggregatedBlueWins += stats.BlueWins;
                            aggregatedRedGames += stats.RedGames;
                            aggregatedRedWins += stats.RedWins;
                        }

                        sideStats = new SideStatsRecord(aggregatedBlueGames, aggregatedBlueWins, aggregatedRedGames, aggregatedRedWins);
                        break;
                }

                var totalGames = sideStats.BlueGames + sideStats.RedGames;
                var blueWinRate = sideStats.BlueGames > 0 ? Math.Round((double)sideStats.BlueWins / sideStats.BlueGames * 100, 1) : 0;
                var redWinRate = sideStats.RedGames > 0 ? Math.Round((double)sideStats.RedWins / sideStats.RedGames * 100, 1) : 0;
                var bluePercentage = totalGames > 0 ? Math.Round((double)sideStats.BlueGames / totalGames * 100, 1) : 0;
                var redPercentage = totalGames > 0 ? Math.Round((double)sideStats.RedGames / totalGames * 100, 1) : 0;

                return Results.Ok(new SideStatsResponse(
                    BlueGames: sideStats.BlueGames,
                    BlueWins: sideStats.BlueWins,
                    BlueWinRate: blueWinRate,
                    RedGames: sideStats.RedGames,
                    RedWins: sideStats.RedWins,
                    RedWinRate: redWinRate,
                    TotalGames: totalGames,
                    BluePercentage: bluePercentage,
                    RedPercentage: redPercentage
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

