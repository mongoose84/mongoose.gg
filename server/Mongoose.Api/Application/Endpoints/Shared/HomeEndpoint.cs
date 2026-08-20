
namespace Mongoose.Api.Application.Endpoints
{
    public sealed class HomeEndpoint : IEndpoint
    {
        private const string ApiVersion = "v2";
        private readonly string _basePath;
        public string Route { get; } = "/";

        public HomeEndpoint(string basePath)
        {
            _basePath = basePath;
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, () =>
            {
                Metrics.IncrementHome();

                var sitemap = $@"{{  ""Description"": ""Welcome to the League of Legends API. Below are the available endpoints."",  
                                    ""ApiVersion"": ""{ApiVersion}"",
                                    ""{_basePath}/Metrics"": ""Metrics available for this API."", 
                                    ""{_basePath}/Summoner"": ""Retrieve summoner information by game name and tag line."",
                                    ""{_basePath}/Winrate"": ""Retrieve summoner winrate by region and puuid""
                                }}";

                return Results.Content(sitemap, "application/json");
            });
        }

    }
}