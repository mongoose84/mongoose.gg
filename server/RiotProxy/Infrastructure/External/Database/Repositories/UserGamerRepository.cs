using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class UserGamerRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserGamerRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<bool> LinkGamerToUserAsync(int userId, string puuid)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "INSERT IGNORE INTO UserGamer (UserId, PuuId) VALUES (@userId, @puuid)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return true; // Always return true as INSERT IGNORE won't fail on duplicates
        }

        public async Task<IList<string>> GetGamersPuuidByUserIdAsync(int userId)
        {
            var puuids = new List<string>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT PuuId FROM UserGamer WHERE UserId = @userId";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                puuids.Add(reader.GetString(0));
            }
            return puuids;
        }
    }
}