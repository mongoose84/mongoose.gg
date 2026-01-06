namespace RiotProxy.Infrastructure
{
    public static class Secrets
    {
        private static bool _initialized = false;
        private static readonly string SecretsFolder = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApiKey { get; private set; } = string.Empty;
        public static string DatabaseConnectionString { get; private set; } = string.Empty;

        public static void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The DatabaseConnectionString and ApiKey have already been set. It is not possible to overwrite them.");
            }

            DatabaseConnectionString = Read("DatabaseSecret.txt");

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
        
    }
}