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
        public static string DatabaseConnectionStringV2 { get; private set; } = string.Empty;
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
                dbConnectionV2Candidate = FirstNonEmpty(
                    config.GetConnectionString("DatabaseV2_Prod"),
                    config["ConnectionStrings:DatabaseV2_Prod"],
                    config["Database:ConnectionStringV2_Prod"],
                    config["LOL_DB_CONNECTIONSTRING_V2_PROD"],
                    Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING_V2_PROD"),
                    // fallback to default
                    config.GetConnectionString("DatabaseV2"),
                    config["ConnectionStrings:DatabaseV2"],
                    config["Database:ConnectionStringV2"],
                    config["LOL_DB_CONNECTIONSTRING_V2"],
                    Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING_V2"));
            }
            else if (isTesting)
            {
                // Testing: prefer test-specific connection string
                dbConnectionV2Candidate = GetDatabaseConnectionString(config, "Database_production");
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
                DatabaseConnectionStringV2 = dbConnectionV2Candidate;
                EncryptionSecret = encryptionSecretCandidate;

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
            var isDbV2ConnectionSet = !string.IsNullOrWhiteSpace(DatabaseConnectionStringV2);
            var isEncryptionSecretSet = !string.IsNullOrWhiteSpace(EncryptionSecret);

            Console.WriteLine($"[Secrets.Initialize] ApiKey configured: {isApiKeySet}");
            Console.WriteLine($"[Secrets.Initialize] DatabaseConnectionStringV2 configured: {isDbV2ConnectionSet}");
            Console.WriteLine($"[Secrets.Initialize] EncryptionSecret configured: {isEncryptionSecretSet}");
            // List environment variables that might be missing
            if (!isDbV2ConnectionSet)
            {
                Console.WriteLine("[Secrets.Initialize] WARNING: LOL_DB_CONNECTIONSTRING_V2 not found. Checked: appsettings, env vars");
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