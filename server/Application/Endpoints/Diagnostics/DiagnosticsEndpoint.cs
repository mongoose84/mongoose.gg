using RiotProxy.Infrastructure;
using RiotProxy.Utilities;

namespace RiotProxy.Application.Endpoints.Diagnostics
{
    /// <summary>
    /// Diagnostics endpoint for verifying configuration, connectivity, and runtime metrics.
    /// Returns status of critical infrastructure: database, API keys, usage statistics, etc.
    /// </summary>
    public sealed class DiagnosticsEndpoint : IEndpoint
    {
        public string Route { get; }

        public DiagnosticsEndpoint(string basePath)
        {
            Route = basePath + "/diagnostics";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, (HttpContext httpContext) =>
            {
                // Increment metrics counter
                Metrics.IncrementMetrics();

                var isApiKeyConfigured = !string.IsNullOrWhiteSpace(Secrets.ApiKey);
                var isDbV2Configured = !string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionStringV2);
                var isEncryptionKeyConfigured = !string.IsNullOrWhiteSpace(Secrets.EmailEncryptionKey);

                var diagnostics = new
                {
                    environment = GetEnvironment(),
                    timestamp = DateTime.UtcNow,
                    build = Metrics.BuildNumber,
                    configuration = new
                    {
                        apiKeyConfigured = isApiKeyConfigured,
                        databaseV2Configured = isDbV2Configured,
                        emailEncryptionKeyConfigured = isEncryptionKeyConfigured,
                        allConfigured = isApiKeyConfigured && isDbV2Configured && isEncryptionKeyConfigured
                    },
                    metrics = new
                    {
                        homeHits = Metrics.HomeHits,
                        metricHits = Metrics.MetricHits,
                        winrateHits = Metrics.WinrateHits,
                        summonerHits = Metrics.SummonerHits,
                        lastUrlCalled = string.IsNullOrWhiteSpace(Metrics.LastUrlCalled) ? "none" : Metrics.LastUrlCalled
                    },
                    notes = new string[]
                    {
                        "If 'allConfigured' is false, check README.md for environment variable setup instructions.",
                        "Required env vars: RIOT_API_KEY, LOL_DB_CONNECTIONSTRING, LOL_DB_CONNECTIONSTRING_V2",
                        "Or set in appsettings.json under ConnectionStrings section."
                    }
                };

                return Results.Ok(diagnostics);
            });
        }

        private static string GetEnvironment()
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return !string.IsNullOrWhiteSpace(aspnetEnv) ? aspnetEnv : "Production";
        }
    }
}
