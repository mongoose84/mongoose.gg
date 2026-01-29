using System.Text.Json;
using System.Web;
using RiotProxy.Infrastructure.Riot.LimitHandler;
using RiotProxy.Infrastructure.Telemetry;

namespace RiotProxy.Infrastructure.Riot;

public class RiotApiClient : IRiotApiClient
{
    private bool _disposed = false;
    private readonly HttpClient _http;
    private readonly IRiotLimitHandler _riotLimitHandler;

    public RiotApiClient(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("RiotApi");
        _riotLimitHandler = new RiotLimitHandler();
    }

    public async Task<double> GetWinrateAsync(string puuid)
    {
        var wins = 0;

        using var matchHistory = await GetMatchHistoryAsync(puuid);
        var matchIds = matchHistory.RootElement;

        if (matchIds.ValueKind != JsonValueKind.Array || matchIds.GetArrayLength() == 0)
            return 0.0;

        int totalGames = matchIds.GetArrayLength();

        foreach (var matchIdElement in matchIds.EnumerateArray())
        {
            var matchId = matchIdElement.GetString();
            if (string.IsNullOrEmpty(matchId))
                continue;

            using var matchDetails = await GetMatchInfoAsync(matchId);
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
        return winrate;
    }

    public async Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default)
    {
        // Build the full request URI.
        var path = $"/account/v1/accounts/by-riot-id/{gameName}/{tagLine}";
        var url = RiotUrlBuilder.GetAccountUrl(path);
        Metrics.SetLastUrlCalled("RiotServices.cs ln 67" + url);

        await _riotLimitHandler.WaitAsync(ct);

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

    public async Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default)
    {
        var url = $"{RiotUrlBuilder.GetMatchUrl($"/match/v5/matches/by-puuid/{puuid}/ids")}&start={start}&count={count}";

        if (startTime.HasValue)
            url += $"&startTime={startTime.Value}";

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var matchDoc = JsonDocument.Parse(json);
        return matchDoc;
    }

    public async Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default)
    {
        var matchUrl = RiotUrlBuilder.GetMatchUrl($"/match/v5/matches/{matchId}");
        Metrics.SetLastUrlCalled("RiotServices.cs ln 110" + matchUrl);

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(matchUrl, ct);
        response.EnsureSuccessStatusCode();   // Throws if the status is not 2xx
        var json = await response.Content.ReadAsStringAsync();
        var matchDoc = JsonDocument.Parse(json);
        return matchDoc;
    }

    public async Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default)
    {
        var timelineUrl = RiotUrlBuilder.GetMatchUrl($"/match/v5/matches/{matchId}/timeline");
        Metrics.SetLastUrlCalled("RiotServices.cs timeline " + timelineUrl);

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(timelineUrl, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(json);
    }

    public async Task<JsonDocument> GetSummonerByPuuIdAsync(string tagLine, string puuid, CancellationToken ct = default)
    {
        var encodedPuuid = HttpUtility.UrlEncode(puuid);
        var summonerUrl = RiotUrlBuilder.GetSummonerUrl(tagLine, encodedPuuid);
        Metrics.SetLastUrlCalled("RiotServices.cs ln 134" + summonerUrl);

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(summonerUrl, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return JsonDocument.Parse("{}"); // Summoner not found
        }

        response.EnsureSuccessStatusCode();   // Throws if the status is not 2xx.

        var json = await response.Content.ReadAsStringAsync(ct);
        var jsonDoc = JsonDocument.Parse(json);
        return jsonDoc;
    }

    public async Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default)
    {
        var leagueUrl = RiotUrlBuilder.GetLeagueEntriesBySummonerIdUrl(region, summonerId);
        Metrics.SetLastUrlCalled("RiotApiClient.cs GetLeagueEntriesBySummonerId " + leagueUrl);

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(leagueUrl, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return JsonDocument.Parse("[]"); // No league entries found
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(json);
    }

    public async Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default)
    {
        var leagueUrl = RiotUrlBuilder.GetLeagueEntriesByPuuidUrl(region, puuid);
        Metrics.SetLastUrlCalled("RiotApiClient.cs GetLeagueEntriesByPuuid " + leagueUrl);

        await _riotLimitHandler.WaitAsync(ct);

        var response = await _http.GetAsync(leagueUrl, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return JsonDocument.Parse("[]"); // No league entries found
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(json);
    }

    public async Task<string> GetLolVersionAsync(CancellationToken ct = default)
    {
        var url = "https://ddragon.leagueoflegends.com/api/versions.json";
        try
        {
            await _riotLimitHandler.WaitAsync(ct);
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var versions = JsonSerializer.Deserialize<List<string>>(json);
            if (versions != null && versions.Count > 0)
            {
                return versions[0];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching LoL version: {ex.Message}");
        }
        throw new InvalidOperationException("Could not retrieve LoL version.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _http.Dispose();
        _riotLimitHandler.Dispose();
    }
}