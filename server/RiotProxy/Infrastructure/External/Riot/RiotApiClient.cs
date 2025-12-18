using System.Text.Json;
using System.Web;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Utilities;

namespace RiotProxy.Infrastructure.External.Riot
{
    public class RiotApiClient : IRiotApiClient
    {
        // Token buckets are set lower for RIOT rate limits  (20 requests/second, 100 requests/2 minutes)
        private readonly RiotTokenBucket _perSecondBucket = new(15, TimeSpan.FromSeconds(1));
        private readonly RiotTokenBucket _perTwoMinuteBucket = new(80, TimeSpan.FromMinutes(2));

        private readonly HttpClient _http;

        public RiotApiClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("RiotApi");
        }

        public async Task<double> GetWinrateAsync(string puuid)
        {
            var wins = 0;

            var matchHistory = await GetMatchHistoryAsync(puuid);
            var totalGames = matchHistory.Count;
            
            foreach (var match in matchHistory)
            {
                var matchDetails = await GetMatchInfoAsync(match.MatchId);
                var info = matchDetails.RootElement.GetProperty("info");
                var participants = info.GetProperty("participants").EnumerateArray();
                foreach (var participant in participants)
                {
                    if (participant.GetProperty("puuid").GetString() == puuid)
                    {
                        if (participant.GetProperty("win").GetBoolean())
                        {
                            wins++;
                        }
                        break;
                    }
                }  
            }
            double winrate = (double)wins / totalGames * 100;
            return winrate; // Placeholder value
        }

        public async Task<string> GetPuuidAsync(string gameName, string tagLine, CancellationToken ct = default)
        {
            // Build the full request URI.
            var path = $"/account/v1/accounts/by-riot-id/{gameName}/{tagLine}";
            var url = RiotUrlBuilder.GetAccountUrl(path);
            Metrics.SetLastUrlCalled("RiotServices.cs ln 67" + url);

            await _perSecondBucket.WaitAsync(ct);
            await _perTwoMinuteBucket.WaitAsync(ct);

            // Perform the GET request.
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();   // Throws if the status is not 2xx.

            // Read the JSON payload.
            string json = await response.Content.ReadAsStringAsync();
            
            // Parse the JSON to extract the "puuid" field.
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("puuid", out JsonElement puuidElement))
            {
                var puuid = puuidElement.GetString();
                return puuid ?? throw new InvalidOperationException("The 'puuid' field is null.");
            }

            throw new InvalidOperationException("Response does not contain a 'puuid' field.");
        }

        public async Task<IList<LolMatch>> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default)
        {
            var url = $"{RiotUrlBuilder.GetMatchUrl($"/match/v5/matches/by-puuid/{puuid}/ids")}&start={start}&count={count}";

            if (startTime.HasValue)
                url += $"&startTime={startTime.Value}";

            await _perSecondBucket.WaitAsync(ct);
            await _perTwoMinuteBucket.WaitAsync(ct);

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var matchIds = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            
            var matchIdList = matchIds.Select(id => new LolMatch { MatchId = id, Puuid = puuid, InfoFetched = false }).ToList();
            return matchIdList;
        }

        public async Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default)
        {
            var matchUrl = RiotUrlBuilder.GetMatchUrl($"/match/v5/matches/{matchId}");
            Metrics.SetLastUrlCalled("RiotServices.cs ln 110" + matchUrl);

            await _perSecondBucket.WaitAsync(ct);
            await _perTwoMinuteBucket.WaitAsync(ct);

            var response = await _http.GetAsync(matchUrl, ct);
            response.EnsureSuccessStatusCode();   // Throws if the status is not 2xx
            var json = await response.Content.ReadAsStringAsync();
            var matchDoc = JsonDocument.Parse(json);
            return matchDoc;
        }

        public async Task<Summoner?> GetSummonerByPuuidAsync(string region, string puuid, CancellationToken ct = default)
        {
            var encodedPuuid = HttpUtility.UrlEncode(puuid);
            var summonerUrl = RiotUrlBuilder.GetSummonerUrl(region, encodedPuuid);
            Metrics.SetLastUrlCalled("RiotServices.cs ln 134" + summonerUrl);

            await _perSecondBucket.WaitAsync(ct);
            await _perTwoMinuteBucket.WaitAsync(ct);

            var response = await _http.GetAsync(summonerUrl, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Summoner not found
            }
            
            response.EnsureSuccessStatusCode();   // Throws if the status is not 2xx.

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var summoner = JsonSerializer.Deserialize<Summoner>(json, options);
            return summoner;
        }

        public Task<string> GetLolVersionAsync(CancellationToken ct = default)
        {
            var url = "https://ddragon.leagueoflegends.com/api/versions.json";
            try
            {
                var response = _http.GetAsync(url, ct).Result;
                response.EnsureSuccessStatusCode();
                var json = response.Content.ReadAsStringAsync().Result;
                var versions = JsonSerializer.Deserialize<List<string>>(json);
                if (versions != null && versions.Count > 0)
                {
                    return Task.FromResult(versions[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching LoL version: {ex.Message}");
            }
            return Task.FromResult(string.Empty);
        }
    }
}