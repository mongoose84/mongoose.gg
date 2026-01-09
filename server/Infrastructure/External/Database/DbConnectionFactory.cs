using MySqlConnector; 

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is not configured. " +
                    "Please set one of: " +
                    "appsettings.json[ConnectionStrings:Default], " +
                    "environment variable LOL_DB_CONNECTIONSTRING, " +
                    "or local file DatabaseSecret.txt. " +
                    "See README.md for setup instructions."
                );
            }

            return new MySqlConnection(Secrets.DatabaseConnectionString);
        }
    }
}