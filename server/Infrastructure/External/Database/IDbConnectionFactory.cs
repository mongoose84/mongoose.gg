using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}