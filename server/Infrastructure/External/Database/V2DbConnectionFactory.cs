using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class V2DbConnectionFactory : IV2DbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionStringV2))
                throw new InvalidOperationException("DatabaseConnectionStringV2 is not configured. Ensure DatabaseSecretV2.txt exists.");

            return new MySqlConnection(Secrets.DatabaseConnectionStringV2);
        }
    }
}
