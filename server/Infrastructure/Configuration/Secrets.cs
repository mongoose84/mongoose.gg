namespace RiotProxy.Infrastructure
{
    public static class Secrets
    {
        private static bool _initialized = false;
        private static readonly string SecretsFolder = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApiKey { get; private set; } = string.Empty;
        public static string DatabaseConnectionString { get; private set; } = string.Empty;
        public static string DatabaseConnectionStringV2 { get; private set; } = string.Empty;

        public static void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The DatabaseConnectionString and ApiKey have already been set. It is not possible to overwrite them.");
            }

            DatabaseConnectionString = Read("DatabaseSecret.txt");
            // Optional: new v2 database connection string for migration/cutover
            DatabaseConnectionStringV2 = TryRead("DatabaseSecretV2.txt");

            ApiKey = Read("RiotSecret.txt");

            _initialized = true;
        }
        
        private static string Read(string filename)
        {
            string filePath = Path.Combine(SecretsFolder, filename);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Secret file not found: {filename}");
            }

            return File.ReadAllText(filePath).Trim();
        }

        private static string TryRead(string filename)
        {
            string filePath = Path.Combine(SecretsFolder, filename);
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }
            return File.ReadAllText(filePath).Trim();
        }
        
    }
}