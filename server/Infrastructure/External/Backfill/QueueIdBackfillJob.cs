using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;
using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Backfill job to populate QueueId for existing matches that don't have it.
    /// </summary>
    public class QueueIdBackfillJob : IBackfillJob
    {
        private readonly LolMatchRepository _matchRepository;
        private readonly IRiotApiClient _riotApiClient;
        private IList<string> _matchIds = new List<string>();

        public QueueIdBackfillJob(LolMatchRepository matchRepository, IRiotApiClient riotApiClient)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _riotApiClient = riotApiClient ?? throw new ArgumentNullException(nameof(riotApiClient));
        }

        public string Name => "QueueId Backfill";

        public async Task<int> GetTotalItemsAsync(CancellationToken ct = default)
        {
            // Query matches where QueueId IS NULL AND InfoFetched = TRUE
            var matches = await _matchRepository.GetUnprocessedMatchesAsync();
            
            // Filter for matches that have been fetched but don't have QueueId
            _matchIds = matches
                .Where(m => m.InfoFetched && !m.QueueId.HasValue)
                .Select(m => m.MatchId)
                .ToList();

            return _matchIds.Count;
        }

        public async Task<int> ProcessBatchAsync(int startIndex, int batchSize, CancellationToken ct = default)
        {
            var batchMatchIds = _matchIds
                .Skip(startIndex)
                .Take(batchSize)
                .ToList();

            int processed = 0;

            foreach (var matchId in batchMatchIds)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // Fetch match info from Riot API
                    var matchInfoJson = await _riotApiClient.GetMatchInfoAsync(matchId, ct);

                    // Extract QueueId
                    var queueId = ExtractQueueId(matchInfoJson);

                    if (queueId.HasValue)
                    {
                        // Update match with QueueId
                        var matches = await _matchRepository.GetExistingMatchesAsync(new[] { matchId });
                        var match = matches.FirstOrDefault();

                        if (match != null)
                        {
                            match.QueueId = queueId;
                            await _matchRepository.UpdateMatchAsync(match);
                            processed++;
                        }
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"[{Name}] Error processing match {matchId}: {ex.Message}");
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

        private int? ExtractQueueId(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("queueId", out var queueIdElement))
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
    }
}
