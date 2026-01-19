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
        public static string EncryptionSecret { get; private set; } = string.Empty;

        /// <summary>
        /// Load secrets from configuration/env. Falls back to optional local files if present.
        /// Priority: appsettings → env vars → local files
        /// Debug logging is conditional (Development or Secrets:EnableDebugLogging=true).
        /// </summary>
        public static void Initialize(IConfiguration config)
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
            var isTesting = string.Equals(aspnetEnv, "Testing", StringComparison.OrdinalIgnoreCase);
            var isProduction = string.Equals(aspnetEnv, "Production", StringComparison.OrdinalIgnoreCase);

            // Compute candidates outside the lock to reduce lock duration
            var apiKeyCandidate = FirstNonEmpty(
                config["Riot:ApiKey"],
                config["RIOT_API_KEY"],
                Environment.GetEnvironmentVariable("RIOT_API_KEY"));

            string dbConnectionV2Candidate;
            if (isProduction)
            {
                // Production: prefer production-specific connection string
                dbConnectionV2Candidate = GetDatabaseConnectionString(config, "Database_production");
            }
            else if (isTesting)
            {
                // Testing: prefer test-specific connection string
                dbConnectionV2Candidate = GetDatabaseConnectionString(config, "Database_test");
            }
            else
            {
                // Default (Development or other): use standard connection string
                dbConnectionV2Candidate = GetDatabaseConnectionString(config, "Database_test");
            }

            var encryptionSecretCandidate = FirstNonEmpty(
                config["Security:EncryptionSecret"],
                config["ENCRYPTION_SECRET"],
                Environment.GetEnvironmentVariable("ENCRYPTION_SECRET"));
            lock (_initLock)
            {
                // In non-testing environments, initialize only once for thread safety.
                if (_initialized && !isTesting)
                    return;

                ApiKey = apiKeyCandidate;
                DatabaseConnectionString = dbConnectionV2Candidate;
                EncryptionSecret = encryptionSecretCandidate;

                // Optional debug logging: only in Development or when explicitly enabled
                var enableSecretsDebug = config.GetValue<bool>("Secrets:EnableDebugLogging", false);
                var isDevelopment = string.Equals(aspnetEnv, "Development", StringComparison.OrdinalIgnoreCase);
                if (enableSecretsDebug || isDevelopment)
                {
                    LogConfigurationStatus(isProduction);
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Log which secrets were successfully loaded (for debugging configuration issues).
        /// </summary>
        private static void LogConfigurationStatus(bool isProduction)
        {
            var isDbConnectionSet = false;
            var dbType = isProduction ? "Production" : "Test";
            if (isProduction)
            {
                isDbConnectionSet = !string.IsNullOrWhiteSpace(DatabaseConnectionString);
            }
            else
            {
                isDbConnectionSet = !string.IsNullOrWhiteSpace(DatabaseConnectionString);
            }
            var isApiKeySet = !string.IsNullOrWhiteSpace(ApiKey);
            var isEncryptionSecretSet = !string.IsNullOrWhiteSpace(EncryptionSecret);

            Console.WriteLine($"[Secrets.Initialize] ApiKey configured: {isApiKeySet}");
            Console.WriteLine($"[Secrets.Initialize] Database configured: {isDbConnectionSet} ({dbType})");
            Console.WriteLine($"[Secrets.Initialize] EncryptionSecret configured: {isEncryptionSecretSet}");
            // List environment variables that might be missing
            if (!isDbConnectionSet)
            {
                Console.WriteLine("[Secrets.Initialize] WARNING: Database connection string not found. Checked: appsettings, env vars");
            }
            if (!isEncryptionSecretSet)
            {
                Console.WriteLine("[Secrets.Initialize] WARNING: ENCRYPTION_SECRET not found. Checked: appsettings Security:EncryptionSecret, env vars");
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .FirstOrDefault() ?? string.Empty;
        }

        private static string GetDatabaseConnectionString(IConfiguration config, string connectionString)
        {
            var dbConnectionString = FirstNonEmpty(
                    config.GetConnectionString(connectionString),
                    config["ConnectionStrings:" + connectionString],
                    config["Database:" + connectionString],
                    config[connectionString],
                    Environment.GetEnvironmentVariable(connectionString));
            return dbConnectionString;
        }
    }
}