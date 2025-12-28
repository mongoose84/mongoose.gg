using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.ChampionPerformanceDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class ChampionPerformanceEndpoint : IEndpoint
    {
        public string Route { get; }

        public ChampionPerformanceEndpoint(string basePath)
        {
            Route = basePath + "/champion-performance/{userId}";
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
                        return Results.Ok(new ChampionPerformanceResponse(Champions: []));
                    }

                    // Dictionary to aggregate champion stats across all puuids
                    // Key: ChampionId, Value: Dictionary of ServerName -> (GamerName, GamesPlayed, Wins)
                    var championAggregates = new Dictionary<int, (string ChampionName, Dictionary<string, (string GamerName, int GamesPlayed, int Wins)> ServerStats)>();

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

                        // Get champion stats for this puuid
                        var championStats = await matchParticipantRepo.GetChampionStatsByPuuIdAsync(puuId);

                        foreach (var stat in championStats)
                        {
                            if (!championAggregates.ContainsKey(stat.ChampionId))
                            {
                                championAggregates[stat.ChampionId] = (stat.ChampionName, new Dictionary<string, (string, int, int)>());
                            }

                            var (championName, serverStats) = championAggregates[stat.ChampionId];

                            if (!serverStats.ContainsKey(serverName))
                            {
                                serverStats[serverName] = (gamerName, 0, 0);
                            }

                            var (existingGamerName, currentGames, currentWins) = serverStats[serverName];
                            serverStats[serverName] = (existingGamerName, currentGames + stat.GamesPlayed, currentWins + stat.Wins);
                        }
                    }

                    // Convert aggregates to response format
                    var champions = championAggregates
                        .Select(kvp =>
                        {
                            var championId = kvp.Key;
                            var (championName, serverStats) = kvp.Value;

                            var servers = serverStats
                                .Select(ss =>
                                {
                                    var (gamerName, gamesPlayed, wins) = ss.Value;
                                    var winrate = gamesPlayed > 0 ? Math.Round((double)wins / gamesPlayed * 100, 1) : 0;
                                    return new ServerStats(ss.Key, gamerName, gamesPlayed, wins, winrate);
                                })
                                .OrderBy(s => s.ServerName)
                                .ToList();

                            return new ChampionStats(championName, championId, servers);
                        })
                        .OrderByDescending(c => c.Servers.Sum(s => s.GamesPlayed)) // Order by total games played
                        .ToList();

                    return Results.Ok(new ChampionPerformanceResponse(Champions: champions));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting champion performance" 
                        : "Invalid operation when getting champion performance");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting champion performance");
                }
            });
        }
    }
}

