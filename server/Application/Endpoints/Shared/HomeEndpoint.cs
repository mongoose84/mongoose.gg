using RiotProxy.Utilities;

namespace RiotProxy.Application.Endpoints
{
    public class HomeEndpoint : IEndpoint
    {
        private readonly string _apiVersion;
        private readonly string _basePath;
        public string Route { get; } = "/";
        public HomeEndpoint(string apiVersion, string basePath)
        {
            _apiVersion = apiVersion;
            _basePath = basePath;
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, () =>
            {
                Metrics.IncrementHome();

                var sitemap = $@"{{  ""Description"": ""Welcome to the League of Legends API. Below are the available endpoints."",  
                                    ""ApiVersion"": ""{_apiVersion}"",
                                    ""{_basePath}/Metrics"": ""Metrics available for this API."", 
                                    ""{_basePath}/Summoner"": ""Retrieve summoner information by game name and tag line."",
                                    ""{_basePath}/Winrate"": ""Retrieve summoner winrate by region and puuid""
                                }}";

                return Results.Content(sitemap, "application/json");
            });
        }

    }
}