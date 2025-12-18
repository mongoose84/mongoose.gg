using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External
{
    public class MatchHistorySyncJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _jobLock = new(1, 1); // Only allow 1 execution at a time

        public MatchHistorySyncJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = DateTime.UtcNow.AddDays(1); // Run daily
                var delay = nextRun - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                await RunJobAsync(stoppingToken);
            }
            throw new InvalidOperationException("ExecuteAsync has been cancelled.");
        }

        public async Task RunJobAsync(CancellationToken ct = default)
        {
            if (!await _jobLock.WaitAsync(0, ct))
            {
                Console.WriteLine("RiotRateLimitedJob is already running. Skipping this execution.");
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var riotApiClient = scope.ServiceProvider.GetRequiredService<IRiotApiClient>();
                var gamerRepository = scope.ServiceProvider.GetRequiredService<GamerRepository>();
                var matchRepository = scope.ServiceProvider.GetRequiredService<LolMatchRepository>();
                var participantRepository = scope.ServiceProvider.GetRequiredService<LolMatchParticipantRepository>();

                Console.WriteLine("RiotRateLimitedJob started.");
                var gamers = await gamerRepository.GetAllGamersAsync();
                Console.WriteLine($"Found {gamers.Count} gamers.");

                // Fetch only NEW match IDs incrementally
                var newMatches = await GetNewMatchHistoryFromRiotApi(gamers, riotApiClient, participantRepository, ct);
                Console.WriteLine($"Fetched {newMatches.Count} NEW matches.");

                if (newMatches.Count > 0)
                {
                    await AddMatchHistoryToDb(newMatches, matchRepository, ct);
                    Console.WriteLine("New match history added to DB.");

                    // Fetch details only for new matches
                    await AddMatchInfoToDb(newMatches, gamers, riotApiClient, participantRepository, matchRepository, gamerRepository, ct);
                }

                Console.WriteLine("RiotRateLimitedJob completed.");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"Error in RiotRateLimitedJob: {ex.Message}");
            }
            finally
            {
                _jobLock.Release();
            }
        }

        private async Task<IList<LolMatch>> GetNewMatchHistoryFromRiotApi(
            IList<Gamer> gamers,
            IRiotApiClient riotApiClient,
            LolMatchParticipantRepository participantRepository,
            CancellationToken ct)
        {
            var allNewMatches = new List<LolMatch>();

            foreach (var gamer in gamers)
            {
                // Get existing match IDs for this gamer
                Console.WriteLine($"Fetching match history for gamer: {gamer.GamerName}");
                var existingMatchIds = await participantRepository.GetMatchIdsForPuuidAsync(gamer.Puuid);
                var existingSet = new HashSet<string>(existingMatchIds);

                int start = 0;
                const int pageSize = 20; // Smaller pages
                bool foundExisting = false;
                int newMatchesForGamer = 0; // Track count per gamer

                // Use startTime filter if we have LastChecked
                long? startTime = null;
                if (gamer.LastChecked != DateTime.MinValue)
                {
                    startTime = new DateTimeOffset(gamer.LastChecked).ToUnixTimeSeconds();
                }

                Console.WriteLine("Getting match history for gamer: " + gamer.GamerName);

                while (!foundExisting && start < 100) // Safety limit
                {
                    var matchHistory = await riotApiClient.GetMatchHistoryAsync(gamer.Puuid, start, pageSize, startTime);

                    if (matchHistory.Count == 0)
                        break; // No more matches

                    foreach (var match in matchHistory)
                    {
                        if (existingSet.Contains(match.MatchId))
                        {
                            // Found a match we already have - stop paging for this gamer
                            foundExisting = true;
                            Console.WriteLine($"Found existing match {match.MatchId} for {gamer.GamerName}, stopping pagination.");
                            break;
                        }

                        allNewMatches.Add(match);
                        newMatchesForGamer++;
                    }

                    if (matchHistory.Count < pageSize)
                        break; // Last page

                    start += pageSize;
                }

                Console.WriteLine($"Found {newMatchesForGamer} new matches for {gamer.GamerName}");
            }

            return allNewMatches;
        }

        private async Task AddMatchHistoryToDb(IList<LolMatch> matchHistory, LolMatchRepository matchRepository, CancellationToken ct)
        {
            foreach (var match in matchHistory)
            {
                try
                {
                    await matchRepository.AddMatchIfNotExistsAsync(match);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error adding match history to DB: {ex.Message}");
                }
            }
        }

        private async Task AddMatchInfoToDb(IList<LolMatch> matches,
                        IList<Gamer> gamers,
                        IRiotApiClient riotApiClient,
                        LolMatchParticipantRepository participantRepository,
                        LolMatchRepository matchRepository,
                        GamerRepository gamerRepository,
                        CancellationToken ct)
        {
            var participantsAdded = 0;
            foreach (var match in matches)
            {
                try
                {
                    // Fetch match info from Riot API
                    var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId);
                    
                    // Map and update match entity and add to database
                    MapToLolMatchEntity(matchInfoJson, match);
                    await matchRepository.UpdateMatchAsync(match);

                    // Map and add participants to database
                    var participants = MapToParticipantEntity(matchInfoJson, match.MatchId);
                    participantsAdded += participants.Count;
                    foreach (var participant in participants)
                        await participantRepository.AddParticipantIfNotExistsAsync(participant);

                    // Update that a gamers info has been updated
                    var gamer = gamers.FirstOrDefault(g => g.Puuid == match.Puuid);
                    if (gamer != null && (gamer.LastChecked == DateTime.MinValue || gamer.LastChecked < match.GameEndTimestamp))
                    {
                        gamer.LastChecked = match.GameEndTimestamp;
                        await gamerRepository.UpdateGamerAsync(gamer);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error adding match info to DB for match {match.MatchId}: {ex.Message}");
                }
            }
            Console.WriteLine($"{participantsAdded} match participants added to DB.");
        }

        private string GetGameMode(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("gameMode", out var gameModeElement))
            {
                if (gameModeElement.ValueKind == JsonValueKind.String)
                    return gameModeElement.GetString() ?? string.Empty;

                // fallback if API changes type unexpectedly
                return gameModeElement.ToString();
            }
            return string.Empty;
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

        private void MapToLolMatchEntity(JsonDocument matchInfo, LolMatch match)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                // gameEndTimestamp is epoch ms; fall back to gameCreation if needed
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp")
                            ?? GetEpochMilliseconds(infoElement, "gameCreation")
                            ?? 0L;

                if (endMs > 0)
                    match.GameEndTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(endMs).UtcDateTime;
                else
                    match.GameEndTimestamp = DateTime.MinValue;

                if (!matchInfo.RootElement.TryGetProperty("metadata", out var metadataElement))
                    throw new InvalidOperationException("Missing 'metadata' object in match info.");
                
                // Map match ID and other properties
                match.GameMode = GetGameMode(matchInfo);
                match.InfoFetched = true;
                var gameDuration = Require<long>(infoElement, "gameDuration", e => e.GetInt64(), JsonValueKind.Number);
                match.DurationSeconds = gameDuration;
            }
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
                        var puuid = Require<string>(participant, "puuid", e => e.GetString()!, JsonValueKind.String);
                        var teamId = Require<int>(participant, "teamId", e => e.GetInt32(), JsonValueKind.Number);
                        var championId = Require<int>(participant, "championId", e => e.GetInt32(), JsonValueKind.Number);
                        var lane = Require<string>(participant, "lane", e => e.GetString()!, JsonValueKind.String);
                        var teamPosition = Require<string>(participant, "teamPosition", e => e.GetString()!, JsonValueKind.String);
                        var championName = Require<string>(participant, "championName", e => e.GetString()!, JsonValueKind.String);
                        var win = Require<bool>(participant, "win", e => e.GetBoolean(), JsonValueKind.True, JsonValueKind.False);
                        var role = Require<string>(participant, "role", e => e.GetString()!, JsonValueKind.String);
                        var kills = Require<int>(participant, "kills", e => e.GetInt32(), JsonValueKind.Number);
                        var deaths = Require<int>(participant, "deaths", e => e.GetInt32(), JsonValueKind.Number);
                        var assists = Require<int>(participant, "assists", e => e.GetInt32(), JsonValueKind.Number);
                        var doubleKills = Require<int>(participant, "doubleKills", e => e.GetInt32(), JsonValueKind.Number);
                        var tripleKills = Require<int>(participant, "tripleKills", e => e.GetInt32(), JsonValueKind.Number);
                        var quadraKills = Require<int>(participant, "quadraKills", e => e.GetInt32(), JsonValueKind.Number);
                        var pentaKills = Require<int>(participant, "pentaKills", e => e.GetInt32(), JsonValueKind.Number);
                        var goldEarned = Require<int>(participant, "goldEarned", e => e.GetInt32(), JsonValueKind.Number);
                        var creepScore = Require<int>(participant, "totalMinionsKilled", e => e.GetInt32(), JsonValueKind.Number);

                        var participantEntity = new LolMatchParticipant
                        {
                            MatchId = matchId,
                            Puuid = puuid,
                            TeamId = teamId,
                            Lane = lane,
                            TeamPosition = teamPosition,
                            ChampionId = championId,
                            ChampionName = championName,
                            Win = win,
                            Role = role,
                            Kills = kills,
                            Deaths = deaths,
                            Assists = assists,
                            DoubleKills = doubleKills,
                            TripleKills = tripleKills,
                            QuadraKills = quadraKills,
                            PentaKills = pentaKills,
                            GoldEarned = goldEarned,
                            CreepScore = creepScore
                        };
                        list.Add(participantEntity);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Skipping participant due to error: {ex.Message}");
                        continue;
                    }
                }

                return list;
            }
             throw new InvalidOperationException("Missing or invalid participants data in match info.");
        }

        private static T Require<T>(JsonElement element, string propertyName, Func<JsonElement, T> read, params JsonValueKind[] allowedKinds)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                throw new InvalidOperationException($"Missing required '{propertyName}' property.");

            if (allowedKinds != null && allowedKinds.Length > 0)
            {
                var kind = property.ValueKind;
                var ok = allowedKinds.Any(k => k == kind);
                if (!ok)
                    throw new InvalidOperationException($"Invalid kind for '{propertyName}': {kind}");
            }

            try
            {
                return read(property);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read '{propertyName}' as {typeof(T).Name}. Error: {ex.Message}");
            }
        }
    }
}