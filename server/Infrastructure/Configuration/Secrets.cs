using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RiotProxy.Infrastructure
{
    public static class Secrets
    {
        private static bool _initialized;
        private static readonly object _initLock = new object();
        private static readonly string SecretsFolder = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApiKey { get; private set; } = string.Empty;
        public static string DatabaseConnectionString { get; private set; } = string.Empty;
        public static string DatabaseConnectionStringV2 { get; private set; } = string.Empty;

        /// <summary>
        /// Load secrets from configuration/env. Falls back to optional local files if present.
        /// Priority: appsettings → env vars → local files
        /// Debug logging is conditional (Development or Secrets:EnableDebugLogging=true).
        /// </summary>
        public static void Initialize(IConfiguration config)
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
            var isTesting = string.Equals(aspnetEnv, "Testing", StringComparison.OrdinalIgnoreCase);

            // Compute candidates outside the lock to reduce lock duration
            var apiKeyCandidate = FirstNonEmpty(
                config["Riot:ApiKey"],
                config["RIOT_API_KEY"],
                Environment.GetEnvironmentVariable("RIOT_API_KEY"),
                ReadIfExists("RiotSecret.txt"));

            var dbConnectionCandidate = FirstNonEmpty(
                config.GetConnectionString("Default"),
                config["ConnectionStrings:Database"],
                config["Database:ConnectionString"],
                config["LOL_DB_CONNECTIONSTRING"],
                Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING"),
                ReadIfExists("DatabaseSecret.txt"));

            var dbConnectionV2Candidate = FirstNonEmpty(
                config.GetConnectionString("DatabaseV2"),
                config["ConnectionStrings:DatabaseV2"],
                config["Database:ConnectionStringV2"],
                config["LOL_DB_CONNECTIONSTRING_V2"],
                Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING_V2"),
                ReadIfExists("DatabaseSecretV2.txt"));

            lock (_initLock)
            {
                // In non-testing environments, initialize only once for thread safety.
                if (_initialized && !isTesting)
                    return;

                ApiKey = apiKeyCandidate;
                DatabaseConnectionString = dbConnectionCandidate;
                DatabaseConnectionStringV2 = dbConnectionV2Candidate;

                // Optional debug logging: only in Development or when explicitly enabled
                var enableSecretsDebug = config.GetValue<bool>("Secrets:EnableDebugLogging", false);
                var isDevelopment = string.Equals(aspnetEnv, "Development", StringComparison.OrdinalIgnoreCase);
                if (enableSecretsDebug || isDevelopment)
                {
                    LogConfigurationStatus(config);
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Log which secrets were successfully loaded (for debugging configuration issues).
        /// </summary>
        private static void LogConfigurationStatus(IConfiguration config)
        {
            var isApiKeySet = !string.IsNullOrWhiteSpace(ApiKey);
            var isDbConnectionSet = !string.IsNullOrWhiteSpace(DatabaseConnectionString);
            var isDbV2ConnectionSet = !string.IsNullOrWhiteSpace(DatabaseConnectionStringV2);

            Console.WriteLine($"[Secrets.Initialize] ApiKey configured: {isApiKeySet}");
            Console.WriteLine($"[Secrets.Initialize] DatabaseConnectionString configured: {isDbConnectionSet}");
            Console.WriteLine($"[Secrets.Initialize] DatabaseConnectionStringV2 configured: {isDbV2ConnectionSet}");

            // List environment variables that might be missing
            if (!isDbConnectionSet)
            {
                Console.WriteLine("[Secrets.Initialize] WARNING: LOL_DB_CONNECTIONSTRING not found. Checked: appsettings, env vars, DatabaseSecret.txt");
            }
            if (!isDbV2ConnectionSet)
            {
                Console.WriteLine("[Secrets.Initialize] WARNING: LOL_DB_CONNECTIONSTRING_V2 not found. Checked: appsettings, env vars, DatabaseSecretV2.txt");
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .FirstOrDefault() ?? string.Empty;
        }

        private static string ReadIfExists(string filename)
        {
            string filePath = Path.Combine(SecretsFolder, filename);
            if (!File.Exists(filePath))
                return string.Empty;
            return File.ReadAllText(filePath).Trim();
        }
    }
}