using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.PerformanceTimelineDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class PerformanceTimelineEndpoint : IEndpoint
    {
        public string Route { get; }

        public PerformanceTimelineEndpoint(string basePath)
        {
            Route = basePath + "/performance/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromQuery] string? period,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] LolMatchParticipantRepository matchParticipantRepo
            ) =>
            {
                try
                {
                    // Calculate date range based on period
                    DateTime? fromDate = period switch
                    {
                        "1w" => DateTime.UtcNow.AddDays(-7),
                        "1m" => DateTime.UtcNow.AddMonths(-1),
                        "3m" => DateTime.UtcNow.AddMonths(-3),
                        "6m" => DateTime.UtcNow.AddMonths(-6),
                        "all" => null,
                        _ => DateTime.UtcNow.AddMonths(-3) // Default to 3 months
                    };
                    
                    // Rolling average window in days
                    const int rollingWindowDays = 7;

                    var userIdInt = int.TryParse(userId, out var result) 
                        ? result 
                        : throw new ArgumentException($"Invalid userId: {userId}");

                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.Ok(new PerformanceTimelineResponse(Gamers: []));
                    }

                    var gamerTimelines = new List<GamerTimeline>();

                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var matchRecords = await matchParticipantRepo.GetMatchPerformanceTimelineAsync(puuId, fromDate);
                        
                        // Records are already ordered oldest to newest from SQL
                        var dataPoints = new List<PerformanceDataPoint>();
                        var allRecordsWithDates = matchRecords.ToList();
                        int gameNumber = 0;

                        foreach (var record in allRecordsWithDates)
                        {
                            gameNumber++;
                            
                            // Calculate rolling winrate over the last N days
                            var windowStart = record.GameEndTimestamp.AddDays(-rollingWindowDays);
                            var gamesInWindow = allRecordsWithDates
                                .Where(r => r.GameEndTimestamp >= windowStart && r.GameEndTimestamp <= record.GameEndTimestamp)
                                .ToList();
                            
                            var winsInWindow = gamesInWindow.Count(r => r.Win);
                            var rollingWinrate = gamesInWindow.Count > 0 
                                ? Math.Round((winsInWindow / (double)gamesInWindow.Count) * 100, 1) 
                                : 50; // Default to 50% if no games in window

                            var goldPerMin = record.DurationMinutes > 0 
                                ? Math.Round(record.GoldEarned / record.DurationMinutes, 1) 
                                : 0;

                            var csPerMin = record.DurationMinutes > 0 
                                ? Math.Round(record.CreepScore / record.DurationMinutes, 1) 
                                : 0;

                            dataPoints.Add(new PerformanceDataPoint(
                                GameNumber: gameNumber,
                                Winrate: rollingWinrate,
                                GoldPerMin: goldPerMin,
                                CsPerMin: csPerMin,
                                Win: record.Win,
                                GameEndTimestamp: record.GameEndTimestamp,
                                Patch: null // Could be extracted from MatchId if needed (e.g., "NA1_12345" doesn't contain patch)
                            ));
                        }

                        gamerTimelines.Add(new GamerTimeline(
                            GamerName: gamerName,
                            DataPoints: dataPoints
                        ));
                    }

                    return Results.Ok(new PerformanceTimelineResponse(Gamers: gamerTimelines));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting performance timeline"
                        : "Invalid operation when getting performance timeline");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting performance timeline");
                }
            });
        }
    }
}
