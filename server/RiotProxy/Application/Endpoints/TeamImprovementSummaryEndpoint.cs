using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamImprovementSummaryDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamImprovementSummaryEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamImprovementSummaryEndpoint(string basePath)
        {
            Route = basePath + "/team-improvement-summary/{userId}";
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
                        return Results.BadRequest("Team improvement summary requires at least 3 players");
                    }

                    var insights = new List<Insight>();
                    var strengths = new List<string>();
                    var weaknesses = new List<string>();

                    // Get team stats
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    if (teamStats == null || teamStats.GamesPlayed == 0)
                    {
                        return Results.Ok(new TeamImprovementSummaryResponse(
                            Insights: new List<Insight> { new Insight("info", "Not enough team games to analyze", 0) },
                            Strengths: new List<string>(),
                            Weaknesses: new List<string>()
                        ));
                    }

                    // Get player names
                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = $"{gamer.GamerName}#{gamer.Tagline.ToUpperInvariant()}";
                        }
                    }

                    // 1. Overall win rate insight
                    var winRate = (double)teamStats.Wins / teamStats.GamesPlayed * 100;
                    if (winRate >= 55)
                    {
                        strengths.Add($"Strong team synergy with {winRate:F0}% win rate over {teamStats.GamesPlayed} games");
                        insights.Add(new Insight("positive", $"Your team has a {winRate:F0}% win rate - keep playing together!", 1));
                    }
                    else if (winRate < 45)
                    {
                        weaknesses.Add($"Team win rate of {winRate:F0}% needs improvement");
                        insights.Add(new Insight("negative", $"Team win rate is {winRate:F0}% - consider reviewing your strategy", 1));
                    }

                    // 2. Best player pair synergy
                    var synergyRecords = await matchParticipantRepo.GetTeamPairSynergyByPuuIdsAsync(distinctPuuIds, gameMode);
                    var bestPair = synergyRecords
                        .Where(r => r.GamesPlayed >= 3)
                        .OrderByDescending(r => (double)r.Wins / r.GamesPlayed)
                        .FirstOrDefault();
                    
                    if (bestPair != null && playerNames.ContainsKey(bestPair.PuuId1) && playerNames.ContainsKey(bestPair.PuuId2))
                    {
                        var pairWinRate = Math.Round((double)bestPair.Wins / bestPair.GamesPlayed * 100, 0);
                        var p1 = playerNames[bestPair.PuuId1].Split('#')[0];
                        var p2 = playerNames[bestPair.PuuId2].Split('#')[0];
                        insights.Add(new Insight("synergy", $"Best duo: {p1} + {p2} ({pairWinRate}% WR)", 2));
                        strengths.Add($"{p1} and {p2} have great synergy");
                    }

                    // 3. KDA insight
                    var avgKda = teamStats.TotalDeaths > 0 
                        ? (double)(teamStats.TotalKills + teamStats.TotalAssists) / teamStats.TotalDeaths 
                        : teamStats.TotalKills + teamStats.TotalAssists;
                    
                    if (avgKda >= 3.0)
                    {
                        strengths.Add($"Excellent team KDA of {avgKda:F1}");
                        insights.Add(new Insight("positive", $"Team KDA of {avgKda:F1} shows great coordination", 3));
                    }
                    else if (avgKda < 2.0)
                    {
                        weaknesses.Add("Team KDA needs work - dying too often");
                        insights.Add(new Insight("negative", $"Team KDA is {avgKda:F1} - focus on survival", 3));
                    }

                    // 4. Game duration insight
                    var avgDuration = teamStats.AvgDurationSeconds / 60.0;
                    if (avgDuration < 25)
                    {
                        insights.Add(new Insight("duration", $"Your team wins quickly (avg {avgDuration:F0} min) - early game focused", 4));
                    }
                    else if (avgDuration > 35)
                    {
                        insights.Add(new Insight("duration", $"Games tend to go long (avg {avgDuration:F0} min) - late game team", 4));
                    }

                    // 5. Role conflict detection
                    var roleRecords = await matchParticipantRepo.GetTeamRoleDistributionByPuuIdsAsync(distinctPuuIds, gameMode);
                    var roleGroups = roleRecords.GroupBy(r => r.Position).Where(g => g.Count() > 1).ToList();
                    if (roleGroups.Any())
                    {
                        var conflictRole = roleGroups.First().Key;
                        insights.Add(new Insight("role", $"Multiple players compete for {conflictRole} - coordinate picks!", 5));
                        weaknesses.Add($"Role conflict in {conflictRole} position");
                    }

                    return Results.Ok(new TeamImprovementSummaryResponse(
                        Insights: insights.OrderBy(i => i.Priority).ToList(),
                        Strengths: strengths,
                        Weaknesses: weaknesses
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when getting team improvement summary"
                        : "Invalid operation when getting team improvement summary");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team improvement summary");
                }
            });
        }
    }
}

