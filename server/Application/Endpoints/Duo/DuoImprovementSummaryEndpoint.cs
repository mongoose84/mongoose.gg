using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoImprovementSummaryDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoImprovementSummaryEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoImprovementSummaryEndpoint(string basePath)
        {
            Route = basePath + "/duo-improvement-summary/{userId}";
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
                    
                    // Duo dashboard requires exactly 2 players
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo improvement summary requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    // Get the most common game mode for filtering
                    var duoStats = await matchParticipantRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var mostCommonGameMode = duoStats?.MostCommonQueueType;

                    var insights = new List<Insight>();

                    // Get gamer names
                    var gamer1 = await gamerRepo.GetByPuuIdAsync(puuId1);
                    var gamer2 = await gamerRepo.GetByPuuIdAsync(puuId2);
                    var player1Name = gamer1 != null ? $"{gamer1.GamerName}#{gamer1.Tagline.ToUpperInvariant()}" : "Player 1";
                    var player2Name = gamer2 != null ? $"{gamer2.GamerName}#{gamer2.Tagline.ToUpperInvariant()}" : "Player 2";

                    // 1. Best champion pairing insight
                    var championSynergies = await matchParticipantRepo.GetChampionSynergyByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);
                    var bestSynergy = championSynergies
                        .Where(s => s.GamesPlayed >= 3)
                        .OrderByDescending(s => (double)s.Wins / s.GamesPlayed)
                        .ThenByDescending(s => s.GamesPlayed)
                        .FirstOrDefault();

                    if (bestSynergy != null)
                    {
                        var winrate = Math.Round((double)bestSynergy.Wins / bestSynergy.GamesPlayed * 100, 0);
                        insights.Add(new Insight(
                            Type: "champion",
                            Text: $"Best pairing: {bestSynergy.ChampionName1} + {bestSynergy.ChampionName2} ({winrate}% WR, {bestSynergy.GamesPlayed} games)"
                        ));
                    }

                    // 2. Match duration performance insight
                    var durationStats = await matchParticipantRepo.GetDuoDurationStatsByPuuIdsAsync(puuId1, puuId2, mostCommonGameMode);
                    var longGames = durationStats.Where(d => d.MinMinutes >= 35).ToList();
                    if (longGames.Any())
                    {
                        var totalLongGames = longGames.Sum(d => d.GamesPlayed);
                        var totalLongWins = longGames.Sum(d => d.Wins);
                        var longGameWinrate = totalLongGames > 0 ? Math.Round((double)totalLongWins / totalLongGames * 100, 0) : 0;

                        if (longGameWinrate < 48)
                        {
                            insights.Add(new Insight(
                                Type: "duration",
                                Text: $"Weak late games (>35 min): {longGameWinrate}% WR"
                            ));
                        }
                        else if (longGameWinrate > 55)
                        {
                            insights.Add(new Insight(
                                Type: "duration",
                                Text: $"Strong late games (>35 min): {longGameWinrate}% WR - you scale well"
                            ));
                        }
                    }

                    // 3. Death efficiency insight
                    var efficiency1 = await matchParticipantRepo.GetDuoKillEfficiencyByPuuIdsAsync(puuId1, puuId2, puuId1, mostCommonGameMode);
                    var efficiency2 = await matchParticipantRepo.GetDuoKillEfficiencyByPuuIdsAsync(puuId1, puuId2, puuId2, mostCommonGameMode);

                    if (efficiency1 != null && efficiency2 != null)
                    {
                        var deathShare1 = efficiency1.TeamDeathsInLosses > 0
                            ? Math.Round((double)efficiency1.DeathsInLosses / efficiency1.TeamDeathsInLosses * 100, 0)
                            : 0;
                        var deathShare2 = efficiency2.TeamDeathsInLosses > 0
                            ? Math.Round((double)efficiency2.DeathsInLosses / efficiency2.TeamDeathsInLosses * 100, 0)
                            : 0;

                        if (deathShare1 > 55)
                        {
                            insights.Add(new Insight(
                                Type: "negative",
                                Text: $"{player1Name} accounts for {deathShare1}% of deaths in losses"
                            ));
                        }
                        else if (deathShare2 > 55)
                        {
                            insights.Add(new Insight(
                                Type: "negative",
                                Text: $"{player2Name} accounts for {deathShare2}% of deaths in losses"
                            ));
                        }
                    }

                    // 4. Off-role games insight
                    var roleDistribution1 = await matchParticipantRepo.GetDuoRoleDistributionByPuuIdsAsync(puuId1, puuId2, puuId1, mostCommonGameMode);
                    var roleDistribution2 = await matchParticipantRepo.GetDuoRoleDistributionByPuuIdsAsync(puuId1, puuId2, puuId2, mostCommonGameMode);

                    var unknownGames1 = roleDistribution1.FirstOrDefault(r => r.Position == "UNKNOWN")?.GamesPlayed ?? 0;
                    var unknownGames2 = roleDistribution2.FirstOrDefault(r => r.Position == "UNKNOWN")?.GamesPlayed ?? 0;
                    var totalGames1 = roleDistribution1.Sum(r => r.GamesPlayed);
                    var totalGames2 = roleDistribution2.Sum(r => r.GamesPlayed);

                    var unknownPercentage1 = totalGames1 > 0 ? Math.Round((double)unknownGames1 / totalGames1 * 100, 0) : 0;
                    var unknownPercentage2 = totalGames2 > 0 ? Math.Round((double)unknownGames2 / totalGames2 * 100, 0) : 0;

                    if (unknownPercentage1 > 10 || unknownPercentage2 > 10)
                    {
                        insights.Add(new Insight(
                            Type: "role",
                            Text: $"High autofill/off-role games detected - may impact performance"
                        ));
                    }

                    // 5. Overall duo performance
                    if (duoStats != null)
                    {
                        var duoWinrate = duoStats.GamesPlayed > 0
                            ? Math.Round((double)duoStats.Wins / duoStats.GamesPlayed * 100, 0)
                            : 0;

                        if (duoWinrate >= 55)
                        {
                            insights.Add(new Insight(
                                Type: "positive",
                                Text: $"Strong duo synergy: {duoWinrate}% WR over {duoStats.GamesPlayed} games"
                            ));
                        }
                    }

                    return Results.Ok(new DuoImprovementSummaryResponse(Insights: insights));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException 
                        ? "Invalid argument when getting duo improvement summary" 
                        : "Invalid operation when getting duo improvement summary");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo improvement summary");
                }
            });
        }
    }
}

