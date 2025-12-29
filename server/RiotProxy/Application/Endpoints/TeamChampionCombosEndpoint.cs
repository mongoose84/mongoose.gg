using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Endpoint for fetching best team champion combinations.
/// </summary>
public class TeamChampionCombosEndpoint : IEndpoint
{
    public string Route { get; }

    public TeamChampionCombosEndpoint(string basePath)
    {
        Route = basePath + "/team-champion-combos/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            [FromRoute] string userId,
            [FromServices] GamerRepository gamerRepo,
            [FromServices] UserGamerRepository userGamerRepo,
            [FromServices] LolMatchParticipantRepository participantRepo) =>
        {
            try
            {
                var userIdInt = int.TryParse(userId, out var result) ? result : throw new ArgumentException($"Invalid userId: {userId}");
                var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                if (distinctPuuIds.Length < 3)
                {
                    return Results.BadRequest("Team analytics requires at least 3 players.");
                }

                // Build gamer name lookup
                var gamerNames = new Dictionary<string, string>();
                foreach (var puuId in distinctPuuIds)
                {
                    var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                    if (gamer != null)
                    {
                        gamerNames[puuId] = !string.IsNullOrEmpty(gamer.GamerName) ? gamer.GamerName : puuId[..8];
                    }
                }

                var comboRecords = await participantRepo.GetTeamChampionCombosByPuuIdsAsync(distinctPuuIds, 10);

                var combos = comboRecords.Select(record =>
                {
                    var champions = record.Champions.Select(c => new TeamChampionCombosDto.ChampionPick(
                        PlayerName: gamerNames.GetValueOrDefault(c.Puuid, c.GameName),
                        ChampionId: c.ChampionId,
                        ChampionName: c.ChampionName
                    )).ToList();

                    return new TeamChampionCombosDto.ChampionCombo(
                        Champions: champions,
                        GamesPlayed: record.GamesPlayed,
                        Wins: record.Wins,
                        WinRate: Math.Round((double)record.Wins / record.GamesPlayed * 100, 1)
                    );
                }).ToList();

                return Results.Ok(new TeamChampionCombosDto.TeamChampionCombosResponse(
                    Combos: combos,
                    TotalUniqueCompos: combos.Count
                ));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Results.BadRequest(ex.Message);
            }
        });
    }
}

