using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RiotProxy.Infrastructure
{
    public static class Secrets
    {
        private static bool _initialized;
        private static readonly string SecretsFolder = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApiKey { get; private set; } = string.Empty;
        public static string DatabaseConnectionString { get; private set; } = string.Empty;
        public static string DatabaseConnectionStringV2 { get; private set; } = string.Empty;

        /// <summary>
        /// Load secrets from configuration/env. Falls back to optional local files if present.
        /// </summary>
        public static void Initialize(IConfiguration config)
        {
            if (_initialized)
                return;

            ApiKey = FirstNonEmpty(
                config["Riot:ApiKey"],
                config["RIOT_API_KEY"],
                Environment.GetEnvironmentVariable("RIOT_API_KEY"),
                ReadIfExists("RiotSecret.txt"));

            DatabaseConnectionString = FirstNonEmpty(
                config.GetConnectionString("Default"),
                config["ConnectionStrings:Database"],
                config["Database:ConnectionString"],
                config["LOL_DB_CONNECTIONSTRING"],
                Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING"),
                ReadIfExists("DatabaseSecret.txt"));

            DatabaseConnectionStringV2 = FirstNonEmpty(
                config.GetConnectionString("DatabaseV2"),
                config["ConnectionStrings:DatabaseV2"],
                config["Database:ConnectionStringV2"],
                config["LOL_DB_CONNECTIONSTRING_V2"],
                Environment.GetEnvironmentVariable("LOL_DB_CONNECTIONSTRING_V2"),
                ReadIfExists("DatabaseSecretV2.txt"));

            _initialized = true;
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