using RiotProxy.Infrastructure;
using RiotProxy.Infrastructure.Telemetry;

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
            app.MapGet(Route, (HttpContext httpContext, IConfiguration config) =>
            {
                // Increment metrics counter
                Metrics.IncrementMetrics();

                var isApiKeyConfigured = !string.IsNullOrWhiteSpace(Secrets.ApiKey);
                var isDbConfigured = !string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString);
                var isEncryptionKeyConfigured = !string.IsNullOrWhiteSpace(Secrets.EncryptionSecret);

                // Check SMTP configuration from multiple sources
                var smtpHostFromEnv = Environment.GetEnvironmentVariable("SMTP_HOST");
                var smtpHostFromConfig = config["Email:SmtpHost"];
                var isSmtpConfigured = !string.IsNullOrWhiteSpace(smtpHostFromEnv) || !string.IsNullOrWhiteSpace(smtpHostFromConfig);

                // Get configuration source details (without exposing actual values)
                var configSources = GetConfigurationSources(config);

                var diagnostics = new
                {
                    environment = GetEnvironment(),
                    timestamp = DateTime.UtcNow,
                    build = Metrics.BuildNumber,
                    configuration = new
                    {
                        apiKeyConfigured = isApiKeyConfigured,
                        databaseConfigured = isDbConfigured,
                        smtpConfigured = isSmtpConfigured,
                        emailEncryptionKeyConfigured = isEncryptionKeyConfigured,
                        allConfigured = isApiKeyConfigured &&
                                        isDbConfigured &&
                                        isSmtpConfigured &&
                                        isEncryptionKeyConfigured
                    },
                    configurationSources = configSources,
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
                        "Required env vars: RIOT_API_KEY, Database_production, ENCRYPTION_SECRET, SMTP_HOST, SMTP_USERNAME, SMTP_PASSWORD.",
                        "Or set in appsettings.Production.json under ConnectionStrings and Email sections.",
                        "Check 'configurationSources' to see which config sources are being checked."
                    }
                };

                return Results.Ok(diagnostics);
            });
        }

        private static object GetConfigurationSources(IConfiguration config)
        {
            var env = GetEnvironment();
            var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

            // Mask sensitive values - only show if they exist, not the actual values
            string MaskValue(string? value) => string.IsNullOrWhiteSpace(value) ? "NOT_SET" : $"SET ({value.Length} chars)";

            // Show first few chars to help debug (safe for connection strings that start with "Server=")
            string MaskValueWithPrefix(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "NOT_SET";
                var prefix = value.Length > 10 ? value.Substring(0, 10) : value;
                return $"SET ({value.Length} chars, starts with '{prefix}...')";
            }

            var dbKey = isProduction ? "Database_production" : "Database_test";
            var dbFromConfig = config.GetConnectionString(dbKey);
            var dbDirect = config["ConnectionStrings:" + dbKey];

            return new
            {
                database = new
                {
                    expectedKey = dbKey,
                    connectionStringFromConfig = MaskValueWithPrefix(dbFromConfig),
                    connectionStringDirect = MaskValueWithPrefix(dbDirect),
                    fromEnvironmentVariable = MaskValue(Environment.GetEnvironmentVariable(dbKey)),
                    secretsValue = MaskValueWithPrefix(Secrets.DatabaseConnectionString),
                    // Debug: Show what Secrets.Initialize would have seen
                    debugInfo = new
                    {
                        configHasValue = !string.IsNullOrWhiteSpace(dbFromConfig),
                        secretsInitialized = !string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString),
                        valuesMatch = dbFromConfig == Secrets.DatabaseConnectionString
                    }
                },
                smtp = new
                {
                    smtpHostFromConfig = MaskValue(config["Email:SmtpHost"]),
                    smtpHostFromEnv = MaskValue(Environment.GetEnvironmentVariable("SMTP_HOST")),
                    smtpUsernameFromConfig = MaskValue(config["Email:SmtpUsername"]),
                    smtpUsernameFromEnv = MaskValue(Environment.GetEnvironmentVariable("SMTP_USERNAME")),
                    smtpPasswordFromConfig = MaskValue(config["Email:SmtpPassword"]),
                    smtpPasswordFromEnv = MaskValue(Environment.GetEnvironmentVariable("SMTP_PASSWORD")),
                    smtpPortFromConfig = config.GetValue<int>("Email:SmtpPort", 0)
                },
                riotApi = new
                {
                    fromConfig = MaskValue(config["Riot:ApiKey"]),
                    fromEnv = MaskValue(Environment.GetEnvironmentVariable("RIOT_API_KEY")),
                    secretsValue = MaskValue(Secrets.ApiKey)
                },
                encryption = new
                {
                    fromConfig = MaskValue(config["Security:EncryptionSecret"]),
                    fromEnv = MaskValue(Environment.GetEnvironmentVariable("ENCRYPTION_SECRET")),
                    secretsValue = MaskValue(Secrets.EncryptionSecret)
                }
            };
        }

        private static string GetEnvironment()
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return !string.IsNullOrWhiteSpace(aspnetEnv) ? aspnetEnv : "Production";
        }
    }
}
