using MySqlConnector;

namespace RiotProxy.Infrastructure.Database;

public interface IDbConnectionFactory
{
    MySqlConnection CreateConnection();

    /// <summary>
    /// Creates, opens, and configures a connection with the session time zone set to UTC.
    /// This ensures TIMESTAMP values are read correctly as UTC.
    /// </summary>
    Task<MySqlConnection> CreateOpenConnectionAsync();
}
