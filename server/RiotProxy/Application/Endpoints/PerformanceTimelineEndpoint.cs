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
                    // Calculate match limit based on period
                    // Note: Even "all" has a safety limit to prevent memory issues
                    const int maxSafeLimit = 500; // Safety limit for "all" to prevent OOM

                    int? matchLimit = period switch
                    {
                        "20" => 20,
                        "50" => 50,
                        "100" => 100,
                        "all" => maxSafeLimit,
                        _ => 50 // Default to 50 matches
                    };

                    // Rolling average window in days
                    // This determines how many days of games to include when calculating rolling winrate
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

                    // TODO: Performance optimization needed - this is an N+1 query problem
                    // Consider batching: fetch all gamers at once, then all match records
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                        {
                            Console.WriteLine($"Gamer with puuid {puuId} not found in database.");
                            continue;
                        }

                        var gamerName = $"{gamer.GamerName}#{gamer.Tagline}";
                        var matchRecords = await matchParticipantRepo.GetMatchPerformanceTimelineAsync(puuId, fromDate: null, limit: matchLimit);
                        
                        // Records are already ordered oldest to newest from SQL
                        var dataPoints = new List<PerformanceDataPoint>();
                        var allRecordsWithDates = matchRecords.ToList();
                        int gameNumber = 0;

                        // TODO: Performance optimization needed - this is O(n²) complexity
                        // For 100 games, this does 10,000 iterations
                        // Consider using a sliding window algorithm or pre-computing windows
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
                                ? Math.Round(winsInWindow / (double)gamesInWindow.Count * 100, 1)
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
