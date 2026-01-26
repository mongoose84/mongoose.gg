using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString))
                throw new InvalidOperationException(
                    "Database connection string is not configured. " +
                    "Please set one of: " +
                    "appsettings.json[ConnectionStrings:Default], " +
                    "environment variable Database_test or Database_production, " +
                    "See README.md for setup instructions."
                );

            return new MySqlConnection(Secrets.DatabaseConnectionString);
        }

        /// <inheritdoc />
        public async Task<MySqlConnection> CreateOpenConnectionAsync()
        {
            var conn = CreateConnection();
            await conn.OpenAsync();

            // Explicitly set session time zone to UTC to ensure TIMESTAMP values
            // are returned as UTC, regardless of the server's default time zone.
            await using var cmd = new MySqlCommand("SET time_zone = '+00:00'", conn);
            await cmd.ExecuteNonQueryAsync();

            return conn;
        }
    }
}
