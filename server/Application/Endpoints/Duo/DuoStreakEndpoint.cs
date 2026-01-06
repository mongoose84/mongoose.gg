using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Records;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.DuoTrendAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class DuoStreakEndpoint : IEndpoint
    {
        public string Route { get; }

        public DuoStreakEndpoint(string basePath)
        {
            Route = basePath + "/duo-streak/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                [FromRoute] string userId,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] DuoStatsRepository duoStatsRepo
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();
                    
                    if (distinctPuuIds.Length != 2)
                    {
                        return Results.BadRequest("Duo streak requires exactly 2 players");
                    }

                    var puuId1 = distinctPuuIds[0];
                    var puuId2 = distinctPuuIds[1];

                    var duoStats = await duoStatsRepo.GetDuoStatsByPuuIdsAsync(puuId1, puuId2);
                    var gameMode = duoStats?.MostCommonQueueType;

                    var streakData = await duoStatsRepo.GetDuoStreakAsync(puuId1, puuId2);

                    if (streakData == null)
                    {
                        return Results.Ok(new DuoStreakResponse(
                            CurrentStreak: 0,
                            IsWinStreak: false,
                            LongestWinStreak: 0,
                            LongestLossStreak: 0,
                            StreakMessage: "No games played together yet"
                        ));
                    }

                    var streakMessage = GenerateStreakMessage(streakData);

                    return Results.Ok(new DuoStreakResponse(
                        CurrentStreak: streakData.CurrentStreak,
                        IsWinStreak: streakData.IsWinStreak,
                        LongestWinStreak: streakData.LongestWinStreak,
                        LongestLossStreak: streakData.LongestLossStreak,
                        StreakMessage: streakMessage
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for duo streak");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting duo streak");
                }
            });
        }

        private static string GenerateStreakMessage(DuoStreakRecord streak)
        {
            if (streak.IsWinStreak)
            {
                return streak.CurrentStreak switch
                {
                    >= 10 => $"🔥 UNSTOPPABLE! {streak.CurrentStreak} wins in a row!",
                    >= 7 => $"🔥 On fire! {streak.CurrentStreak} game win streak!",
                    >= 5 => $"💪 Strong! {streak.CurrentStreak} consecutive wins!",
                    >= 3 => $"👍 Nice! {streak.CurrentStreak} wins in a row!",
                    _ => $"Currently on a {streak.CurrentStreak} game win streak"
                };
            }
            else
            {
                return streak.CurrentStreak switch
                {
                    >= 7 => $"😰 Rough patch... {streak.CurrentStreak} losses. Take a break?",
                    >= 5 => $"😓 {streak.CurrentStreak} losses in a row. Hang in there!",
                    >= 3 => $"📉 {streak.CurrentStreak} game losing streak. Time to turn it around!",
                    _ => $"Currently on a {streak.CurrentStreak} game losing streak"
                };
            }
        }
    }
}

