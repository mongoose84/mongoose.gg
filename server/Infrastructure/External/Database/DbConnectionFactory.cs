using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionStringV2))
                throw new InvalidOperationException(
                    "Database connection string is not configured. " +
                    "Please set one of: " +
                    "appsettings.json[ConnectionStrings:Default], " +
                    "environment variable LOL_DB_CONNECTIONSTRING, " +
                    "See README.md for setup instructions."
                );

            return new MySqlConnection(Secrets.DatabaseConnectionStringV2);
        }
    }
}
