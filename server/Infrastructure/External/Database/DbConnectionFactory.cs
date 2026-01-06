using MySqlConnector; 

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection() => new MySqlConnection(Secrets.DatabaseConnectionString);
    }
}