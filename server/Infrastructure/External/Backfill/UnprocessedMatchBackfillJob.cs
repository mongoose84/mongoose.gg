using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.External.Domain.Entities;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Backfill for matches with InfoFetched=false: hydrate match metadata and insert participants.
    /// </summary>
    public class UnprocessedMatchBackfillJob : IBackfillJob
    {
        private readonly LolMatchRepository _matchRepository;
        private readonly LolMatchParticipantRepository _participantRepository;
        private readonly IRiotApiClient _riotApiClient;
        private IList<LolMatch> _matches = new List<LolMatch>();

        public UnprocessedMatchBackfillJob(
            LolMatchRepository matchRepository,
            LolMatchParticipantRepository participantRepository,
            IRiotApiClient riotApiClient)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _riotApiClient = riotApiClient ?? throw new ArgumentNullException(nameof(riotApiClient));
        }

        public string Name => "Unprocessed Match Backfill";

        public async Task<int> GetTotalItemsAsync(CancellationToken ct = default)
        {
            _matches = await _matchRepository.GetUnprocessedMatchesAsync();
            return _matches.Count;
        }

        public async Task<int> ProcessBatchAsync(int startIndex, int batchSize, CancellationToken ct = default)
        {
            var batch = _matches.Skip(startIndex).Take(batchSize).ToList();
            var processed = 0;

            foreach (var match in batch)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var matchInfoJson = await _riotApiClient.GetMatchInfoAsync(match.MatchId, ct);

                    ExtractAndMapMatchData(matchInfoJson, match);
                    await _matchRepository.UpdateMatchAsync(match);

                    var participants = MapToParticipantEntity(matchInfoJson, match.MatchId);
                    foreach (var participant in participants)
                    {
                        await _participantRepository.AddParticipantIfNotExistsAsync(participant);
                    }

                    processed++;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"[{Name}] Error processing match {match.MatchId}: {ex.Message}");
                }
            }

            return processed;
        }

        public Task OnCompleteAsync(CancellationToken ct = default)
        {
            Console.WriteLine($"[{Name}] Backfill completed successfully.");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex, CancellationToken ct = default)
        {
            Console.WriteLine($"[{Name}] Backfill failed with error: {ex.Message}");
            return Task.CompletedTask;
        }

        private void ExtractAndMapMatchData(JsonDocument matchInfo, LolMatch match)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                match.GameEndTimestamp = BackfillMatchJsonExtractor.ExtractGameEndTimestamp(matchInfo);
                match.GameMode = BackfillMatchJsonExtractor.ExtractGameMode(matchInfo);
                match.QueueId = BackfillMatchJsonExtractor.ExtractQueueId(matchInfo);
                match.InfoFetched = true;

                if (infoElement.TryGetProperty("gameDuration", out var gameDurationElement) &&
                    gameDurationElement.ValueKind == JsonValueKind.Number)
                {
                    match.DurationSeconds = gameDurationElement.GetInt64();
                }
            }
        }

        private IList<LolMatchParticipant> MapToParticipantEntity(JsonDocument matchInfo, string matchId)
        {
            if (!matchInfo.RootElement.TryGetProperty("info", out var infoElement))
                return new List<LolMatchParticipant>();

            if (!infoElement.TryGetProperty("participants", out var participantsElement))
                return new List<LolMatchParticipant>();

            return participantsElement.EnumerateArray()
                .Select(participantElement => new LolMatchParticipant
                {
                    MatchId = matchId,
                    PuuId = participantElement.TryGetProperty("puuid", out var puuIdEl) ? puuIdEl.GetString() ?? string.Empty : string.Empty,
                    TeamId = participantElement.TryGetProperty("teamId", out var teamIdEl) && teamIdEl.ValueKind == JsonValueKind.Number ? teamIdEl.GetInt32() : 0,
                    Win = participantElement.TryGetProperty("win", out var winEl) && winEl.GetBoolean(),
                    Role = participantElement.TryGetProperty("role", out var roleEl) ? roleEl.GetString() ?? string.Empty : string.Empty,
                    TeamPosition = participantElement.TryGetProperty("teamPosition", out var teamPosEl) ? teamPosEl.GetString() ?? string.Empty : string.Empty,
                    Lane = participantElement.TryGetProperty("lane", out var laneEl) ? laneEl.GetString() ?? string.Empty : string.Empty,
                    ChampionId = participantElement.TryGetProperty("championId", out var champIdEl) && champIdEl.ValueKind == JsonValueKind.Number ? champIdEl.GetInt32() : 0,
                    ChampionName = participantElement.TryGetProperty("championName", out var champEl) ? champEl.GetString() ?? string.Empty : string.Empty,
                    Kills = participantElement.TryGetProperty("kills", out var killsEl) && killsEl.ValueKind == JsonValueKind.Number ? killsEl.GetInt32() : 0,
                    Deaths = participantElement.TryGetProperty("deaths", out var deathsEl) && deathsEl.ValueKind == JsonValueKind.Number ? deathsEl.GetInt32() : 0,
                    Assists = participantElement.TryGetProperty("assists", out var assistsEl) && assistsEl.ValueKind == JsonValueKind.Number ? assistsEl.GetInt32() : 0,
                    DoubleKills = participantElement.TryGetProperty("doubleKills", out var doubleEl) && doubleEl.ValueKind == JsonValueKind.Number ? doubleEl.GetInt32() : 0,
                    TripleKills = participantElement.TryGetProperty("tripleKills", out var tripleEl) && tripleEl.ValueKind == JsonValueKind.Number ? tripleEl.GetInt32() : 0,
                    QuadraKills = participantElement.TryGetProperty("quadraKills", out var quadraEl) && quadraEl.ValueKind == JsonValueKind.Number ? quadraEl.GetInt32() : 0,
                    PentaKills = participantElement.TryGetProperty("pentaKills", out var pentaEl) && pentaEl.ValueKind == JsonValueKind.Number ? pentaEl.GetInt32() : 0,
                    GoldEarned = participantElement.TryGetProperty("goldEarned", out var goldEl) && goldEl.ValueKind == JsonValueKind.Number ? goldEl.GetInt32() : 0,
                    CreepScore = participantElement.TryGetProperty("totalMinionsKilled", out var minionsEl) && minionsEl.ValueKind == JsonValueKind.Number ? minionsEl.GetInt32() : 0,
                    TimeBeingDeadSeconds = participantElement.TryGetProperty("totalTimeDeadSeconds", out var deadEl) && deadEl.ValueKind == JsonValueKind.Number ? deadEl.GetInt32() : 0,
                })
                .ToList();
        }
    }
}
