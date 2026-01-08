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
                // v2 repositories
                var v2Matches = scope.ServiceProvider.GetRequiredService<RiotProxy.Infrastructure.External.Database.Repositories.V2.V2MatchesRepository>();
                var v2Participants = scope.ServiceProvider.GetRequiredService<RiotProxy.Infrastructure.External.Database.Repositories.V2.V2ParticipantsRepository>();

                Console.WriteLine("MatchHistorySyncJob started.");
                var gamers = await gamerRepository.GetAllGamersAsync();
                Console.WriteLine($"Found {gamers.Count} gamers.");

                // Fetch only NEW match IDs incrementally
                var newMatches = await GetNewMatchHistoryFromRiotApi(gamers, riotApiClient, participantRepository, v2Participants, ct);
                // De-duplicate by MatchId to avoid processing the same match multiple times across gamers
                newMatches = newMatches
                    .GroupBy(m => m.MatchId)
                    .Select(g => g.First())
                    .ToList();
                Console.WriteLine($"Fetched {newMatches.Count} NEW unique matches.");

                if (newMatches.Count > 0)
                {
                    await AddMatchHistoryToDb(newMatches, matchRepository, ct);
                    Console.WriteLine("New match history added to DB.");

                    // Fetch details only for new matches
                    await AddMatchInfoToDb(newMatches, gamers, riotApiClient, participantRepository, matchRepository, gamerRepository, v2Matches, v2Participants, ct);
                }

                Console.WriteLine("MatchHistorySyncJob completed.");
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
            RiotProxy.Infrastructure.External.Database.Repositories.V2.V2ParticipantsRepository v2ParticipantRepository,
            CancellationToken ct)
        {
            var allNewMatches = new List<LolMatch>();
            // Track unique matchIds across ALL gamers in this run to avoid duplicates
            var seenMatchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var gamer in gamers)
            {
                // Get existing match IDs for this gamer
                Console.WriteLine($"Fetching match history for gamer: {gamer.GamerName}");
                var existingMatchIds = await participantRepository.GetMatchIdsForPuuidAsync(gamer.Puuid);
                var existingSet = new HashSet<string>(existingMatchIds);
                var v2ExistingSet = await v2ParticipantRepository.GetMatchIdsForPuuidAsync(gamer.Puuid);

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
                        var existsV1 = existingSet.Contains(match.MatchId);
                        var existsV2 = v2ExistingSet.Contains(match.MatchId);
                        if (existsV1 && existsV2)
                        {
                            // Found a match present in both v1 and v2 - safe to stop paging for this gamer
                            foundExisting = true;
                            Console.WriteLine($"Found existing match {match.MatchId} for {gamer.GamerName} in both v1 and v2, stopping pagination.");
                            break;
                        }

                        if (seenMatchIds.Add(match.MatchId))
                        {
                            allNewMatches.Add(match);
                            newMatchesForGamer++;
                        }
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
                RiotProxy.Infrastructure.External.Database.Repositories.V2.V2MatchesRepository v2Matches,
                RiotProxy.Infrastructure.External.Database.Repositories.V2.V2ParticipantsRepository v2Participants,
                        CancellationToken ct)
        {
            var participantsAdded = 0;
            foreach (var match in matches)
            {
                try
                {
                    // Fetch match info from Riot API
                    var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId);
                    
                    // Map and update match entity and add to database (v1)
                    MapToLolMatchEntity(matchInfoJson, match);
                    await matchRepository.UpdateMatchAsync(match);

                    // Map and add participants to database (v1)
                    var participants = MapToParticipantEntity(matchInfoJson, match.MatchId);
                    participantsAdded += participants.Count;
                    foreach (var participant in participants)
                        await participantRepository.AddParticipantIfNotExistsAsync(participant);

                    // Map and upsert into v2 tables
                    var v2Match = MapToV2Match(matchInfoJson, match.MatchId);
                    await v2Matches.UpsertAsync(v2Match);
                    var v2Parts = MapToV2Participants(matchInfoJson, match.MatchId);
                    foreach (var p in v2Parts)
                        await v2Participants.InsertAsync(p);

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

        private int? GetQueueId(JsonElement infoElement)
        {
            if (infoElement.TryGetProperty("queueId", out var queueIdElement))
            {
                if (queueIdElement.ValueKind == JsonValueKind.Number)
                    return queueIdElement.GetInt32();

                // Handle string fallback if API returns it as string
                if (queueIdElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(queueIdElement.GetString(), out var parsedId))
                    return parsedId;
            }
            return null;
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
                match.QueueId = GetQueueId(infoElement);
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
                        var timeBeingDeadSeconds = Require<int>(participant, "totalTimeSpentDead", e => e.GetInt32(), JsonValueKind.Number);
                        var minions = Require<int>(participant, "totalMinionsKilled", e => e.GetInt32(), JsonValueKind.Number);
                        var jungleMinions = Require<int>(participant, "neutralMinionsKilled", e => e.GetInt32(), JsonValueKind.Number);
                        var creepScore = minions + jungleMinions;
                        
                        var participantEntity = new LolMatchParticipant
                        {
                            MatchId = matchId,
                            PuuId = puuid,
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
                            TimeBeingDeadSeconds = timeBeingDeadSeconds,
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

        private static RiotProxy.External.Domain.Entities.V2.V2Match MapToV2Match(JsonDocument matchInfo, string matchId)
        {
            var v2 = new RiotProxy.External.Domain.Entities.V2.V2Match
            {
                MatchId = matchId,
                CreatedAt = DateTime.UtcNow
            };

            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                // queue id
                if (infoElement.TryGetProperty("queueId", out var qEl) && qEl.ValueKind == JsonValueKind.Number)
                    v2.QueueId = qEl.GetInt32();

                // duration (seconds)
                if (infoElement.TryGetProperty("gameDuration", out var durEl) && durEl.ValueKind == JsonValueKind.Number)
                    v2.GameDurationSec = durEl.GetInt32();

                // start time (epoch ms -> seconds)
                long startMs = 0;
                if (infoElement.TryGetProperty("gameStartTimestamp", out var startEl) && startEl.ValueKind == JsonValueKind.Number)
                    startMs = startEl.GetInt64();
                else if (infoElement.TryGetProperty("gameCreation", out var creationEl) && creationEl.ValueKind == JsonValueKind.Number)
                    startMs = creationEl.GetInt64();
                v2.GameStartTime = startMs > 0 ? startMs / 1000 : 0;

                // patch version (from gameVersion like "13.7.456") -> "13.7"
                if (infoElement.TryGetProperty("gameVersion", out var verEl) && verEl.ValueKind == JsonValueKind.String)
                {
                    var ver = verEl.GetString() ?? string.Empty;
                    var parts = ver.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    v2.PatchVersion = parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : ver;
                    }
            }
            return v2;
        }

        private static IList<RiotProxy.External.Domain.Entities.V2.V2Participant> MapToV2Participants(JsonDocument matchInfo, string matchId)
        {
            var list = new List<RiotProxy.External.Domain.Entities.V2.V2Participant>();
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
                        var role = Require<string>(participant, "role", e => e.GetString()!, JsonValueKind.String);
                        var championName = Require<string>(participant, "championName", e => e.GetString()!, JsonValueKind.String);
                        var win = Require<bool>(participant, "win", e => e.GetBoolean(), JsonValueKind.True, JsonValueKind.False);
                        var kills = Require<int>(participant, "kills", e => e.GetInt32(), JsonValueKind.Number);
                        var deaths = Require<int>(participant, "deaths", e => e.GetInt32(), JsonValueKind.Number);
                        var assists = Require<int>(participant, "assists", e => e.GetInt32(), JsonValueKind.Number);
                        var minions = Require<int>(participant, "totalMinionsKilled", e => e.GetInt32(), JsonValueKind.Number);
                        var jungleMinions = Require<int>(participant, "neutralMinionsKilled", e => e.GetInt32(), JsonValueKind.Number);
                        var creepScore = minions + jungleMinions;
                        var goldEarned = Require<int>(participant, "goldEarned", e => e.GetInt32(), JsonValueKind.Number);
                        var timeDead = Require<int>(participant, "totalTimeSpentDead", e => e.GetInt32(), JsonValueKind.Number);

                        list.Add(new RiotProxy.External.Domain.Entities.V2.V2Participant
                        {
                            MatchId = matchId,
                            Puuid = puuid,
                            TeamId = teamId,
                            Role = role,
                            Lane = lane,
                            ChampionId = championId,
                            ChampionName = championName,
                            Win = win,
                            Kills = kills,
                            Deaths = deaths,
                            Assists = assists,
                            CreepScore = creepScore,
                            GoldEarned = goldEarned,
                            TimeDeadSec = timeDead,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Skipping v2 participant due to error: {ex.Message}");
                        continue;
                    }
                }
            }
            return list;
        }
    }
    }