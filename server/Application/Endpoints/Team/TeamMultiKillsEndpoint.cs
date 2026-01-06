using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Database.Repositories;
using static RiotProxy.Application.DTOs.TeamKillAnalysisDto;

namespace RiotProxy.Application.Endpoints
{
    public sealed class TeamMultiKillsEndpoint : IEndpoint
    {
        public string Route { get; }

        public TeamMultiKillsEndpoint(string basePath)
        {
            Route = basePath + "/team-multi-kills/{userId}";
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
                        return Results.BadRequest("Team multi-kill analysis requires at least 3 players");
                    }

                    // Get team stats for game mode filtering
                    var teamStats = await matchParticipantRepo.GetTeamStatsByPuuIdsAsync(distinctPuuIds);
                    var gameMode = teamStats?.MostCommonGameMode;

                    // Get multi-kill data
                    var multiKillRecords = await matchParticipantRepo.GetTeamMultiKillsAsync(distinctPuuIds, gameMode);

                    // Build player name lookup
                    var playerNames = new Dictionary<string, string>();
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer != null)
                        {
                            playerNames[puuId] = gamer.GamerName;
                        }
                    }

                    // Convert to response format
                    var players = multiKillRecords
                        .Where(r => playerNames.ContainsKey(r.PuuId))
                        .Select(r => new PlayerMultiKills(
                            PlayerName: playerNames[r.PuuId],
                            DoubleKills: r.DoubleKills,
                            TripleKills: r.TripleKills,
                            QuadraKills: r.QuadraKills,
                            PentaKills: r.PentaKills,
                            TotalMultiKills: r.DoubleKills + r.TripleKills + r.QuadraKills + r.PentaKills
                        ))
                        .OrderByDescending(p => p.PentaKills)
                        .ThenByDescending(p => p.QuadraKills)
                        .ThenByDescending(p => p.TripleKills)
                        .ThenByDescending(p => p.TotalMultiKills)
                        .ToList();

                    // Calculate team totals
                    var teamTotals = new TeamMultiKillTotals(
                        DoubleKills: multiKillRecords.Sum(r => r.DoubleKills),
                        TripleKills: multiKillRecords.Sum(r => r.TripleKills),
                        QuadraKills: multiKillRecords.Sum(r => r.QuadraKills),
                        PentaKills: multiKillRecords.Sum(r => r.PentaKills)
                    );

                    return Results.Ok(new MultiKillsResponse(
                        Players: players,
                        TeamTotals: teamTotals
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Invalid request for team multi-kills");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when getting team multi-kills");
                }
            });
        }
    }
}

