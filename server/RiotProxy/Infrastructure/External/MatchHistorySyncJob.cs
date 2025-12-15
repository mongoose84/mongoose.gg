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
                var newMatches = await GetNewMatchHistoryFromRiotApi(gamers, riotApiClient, matchRepository, ct);
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
            LolMatchRepository matchRepository,
            CancellationToken ct)
        {
            var allNewMatches = new List<LolMatch>();

            foreach (var gamer in gamers)
            {
                // Get existing match IDs for this gamer
                Console.WriteLine($"Fetching match history for gamer: {gamer.GamerName}");
                var existingMatchIds = await matchRepository.GetMatchIdsForPuuidAsync(gamer.Puuid);
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
            foreach (var match in matches)
            {
                try
                {
                    var matchInfoJson = await riotApiClient.GetMatchInfoAsync(match.MatchId);
                    var participant = MapToParticipantEntity(matchInfoJson, match);

                    await participantRepository.AddParticipantIfNotExistsAsync(participant);

                    match.GameMode = GetGameMode(matchInfoJson);
                    match.InfoFetched = true;
                    await matchRepository.UpdateMatchAsync(match);

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
            Console.WriteLine($"{matches.Count} match participants added to DB.");
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

        private LolMatchParticipant MapToParticipantEntity(JsonDocument matchInfo, LolMatch match)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("participants", out var participantsElement))
            {
                // gameEndTimestamp is epoch ms; fall back to gameCreation if needed
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp")
                            ?? GetEpochMilliseconds(infoElement, "gameCreation")
                            ?? 0L;

                if (endMs > 0)
                    match.GameEndTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(endMs).UtcDateTime;
                else
                    match.GameEndTimestamp = DateTime.MinValue;

                foreach (var participant in participantsElement.EnumerateArray())
                {
                    if (participant.TryGetProperty("puuid", out var puuidElement) &&
                        puuidElement.GetString() == match.Puuid)
                    {
                        if (!matchInfo.RootElement.TryGetProperty("metadata", out var metadataElement) ||
                            !metadataElement.TryGetProperty("matchId", out var matchIdElement) ||
                            matchIdElement.ValueKind != JsonValueKind.String)
                            throw new InvalidOperationException("Missing or invalid 'metadata.matchId' property.");
                        if (!participant.TryGetProperty("championName", out var championNameElement) ||
                            championNameElement.ValueKind != JsonValueKind.String)
                            throw new InvalidOperationException("Missing or invalid 'championName' property in participant.");
                        if (!participant.TryGetProperty("win", out var winElement) ||
                            winElement.ValueKind != JsonValueKind.True && winElement.ValueKind != JsonValueKind.False)
                            throw new InvalidOperationException("Missing or invalid 'win' property in participant.");
                        if (!participant.TryGetProperty("role", out var roleElement) ||
                            roleElement.ValueKind != JsonValueKind.String)
                            throw new InvalidOperationException("Missing or invalid 'role' property in participant.");
                        if (!participant.TryGetProperty("kills", out var killsElement) ||
                            killsElement.ValueKind != JsonValueKind.Number)
                            throw new InvalidOperationException("Missing or invalid 'kills' property in participant.");
                        if (!participant.TryGetProperty("deaths", out var deathsElement) ||
                            deathsElement.ValueKind != JsonValueKind.Number)
                            throw new InvalidOperationException("Missing or invalid 'deaths' property in participant.");
                        if (!participant.TryGetProperty("assists", out var assistsElement) ||
                            assistsElement.ValueKind != JsonValueKind.Number)
                            throw new InvalidOperationException("Missing or invalid 'assists' property in participant.");

                        var matchId = matchIdElement.GetString() ??
                            throw new InvalidOperationException("Missing or invalid 'MatchId' property in participant.");
                        var puuidValue = puuidElement.GetString() ??
                            throw new InvalidOperationException("Missing or invalid 'Puuid' property in participant.");
                        var championName = championNameElement.GetString() ??
                            throw new InvalidOperationException("Missing or invalid 'ChampionName' property in participant.");
                        var role = roleElement.GetString() ??
                            throw new InvalidOperationException("Missing or invalid 'Role' property in participant.");

                        return new LolMatchParticipant
                        {
                            MatchId = matchId,
                            Puuid = puuidValue,
                            ChampionName = championName,
                            Win = winElement.GetBoolean(),
                            Role = role,
                            Kills = killsElement.GetInt32(),
                            Deaths = deathsElement.GetInt32(),
                            Assists = assistsElement.GetInt32(),
                        };
                    }
                }
            }

            throw new InvalidOperationException("Participant not found.");
        }
    }
}