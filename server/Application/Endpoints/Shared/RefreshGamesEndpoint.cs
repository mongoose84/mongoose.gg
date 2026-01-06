using Microsoft.AspNetCore.Mvc;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using System.Text.Json;

namespace RiotProxy.Application.Endpoints
{
    public sealed class RefreshGamesEndpoint : IEndpoint
    {
        public string Route { get; }
        private const int TargetGameCount = 100;

        public RefreshGamesEndpoint(string basePath)
        {
            Route = basePath + "/refresh-games/{userId}";
        }

        public void Configure(WebApplication app)
        {
            app.MapPost(Route, async (
                [FromRoute] string userId,
                [FromServices] UserGamerRepository userGamerRepo,
                [FromServices] GamerRepository gamerRepo,
                [FromServices] LolMatchRepository matchRepo,
                [FromServices] LolMatchParticipantRepository participantRepo,
                [FromServices] IRiotApiClient riotApiClient,
                CancellationToken ct
                ) =>
            {
                try
                {
                    var userIdInt = int.TryParse(userId, out var result)
                        ? result
                        : throw new ArgumentException($"Invalid userId: {userId}");

                    var puuIds = await userGamerRepo.GetGamersPuuIdByUserIdAsync(userIdInt);
                    var distinctPuuIds = (puuIds ?? []).Distinct().ToArray();

                    if (distinctPuuIds.Length == 0)
                    {
                        return Results.NotFound("No gamers found for this user");
                    }

                    int totalNewGames = 0;

                    // PHASE 1: Always fetch newest games first (since LastChecked)
                    foreach (var puuId in distinctPuuIds)
                    {
                        var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                        if (gamer == null)
                            continue;

                        Console.WriteLine($"Fetching NEW games for gamer: {gamer.GamerName}");
                        var existingMatchIds = await participantRepo.GetMatchIdsForPuuidAsync(puuId);
                        var existingSet = new HashSet<string>(existingMatchIds);

                        // Use startTime to only get games since last check
                        long? startTime = null;
                        if (gamer.LastChecked != DateTime.MinValue)
                        {
                            startTime = new DateTimeOffset(gamer.LastChecked).ToUnixTimeSeconds();
                        }

                        int start = 0;
                        const int pageSize = 20;
                        int gamerNewGames = 0;
                        bool foundExisting = false;

                        while (!foundExisting && start < 100)
                        {
                            var matchHistory = await riotApiClient.GetMatchHistoryAsync(puuId, start, pageSize, startTime, ct);

                            if (matchHistory.Count == 0)
                                break;

                            foreach (var match in matchHistory)
                            {
                                if (existingSet.Contains(match.MatchId))
                                {
                                    foundExisting = true;
                                    break;
                                }

                                await matchRepo.AddMatchIfNotExistsAsync(match);
                                var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId, ct);
                                await ProcessMatch(match, matchInfoJson, matchRepo, participantRepo, gamer, gamerRepo);

                                existingSet.Add(match.MatchId);
                                gamerNewGames++;
                            }

                            if (matchHistory.Count < pageSize)
                                break;

                            start += pageSize;
                        }

                        totalNewGames += gamerNewGames;
                        Console.WriteLine($"Added {gamerNewGames} NEW games for {gamer.GamerName}");
                    }

                    // Calculate total games after fetching new ones
                    int totalExistingGames = 0;
                    foreach (var puuId in distinctPuuIds)
                    {
                        var count = await participantRepo.GetMatchesCountByPuuIdAsync(puuId);
                        totalExistingGames += count;
                    }

                    Console.WriteLine($"User {userId} now has {totalExistingGames} games after fetching new ones.");

                    // PHASE 2: If below 100 games, backfill with older games
                    int gamesNeeded = Math.Max(0, TargetGameCount - totalExistingGames);

                    if (gamesNeeded > 0)
                    {
                        Console.WriteLine($"User needs {gamesNeeded} more games to reach {TargetGameCount}. Backfilling...");

                        foreach (var puuId in distinctPuuIds)
                        {
                            if (gamesNeeded <= 0)
                                break;

                            var gamer = await gamerRepo.GetByPuuIdAsync(puuId);
                            if (gamer == null)
                                continue;

                            Console.WriteLine($"Backfilling games for gamer: {gamer.GamerName}");
                            var existingMatchIds = await participantRepo.GetMatchIdsForPuuidAsync(puuId);
                            var existingSet = new HashSet<string>(existingMatchIds);

                            int start = 0;
                            const int pageSize = 20;
                            int gamerNewGames = 0;

                            while (gamerNewGames < gamesNeeded && start < 500)
                            {
                                // No startTime filter - get all historical games
                                var matchHistory = await riotApiClient.GetMatchHistoryAsync(puuId, start, pageSize, null, ct);

                                if (matchHistory.Count == 0)
                                    break;

                                foreach (var match in matchHistory)
                                {
                                    if (existingSet.Contains(match.MatchId))
                                        continue;

                                    await matchRepo.AddMatchIfNotExistsAsync(match);
                                    var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId, ct);
                                    await ProcessMatch(match, matchInfoJson, matchRepo, participantRepo, gamer, gamerRepo);

                                    existingSet.Add(match.MatchId);
                                    gamerNewGames++;

                                    if (gamerNewGames >= gamesNeeded)
                                        break;
                                }

                                if (matchHistory.Count < pageSize)
                                    break;

                                start += pageSize;
                            }

                            totalNewGames += gamerNewGames;
                            gamesNeeded -= gamerNewGames;
                            Console.WriteLine($"Backfilled {gamerNewGames} games for {gamer.GamerName}");
                        }
                    }

                    // Recalculate final total
                    int finalTotal = 0;
                    foreach (var puuId in distinctPuuIds)
                    {
                        finalTotal += await participantRepo.GetMatchesCountByPuuIdAsync(puuId);
                    }

                    return Results.Ok(new RefreshGamesResponse(
                        TotalGames: finalTotal,
                        NewGamesAdded: totalNewGames,
                        Message: totalNewGames > 0
                            ? $"Successfully added {totalNewGames} new games"
                            : "No new games found"
                    ));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest(ex is ArgumentException
                        ? "Invalid argument when refreshing games"
                        : "Invalid operation when refreshing games");
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    return Results.BadRequest("Error when refreshing games");
                }
            });
        }

        private async Task ProcessMatch(
            LolMatch match,
            JsonDocument matchInfo,
            LolMatchRepository matchRepo,
            LolMatchParticipantRepository participantRepo,
            Gamer gamer,
            GamerRepository gamerRepo)
        {
            // Map and update match entity
            MapToLolMatchEntity(matchInfo, match);
            await matchRepo.UpdateMatchAsync(match);

            // Map and add participants
            var participants = MapToParticipantEntity(matchInfo, match.MatchId);
            foreach (var participant in participants)
                await participantRepo.AddParticipantIfNotExistsAsync(participant);

            // Update gamer's LastChecked if needed
            if (gamer.LastChecked == DateTime.MinValue || gamer.LastChecked < match.GameEndTimestamp)
            {
                gamer.LastChecked = match.GameEndTimestamp;
                await gamerRepo.UpdateGamerAsync(gamer);
            }
        }

        private void MapToLolMatchEntity(JsonDocument matchInfo, LolMatch match)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp")
                            ?? GetEpochMilliseconds(infoElement, "gameCreation")
                            ?? 0L;

                match.GameEndTimestamp = endMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(endMs).UtcDateTime
                    : DateTime.MinValue;

                if (infoElement.TryGetProperty("gameMode", out var gameModeElement) &&
                    gameModeElement.ValueKind == JsonValueKind.String)
                {
                    match.GameMode = gameModeElement.GetString() ?? string.Empty;
                }

                if (infoElement.TryGetProperty("gameDuration", out var durationElement) &&
                    durationElement.ValueKind == JsonValueKind.Number)
                {
                    match.DurationSeconds = durationElement.GetInt64();
                }

                match.InfoFetched = true;
            }
        }

        private static long? GetEpochMilliseconds(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var el))
                return null;

            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetInt64(),
                JsonValueKind.String => long.TryParse(el.GetString(), out var v) ? v : null,
                _ => null
            };
        }

        private IList<LolMatchParticipant> MapToParticipantEntity(JsonDocument matchInfo, string matchId)
        {
            var list = new List<LolMatchParticipant>();

            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("participants", out var participantsElement))
            {
                foreach (var participant in participantsElement.EnumerateArray())
                {
                    try
                    {
                        var participantEntity = new LolMatchParticipant
                        {
                            MatchId = matchId,
                            PuuId = GetString(participant, "puuid"),
                            TeamId = GetInt(participant, "teamId"),
                            Lane = GetString(participant, "lane"),
                            TeamPosition = GetString(participant, "teamPosition"),
                            ChampionId = GetInt(participant, "championId"),
                            ChampionName = GetString(participant, "championName"),
                            Win = GetBool(participant, "win"),
                            Role = GetString(participant, "role"),
                            Kills = GetInt(participant, "kills"),
                            Deaths = GetInt(participant, "deaths"),
                            Assists = GetInt(participant, "assists"),
                            DoubleKills = GetInt(participant, "doubleKills"),
                            TripleKills = GetInt(participant, "tripleKills"),
                            QuadraKills = GetInt(participant, "quadraKills"),
                            PentaKills = GetInt(participant, "pentaKills"),
                            GoldEarned = GetInt(participant, "goldEarned"),
                            TimeBeingDeadSeconds = GetInt(participant, "totalTimeSpentDead"),
                            CreepScore = GetInt(participant, "totalMinionsKilled") + GetInt(participant, "neutralMinionsKilled")
                        };
                        list.Add(participantEntity);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Skipping participant due to error: {ex.Message}");
                    }
                }
            }

            return list;
        }

        private static string GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? string.Empty
                : string.Empty;

        private static int GetInt(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetInt32()
                : 0;

        private static bool GetBool(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False) &&
            prop.GetBoolean();
    }

    public record RefreshGamesResponse(int TotalGames, int NewGamesAdded, string Message);
}

