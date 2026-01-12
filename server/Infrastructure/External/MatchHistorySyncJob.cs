using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using System.Text.Json;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;

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
            // Dev/testing: allow a one-off immediate run via env var
            var runNow = Environment.GetEnvironmentVariable("PULSE_RUN_JOB_NOW");
            if (string.Equals(runNow, "1", StringComparison.Ordinal))
            {
                await RunJobAsync(stoppingToken);
            }

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

                Console.WriteLine("MatchHistorySyncJob started.");
                var gamers = await gamerRepository.GetAllGamersAsync();
                Console.WriteLine($"Found {gamers.Count} gamers.");

                // Fetch only NEW match IDs incrementally (v1 only)
                var newMatches = await GetNewMatchHistoryFromRiotApi(gamers, riotApiClient, participantRepository, ct);
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

                    // Fetch details only for new matches (v1 only)
                    await AddMatchInfoToDb(
                        newMatches,
                        gamers,
                        riotApiClient,
                        participantRepository,
                        matchRepository,
                        gamerRepository,
                        ct);
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
            CancellationToken ct)
        {
            var allNewMatches = new List<LolMatch>();
            // Track unique matchIds across ALL gamers in this run to avoid duplicates
            var seenMatchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var gamer in gamers)
            {
                // Get existing match IDs for this gamer (v1 only)
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
                            // Found a match already in v1 - safe to stop paging for this gamer
                            foundExisting = true;
                            Console.WriteLine($"Found existing match {match.MatchId} for {gamer.GamerName}, stopping pagination.");
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
                        CancellationToken ct)
        {
            var participantsAdded = 0;
            foreach (var match in matches)
            {
                try
                {
                    // Fetch match info from Riot API
                    var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId);

                    // Map and update match entity and add to database (v1 only)
                    MapToLolMatchEntity(matchInfoJson, match);
                    await matchRepository.UpdateMatchAsync(match);

                    // Map and add participants to database (v1 only)
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

        /// <summary>
        /// Maps Riot API match info to a V2Match entity.
        /// </summary>
        public static RiotProxy.External.Domain.Entities.V2.V2Match MapToV2Match(JsonDocument matchInfo, string matchId)
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

                    // season code derived from major patch version (e.g., "14.3.1" -> "S14")
                    if (parts.Length > 0 && int.TryParse(parts[0], out var majorVersion))
                    {
                        v2.SeasonCode = $"S{majorVersion}";
                    }
                }
            }
            return v2;
        }

        /// <summary>
        /// Maps Riot API match info to a list of V2Participant entities.
        /// </summary>
        public static IList<RiotProxy.External.Domain.Entities.V2.V2Participant> MapToV2Participants(JsonDocument matchInfo, string matchId)
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

        private static int GetMinuteFromTimestamp(long timestampMs)
            => (int)Math.Round(timestampMs / 60000.0);

        /// <summary>
        /// Persists timeline-derived data (checkpoints, metrics, objectives) to V2 tables.
        /// </summary>
        public static async Task PersistV2TimelineDerivedAsync(
            string matchId,
            JsonDocument matchInfo,
            JsonDocument timeline,
            V2ParticipantsRepository v2Participants,
            V2ParticipantCheckpointsRepository v2Checkpoints,
            V2ParticipantMetricsRepository v2PartMetrics,
            V2TeamObjectivesRepository v2TeamObjectives,
            V2ParticipantObjectivesRepository v2PartObjectives,
            V2TeamMatchMetricsRepository v2TeamMetrics,
            V2DuoMetricsRepository v2DuoMetrics,
            CancellationToken ct)
        {
            var participants = await v2Participants.GetByMatchAsync(matchId);
            var puuidToV2Id = participants.ToDictionary(p => p.Puuid, p => p.Id, StringComparer.OrdinalIgnoreCase);

            // Build maps from match info: puuid -> timeline participantId (1..10), teamId, lane/role
            var puuidToTimelineId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var timelineIdToTeam = new Dictionary<int, int>();
            var puuidToLane = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var teamToPuuids = new Dictionary<int, List<string>> { [100] = new(), [200] = new() };
            var teamKills = new Dictionary<int, int> { [100] = 0, [200] = 0 };
            var teamDmgToChamps = new Dictionary<int, long> { [100] = 0, [200] = 0 };
            var puuidKills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var puuidAssists = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var puuidDamageToChamps = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var puuidDamageTaken = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var puuidDamageMit = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var puuidVision = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (matchInfo.RootElement.TryGetProperty("info", out var infoEl) &&
                infoEl.TryGetProperty("participants", out var partsEl))
            {
                foreach (var p in partsEl.EnumerateArray())
                {
                    var puuid = p.GetProperty("puuid").GetString()!;
                    var tlId = p.TryGetProperty("participantId", out var pid) && pid.ValueKind == JsonValueKind.Number ? pid.GetInt32() : 0;
                    var teamId = p.TryGetProperty("teamId", out var tEl) && tEl.ValueKind == JsonValueKind.Number ? tEl.GetInt32() : 0;
                    var lane = p.TryGetProperty("teamPosition", out var pos) && pos.ValueKind == JsonValueKind.String ? pos.GetString() : (p.TryGetProperty("lane", out var ln) ? ln.GetString() : null);
                    var kills = p.TryGetProperty("kills", out var kEl) && kEl.ValueKind == JsonValueKind.Number ? kEl.GetInt32() : 0;
                    var assists = p.TryGetProperty("assists", out var aEl) && aEl.ValueKind == JsonValueKind.Number ? aEl.GetInt32() : 0;
                    var dmgToChamps = p.TryGetProperty("totalDamageDealtToChampions", out var d1) && d1.ValueKind == JsonValueKind.Number ? d1.GetInt64() : 0;
                    var dmgTaken = p.TryGetProperty("totalDamageTaken", out var d2) && d2.ValueKind == JsonValueKind.Number ? d2.GetInt32() : 0;
                    var dmgMit = p.TryGetProperty("damageSelfMitigated", out var d3) && d3.ValueKind == JsonValueKind.Number ? d3.GetInt32() : 0;
                    var vision = p.TryGetProperty("visionScore", out var vEl) && vEl.ValueKind == JsonValueKind.Number ? vEl.GetInt32() : 0;

                    puuidToTimelineId[puuid] = tlId;
                    timelineIdToTeam[tlId] = teamId;
                    puuidToLane[puuid] = lane;
                    if (!teamToPuuids.ContainsKey(teamId)) teamToPuuids[teamId] = new();
                    teamToPuuids[teamId].Add(puuid);
                    puuidKills[puuid] = kills;
                    puuidAssists[puuid] = assists;
                    puuidDamageToChamps[puuid] = dmgToChamps;
                    puuidDamageTaken[puuid] = dmgTaken;
                    puuidDamageMit[puuid] = dmgMit;
                    puuidVision[puuid] = vision;
                    teamKills[teamId] += kills;
                    teamDmgToChamps[teamId] += dmgToChamps;
                }
            }

            // Duration minutes for vision per min
            var durationSec = infoEl.TryGetProperty("gameDuration", out var dur) && dur.ValueKind == JsonValueKind.Number ? dur.GetInt32() : 0;
            var durationMin = Math.Max(1, durationSec / 60.0m);

            // Prepare death buckets and first participation minute per puuid
            var deathBuckets = new Dictionary<string, (int pre10, int m10_20, int m20_30, int m30p, int? firstDeath)>(StringComparer.OrdinalIgnoreCase);
            var firstKPMinute = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pu in puuidToV2Id.Keys)
            {
                deathBuckets[pu] = (0, 0, 0, 0, null);
                firstKPMinute[pu] = null;
            }

            // Team objectives counters and participant objective counters
            var teamObj = new Dictionary<int, (int drake, int herald, int baron, int towers)> { [100] = (0,0,0,0), [200] = (0,0,0,0) };
            var partObj = puuidToV2Id.Keys.ToDictionary(k => k, _ => (drake:0, herald:0, baron:0, towers:0), StringComparer.OrdinalIgnoreCase);

            // Gold by team across frames for leads
            var frameTeamGold = new List<(int minute, int gold100, int gold200)>();

            if (timeline.RootElement.TryGetProperty("info", out var tInfo) &&
                tInfo.TryGetProperty("frames", out var frames))
            {
                foreach (var frame in frames.EnumerateArray())
                {
                    var minute = frame.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.Number ? GetMinuteFromTimestamp(ts.GetInt64()) : 0;

                    // Aggregate team gold totals and checkpoints at 10/15/20/25/30
                    var gold100 = 0; var gold200 = 0;
                    if (frame.TryGetProperty("participantFrames", out var pf))
                    {
                        var frameCheckpoints = new List<RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint>();

                        foreach (var kv in pf.EnumerateObject())
                        {
                            var tlId = int.TryParse(kv.Name, out var idVal) ? idVal : 0;
                            var team = timelineIdToTeam.TryGetValue(tlId, out var tId) ? tId : 0;
                            var f = kv.Value;
                            var gold = f.TryGetProperty("totalGold", out var gEl) && gEl.ValueKind == JsonValueKind.Number ? gEl.GetInt32() : 0;
                            var xp = f.TryGetProperty("xp", out var xEl) && xEl.ValueKind == JsonValueKind.Number ? xEl.GetInt32() : 0;
                            var cs = 0;
                            if (f.TryGetProperty("minionsKilled", out var mk) && mk.ValueKind == JsonValueKind.Number) cs += mk.GetInt32();
                            if (f.TryGetProperty("jungleMinionsKilled", out var jk) && jk.ValueKind == JsonValueKind.Number) cs += jk.GetInt32();

                            if (team == 100) gold100 += gold; else if (team == 200) gold200 += gold;

                            // Store checkpoints at target marks
                            if (minute == 10 || minute == 15 || minute == 20 || minute == 25 || minute == 30)
                            {
                                // Map frame tlId -> puuid -> v2 participant id
                                var puuid = puuidToTimelineId.FirstOrDefault(p => p.Value == tlId).Key;
                                if (!string.IsNullOrEmpty(puuid) && puuidToV2Id.TryGetValue(puuid, out var v2Pid))
                                {
                                    int? diffGold = null; int? diffCs = null; bool? ahead = null;
                                    var lane = puuidToLane.TryGetValue(puuid, out var lnVal) ? lnVal : null;
                                    if (!string.IsNullOrEmpty(lane))
                                    {
                                        // find opponent in same lane on opposing team
                                        var myTeam = team;
                                        var oppTeam = myTeam == 100 ? 200 : 100;
                                        var opp = teamToPuuids[oppTeam].FirstOrDefault(pu => string.Equals(puuidToLane.GetValueOrDefault(pu), lane, StringComparison.OrdinalIgnoreCase));
                                        if (!string.IsNullOrEmpty(opp))
                                        {
                                            // find opponent tl frame to compute diffs
                                            var oppTl = puuidToTimelineId.GetValueOrDefault(opp);
                                            if (pf.TryGetProperty(oppTl.ToString(), out var oppF))
                                            {
                                                var oppGold = oppF.TryGetProperty("totalGold", out var og) && og.ValueKind == JsonValueKind.Number ? og.GetInt32() : 0;
                                                var oppCs = 0;
                                                if (oppF.TryGetProperty("minionsKilled", out var omk) && omk.ValueKind == JsonValueKind.Number) oppCs += omk.GetInt32();
                                                if (oppF.TryGetProperty("jungleMinionsKilled", out var ojk) && ojk.ValueKind == JsonValueKind.Number) oppCs += ojk.GetInt32();
                                                diffGold = gold - oppGold;
                                                diffCs = cs - oppCs;
                                                ahead = diffGold > 0;
                                            }
                                        }
                                    }

                                    frameCheckpoints.Add(new RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint
                                    {
        ParticipantId = v2Pid,
        MinuteMark = minute,
        Gold = gold,
        Cs = cs,
        Xp = xp,
        GoldDiffVsLane = diffGold,
        CsDiffVsLane = diffCs,
        IsAhead = ahead,
        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }

                        if (frameCheckpoints.Count > 0)
                        {
                            await v2Checkpoints.UpsertBatchAsync(frameCheckpoints);
                        }
                    }
                    frameTeamGold.Add((minute, gold100, gold200));

                    // Parse events for kills/deaths/objectives
                    if (frame.TryGetProperty("events", out var evs))
                    {
                        foreach (var ev in evs.EnumerateArray())
                        {
                            var type = ev.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                            var evTsMs = ev.TryGetProperty("timestamp", out var eTs) && eTs.ValueKind == JsonValueKind.Number ? eTs.GetInt64() : 0;
                            var m = GetMinuteFromTimestamp(evTsMs);

                            if (type == "CHAMPION_KILL")
                            {
                                var victimId = ev.TryGetProperty("victimId", out var vId) && vId.ValueKind == JsonValueKind.Number ? vId.GetInt32() : 0;
                                var killerId = ev.TryGetProperty("killerId", out var kId) && kId.ValueKind == JsonValueKind.Number ? kId.GetInt32() : 0;
                                var assists = new List<int>();
                                if (ev.TryGetProperty("assistingParticipantIds", out var aid) && aid.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var a in aid.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number))
                                    {
                                        assists.Add(a.GetInt32());
                                    }
                                }
                                // death bucket for victim
                                var victimPuuid = puuidToTimelineId.FirstOrDefault(p => p.Value == victimId).Key;
                                if (!string.IsNullOrEmpty(victimPuuid))
                                {
                                    var b = deathBuckets[victimPuuid];
                                    if (m < 10) b.pre10++;
                                    else if (m < 20) b.m10_20++;
                                    else if (m < 30) b.m20_30++;
                                    else b.m30p++;
                                    b.firstDeath ??= m;
                                    deathBuckets[victimPuuid] = b;
                                }
                                // first kill participation for killer/assists
                                var killerPuuid = puuidToTimelineId.FirstOrDefault(p => p.Value == killerId).Key;
                                if (!string.IsNullOrEmpty(killerPuuid) && firstKPMinute[killerPuuid] is null) firstKPMinute[killerPuuid] = m;
                                foreach (var aidId in assists)
                                {
                                    var ap = puuidToTimelineId.FirstOrDefault(p => p.Value == aidId).Key;
                                    if (!string.IsNullOrEmpty(ap) && firstKPMinute[ap] is null) firstKPMinute[ap] = m;
                                }
                            }
                            else if (type == "ELITE_MONSTER_KILL")
                            {
                                var killerId = ev.TryGetProperty("killerId", out var kId) && kId.ValueKind == JsonValueKind.Number ? kId.GetInt32() : 0;
                                var team = timelineIdToTeam.TryGetValue(killerId, out var tId) ? tId : 0;
                                var monster = ev.TryGetProperty("monsterType", out var mt) && mt.ValueKind == JsonValueKind.String ? mt.GetString() : null;
                                if (team != 0 && !string.IsNullOrEmpty(monster))
                                {
                                    if (monster == "DRAGON") teamObj[team] = (teamObj[team].drake + 1, teamObj[team].herald, teamObj[team].baron, teamObj[team].towers);
                                    else if (monster == "RIFTHERALD") teamObj[team] = (teamObj[team].drake, teamObj[team].herald + 1, teamObj[team].baron, teamObj[team].towers);
                                    else if (monster == "BARON_NASHOR") teamObj[team] = (teamObj[team].drake, teamObj[team].herald, teamObj[team].baron + 1, teamObj[team].towers);

                                    // participant objectives (killer + assists)
                                    (int drake, int herald, int baron, int towers) inc =
                                        monster == "DRAGON" ? (1, 0, 0, 0)
                                        : monster == "RIFTHERALD" ? (0, 1, 0, 0)
                                        : monster == "BARON_NASHOR" ? (0, 0, 1, 0)
                                        : (0, 0, 0, 0);
                                    var killerPuuid = puuidToTimelineId.FirstOrDefault(p => p.Value == killerId).Key;
                                    if (!string.IsNullOrEmpty(killerPuuid))
                                    {
                                        var cur = partObj[killerPuuid];
                                        partObj[killerPuuid] = (cur.drake + inc.drake, cur.herald + inc.herald, cur.baron + inc.baron, cur.towers);
                                    }
                                    if (ev.TryGetProperty("assistingParticipantIds", out var aid) && aid.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var a in aid.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number))
                                        {
                                            var ap = puuidToTimelineId.FirstOrDefault(p => p.Value == a.GetInt32()).Key;
                                            if (!string.IsNullOrEmpty(ap))
                                            {
                                                var cur = partObj[ap];
                                                partObj[ap] = (cur.drake + inc.drake, cur.herald + inc.herald, cur.baron + inc.baron, cur.towers);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (type == "BUILDING_KILL")
                            {
                                var bType = ev.TryGetProperty("buildingType", out var bt) && bt.ValueKind == JsonValueKind.String ? bt.GetString() : null;
                                if (bType == "TOWER_BUILDING")
                                {
                                    var killerId = ev.TryGetProperty("killerId", out var kId) && kId.ValueKind == JsonValueKind.Number ? kId.GetInt32() : 0;
                                    var team = timelineIdToTeam.TryGetValue(killerId, out var tId) ? tId : 0;
                                    if (team != 0) teamObj[team] = (teamObj[team].drake, teamObj[team].herald, teamObj[team].baron, teamObj[team].towers + 1);

                                    var killerPuuid = puuidToTimelineId.FirstOrDefault(p => p.Value == killerId).Key;
                                    if (!string.IsNullOrEmpty(killerPuuid))
                                    {
                                        var cur = partObj[killerPuuid];
                                        partObj[killerPuuid] = (cur.drake, cur.herald, cur.baron, cur.towers + 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Determine team win flags (used by team metrics and duo metrics)
            var teamWin = new Dictionary<int, bool> { [100] = false, [200] = false };
            if (matchInfo.RootElement.TryGetProperty("info", out var winInfoForTeams) && winInfoForTeams.TryGetProperty("teams", out var teamsElForWin))
            {
                foreach (var t in teamsElForWin.EnumerateArray())
                {
                    var tId = t.TryGetProperty("teamId", out var idEl) && idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt32() : 0;
                    var w = t.TryGetProperty("win", out var wEl) && (wEl.ValueKind == JsonValueKind.True || wEl.ValueKind == JsonValueKind.False) && wEl.GetBoolean();
                    if (tId != 0) teamWin[tId] = w;
                }
            }

            // Persist team match metrics
            if (frameTeamGold.Count > 0)
            {
                int? g15 = null; int? largest = null; int? swing20 = null; bool? winAhead20 = null;
                var leads = frameTeamGold.Select(f => (f.minute, lead: f.gold100 - f.gold200)).OrderBy(x => x.minute).ToList();
                var byMin = leads.ToDictionary(x => x.minute, x => x.lead);
                if (byMin.TryGetValue(15, out var l15)) g15 = l15;
                if (leads.Count > 0) largest = Math.Max(Math.Abs(leads.Min(x => x.lead)), Math.Abs(leads.Max(x => x.lead)));
                var post20 = leads.Where(x => x.minute >= 20).Select(x => x.lead).ToList();
                if (post20.Count > 0) swing20 = (post20.Max() - post20.Min());
                if (byMin.TryGetValue(20, out var l20))
                {
                    if (l20 > 0) winAhead20 = teamWin.GetValueOrDefault(100);
                    else if (l20 < 0) winAhead20 = teamWin.GetValueOrDefault(200);
                    else winAhead20 = null;
                }

                foreach (var team in new[] { 100, 200 })
                {
                    await v2TeamMetrics.UpsertAsync(new RiotProxy.External.Domain.Entities.V2.V2TeamMatchMetric
                    {
                        MatchId = matchId,
                        TeamId = team,
                        GoldLeadAt15 = team == 100 ? g15 : (g15.HasValue ? -g15 : null),
                        LargestGoldLead = largest,
                        GoldSwingPost20 = swing20,
                        WinWhenAheadAt20 = winAhead20,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Persist team objectives
            foreach (var kv in teamObj)
            {
                await v2TeamObjectives.UpsertAsync(new RiotProxy.External.Domain.Entities.V2.V2TeamObjective
                {
                    MatchId = matchId,
                    TeamId = kv.Key,
                    DragonsTaken = kv.Value.drake,
                    HeraldsTaken = kv.Value.herald,
                    BaronsTaken = kv.Value.baron,
                    TowersTaken = kv.Value.towers,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Persist participant objectives and metrics
            foreach (var pu in puuidToV2Id.Keys)
            {
                var v2Id = puuidToV2Id[pu];
                var teamId = timelineIdToTeam.GetValueOrDefault(puuidToTimelineId.GetValueOrDefault(pu));
                var teamKillCount = Math.Max(1, teamKills.GetValueOrDefault(teamId));
                var killPartPct = (decimal)(puuidKills.GetValueOrDefault(pu) + puuidAssists.GetValueOrDefault(pu)) * 100m / teamKillCount;
                var teamDmg = Math.Max(1L, teamDmgToChamps.GetValueOrDefault(teamId));
                var dmgSharePct = (decimal)puuidDamageToChamps.GetValueOrDefault(pu) * 100m / teamDmg;
                var deaths = deathBuckets[pu];
                var fkp = firstKPMinute[pu];
                var vis = puuidVision.GetValueOrDefault(pu);
                var vpm = Math.Round((decimal)vis / durationMin, 2);

                await v2PartObjectives.UpsertAsync(new RiotProxy.External.Domain.Entities.V2.V2ParticipantObjective
                {
                    ParticipantId = v2Id,
                    DragonsParticipated = partObj[pu].drake,
                    HeraldsParticipated = partObj[pu].herald,
                    BaronsParticipated = partObj[pu].baron,
                    TowersParticipated = partObj[pu].towers,
                    CreatedAt = DateTime.UtcNow
                });

                await v2PartMetrics.UpsertAsync(new RiotProxy.External.Domain.Entities.V2.V2ParticipantMetric
                {
                    ParticipantId = v2Id,
                    KillParticipationPct = Math.Round(killPartPct, 2),
                    DamageSharePct = Math.Round(dmgSharePct, 2),
                    DamageTaken = puuidDamageTaken.GetValueOrDefault(pu),
                    DamageMitigated = puuidDamageMit.GetValueOrDefault(pu),
                    VisionScore = vis,
                    VisionPerMin = vpm,
                    DeathsPre10 = deaths.pre10,
                    Deaths10To20 = deaths.m10_20,
                    Deaths20To30 = deaths.m20_30,
                    Deaths30Plus = deaths.m30p,
                    FirstDeathMinute = deaths.firstDeath,
                    FirstKillParticipationMinute = fkp,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Optional: compute simple duo metrics for BOT+SUP duos if present
            foreach (var team in new[] { 100, 200 })
            {
                var teamPlayers = teamToPuuids.GetValueOrDefault(team);
                if (teamPlayers == null || teamPlayers.Count == 0) continue;
                var adc = teamPlayers.FirstOrDefault(p => string.Equals(puuidToLane.GetValueOrDefault(p), "BOTTOM", StringComparison.OrdinalIgnoreCase));
                var sup = teamPlayers.FirstOrDefault(p => string.Equals(puuidToLane.GetValueOrDefault(p), "UTILITY", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(adc) || string.IsNullOrEmpty(sup)) continue;

                // Get v2 participant IDs for this duo
                if (!puuidToV2Id.TryGetValue(adc, out var adcV2Id) || !puuidToV2Id.TryGetValue(sup, out var supV2Id))
                    continue;

                // Find enemy team and their bot lane duo
                var enemyTeam = team == 100 ? 200 : 100;
                var enemyPlayers = teamToPuuids.GetValueOrDefault(enemyTeam);
                if (enemyPlayers == null || enemyPlayers.Count == 0) continue;
                var enemyAdc = enemyPlayers.FirstOrDefault(p => string.Equals(puuidToLane.GetValueOrDefault(p), "BOTTOM", StringComparison.OrdinalIgnoreCase));
                var enemySup = enemyPlayers.FirstOrDefault(p => string.Equals(puuidToLane.GetValueOrDefault(p), "UTILITY", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(enemyAdc) || string.IsNullOrEmpty(enemySup)) continue;

                if (!puuidToV2Id.TryGetValue(enemyAdc, out var enemyAdcV2Id) || !puuidToV2Id.TryGetValue(enemySup, out var enemySupV2Id))
                    continue;

                // Fetch checkpoints for all four players in a single batch
                var participantIds = new[] { adcV2Id, supV2Id, enemyAdcV2Id, enemySupV2Id };
                var allCheckpoints = await v2Checkpoints.GetByParticipantIdsAsync(participantIds);
                var cpsByParticipant = allCheckpoints
                    .GroupBy(c => c.ParticipantId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                cpsByParticipant.TryGetValue(adcV2Id, out var adcCheckpoints);
                cpsByParticipant.TryGetValue(supV2Id, out var supCheckpoints);
                cpsByParticipant.TryGetValue(enemyAdcV2Id, out var enemyAdcCheckpoints);
                cpsByParticipant.TryGetValue(enemySupV2Id, out var enemySupCheckpoints);

                adcCheckpoints ??= new List<RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint>();
                supCheckpoints ??= new List<RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint>();
                enemyAdcCheckpoints ??= new List<RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint>();
                enemySupCheckpoints ??= new List<RiotProxy.External.Domain.Entities.V2.V2ParticipantCheckpoint>();

                int? eg10 = null; int? eg15 = null; bool? winAhead15 = null;

                // Calculate duo gold difference at 10 minutes
                var adc10 = adcCheckpoints.FirstOrDefault(c => c.MinuteMark == 10);
                var sup10 = supCheckpoints.FirstOrDefault(c => c.MinuteMark == 10);
                var enemyAdc10 = enemyAdcCheckpoints.FirstOrDefault(c => c.MinuteMark == 10);
                var enemySup10 = enemySupCheckpoints.FirstOrDefault(c => c.MinuteMark == 10);
                if (adc10 != null && sup10 != null && enemyAdc10 != null && enemySup10 != null)
                {
                    var duoGold10 = adc10.Gold + sup10.Gold;
                    var enemyDuoGold10 = enemyAdc10.Gold + enemySup10.Gold;
                    eg10 = duoGold10 - enemyDuoGold10;
                }

                // Calculate duo gold difference at 15 minutes
                var adc15 = adcCheckpoints.FirstOrDefault(c => c.MinuteMark == 15);
                var sup15 = supCheckpoints.FirstOrDefault(c => c.MinuteMark == 15);
                var enemyAdc15 = enemyAdcCheckpoints.FirstOrDefault(c => c.MinuteMark == 15);
                var enemySup15 = enemySupCheckpoints.FirstOrDefault(c => c.MinuteMark == 15);
                if (adc15 != null && sup15 != null && enemyAdc15 != null && enemySup15 != null)
                {
                    var duoGold15 = adc15.Gold + sup15.Gold;
                    var enemyDuoGold15 = enemyAdc15.Gold + enemySup15.Gold;
                    eg15 = duoGold15 - enemyDuoGold15;

                    // Track outcomes both when ahead and when behind at 15
                    if (eg15 > 0)
                    {
                        // Won/lost while ahead at 15
                        winAhead15 = teamWin.GetValueOrDefault(team);
                    }
                    else if (eg15 < 0)
                    {
                        // Won/lost from behind at 15 (captures comebacks)
                        winAhead15 = teamWin.GetValueOrDefault(team);
                    }
                    // If exactly even at 15, leave null
                }

                // Persist duo metrics
                await v2DuoMetrics.InsertAsync(new RiotProxy.External.Domain.Entities.V2.V2DuoMetric
                {
                    MatchId = matchId,
                    ParticipantId1 = adcV2Id,
                    ParticipantId2 = supV2Id,
                    EarlyGoldDelta10 = eg10,
                    EarlyGoldDelta15 = eg15,
                    WinWhenAheadAt15 = winAhead15,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
    }