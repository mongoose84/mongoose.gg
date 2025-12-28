using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoLaneMatchupDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoLaneMatchupEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoLaneMatchupEndpoint(string basePath)
        {
            Route = basePath + "/duo-lane-matchup/{userId}";
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
                    
                    // Duo dashboard requires exactly 2 players
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo lane matchup requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get the most common game mode for filtering
                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var mostCommonGameMode = duoStats?.MostCommonQueueType;

                    // Get lane combination statistics
                    var laneComboRecords = await matchParticipantRepo.GetDuoLaneCombosByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);

                    // Convert to response format
                    var laneCombos = laneComboRecords
                        .Select(combo =>
                        {
                            var winrate = combo.GamesPlayed > 0
                                ? Math.Round((double)combo.Wins / combo.GamesPlayed * 100, 1)
                                : 0.0;

                            // Format lane names
                            var lane1Formatted = FormatLane(combo.Lane1);
                            var lane2Formatted = FormatLane(combo.Lane2);
                            var laneCombo = $"{lane1Formatted} + {lane2Formatted}";

                            return new LaneComboStats(
                                LaneCombo: laneCombo,
                                GamesPlayed: combo.GamesPlayed,
                                Wins: combo.Wins,
                                Winrate: winrate
                            );
                        })
                        .OrderByDescending(c => c.GamesPlayed)
                        .ToList();

                    return Results.Ok(new DuoLaneMatchupResponse(LaneCombos: laneCombos));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting duo lane matchup" 
                        : "Invalid operation when getting duo lane matchup");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo lane matchup");
                }
            });
        }

        private static string FormatLane(string lane)
        {
            return lane switch
            {
                "TOP" => "Top",
                "JUNGLE" => "Jungle",
                "MIDDLE" => "Mid",
                "BOTTOM" => "Bot",
                "UTILITY" => "Support",
                "UNKNOWN" => "Unknown",
                _ => lane
            };
        }
    }
}

