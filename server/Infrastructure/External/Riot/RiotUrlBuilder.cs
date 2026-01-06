
using Microsoft.AspNetCore.DataProtection;

namespace RiotProxy.Infrastructure.External.Riot
{
    public static class RiotUrlBuilder
    {
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
            { "9765", "eun1" } // spicial case for Doend
        };

        public static string GetAccountUrl(string path)
        {
            return $"https://europe.api.riotgames.com/riot{path}?api_key={Secrets.ApiKey}";
        }

        public static string GetMatchUrl(string path)
        {
            return $"https://europe.api.riotgames.com/lol{path}?api_key={Secrets.ApiKey}";
        }

        public static string GetSummonerUrl(string tagline, string puuid)
        {
            if (!_regionMapping.TryGetValue(tagline.ToUpper(), out var regionCode))
            {
                throw new ArgumentException($"Invalid region tagline: {tagline}. Supported regions: {string.Join(", ", _regionMapping.Keys)}");
            }
            return $"https://{regionCode}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={Secrets.ApiKey}";
        }
    }
}