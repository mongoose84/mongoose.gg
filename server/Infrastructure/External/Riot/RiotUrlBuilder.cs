
using Microsoft.AspNetCore.DataProtection;

namespace RiotProxy.Infrastructure.External.Riot
{
    public static class RiotUrlBuilder
    {
        // Maps display names to Riot API region codes
        private static readonly Dictionary<string, string> _regionMapping = new Dictionary<string, string>
        {
            { "NA", "na1" },
            { "EUW", "euw1" },
            { "EUNE", "eun1" },
            { "KR", "kr" },
            { "JP", "jp1" },
            { "BR", "br1" },
            { "LAN", "la1" },
            { "LAS", "la2" },
            { "OCE", "oc1" },
            { "RU", "ru" },
            { "TR", "tr1" },
            { "9765", "eun1" } // special case for Doend
        };

        // Also accept API codes directly (reverse mapping)
        private static readonly Dictionary<string, string> _apiCodeMapping = new Dictionary<string, string>
        {
            { "NA1", "na1" },
            { "EUW1", "euw1" },
            { "EUN1", "eun1" },
            { "JP1", "jp1" },
            { "BR1", "br1" },
            { "LA1", "la1" },
            { "LA2", "la2" },
            { "OC1", "oc1" },
        };

        public static string GetAccountUrl(string path)
        {
            EnsureApiKey();
            return $"https://europe.api.riotgames.com/riot{path}?api_key={Secrets.ApiKey}";
        }

        public static string GetMatchUrl(string path)
        {
            EnsureApiKey();
            return $"https://europe.api.riotgames.com/lol{path}?api_key={Secrets.ApiKey}";
        }

        public static string GetSummonerUrl(string region, string puuid)
        {
            var upperRegion = region.ToUpper();

            // First try display name mapping (NA, EUW, EUNE, etc.)
            if (!_regionMapping.TryGetValue(upperRegion, out var regionCode))
            {
                // Then try API code mapping (na1, euw1, eun1, etc.)
                if (!_apiCodeMapping.TryGetValue(upperRegion, out regionCode))
                {
                    // If the input is already a valid API code, use it directly
                    var lowerRegion = region.ToLowerInvariant();
                    if (_apiCodeMapping.Values.Contains(lowerRegion) || _regionMapping.Values.Contains(lowerRegion))
                    {
                        regionCode = lowerRegion;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid region: {region}. Supported regions: {string.Join(", ", _regionMapping.Keys)} or API codes: {string.Join(", ", _apiCodeMapping.Keys)}");
                    }
                }
            }
            EnsureApiKey();
            return $"https://{regionCode}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={Secrets.ApiKey}";
        }

        private static void EnsureApiKey()
        {
            if (string.IsNullOrWhiteSpace(Secrets.ApiKey))
                throw new InvalidOperationException("Riot API key is not configured. Set RIOT_API_KEY or configuration key Riot:ApiKey.");
        }
    }
}