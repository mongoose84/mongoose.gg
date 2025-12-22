using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class GamerRepository
    {
        private readonly IDbConnectionFactory _factory;

        public GamerRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IList<Gamer>> GetAllGamersAsync()
        {
            var gamers = new List<Gamer>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked FROM Gamer";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gamers.Add(new Gamer
                {
                    Puuid = reader.GetString(0),
                    GamerName = reader.GetString(1),
                    Tagline = reader.GetString(2),
                    IconId = reader.GetInt32(3),
                    Level = reader.GetInt64(4),
                    LastChecked = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                });
            }
            return gamers;
        }

        public async Task<bool> CreateGamerAsync(string puuid, string gamerName, string tagLine, int iconId, long level)
        {
            Console.WriteLine($"Creating gamer {gamerName}#{tagLine} in database...");

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
            INSERT IGNORE INTO Gamer (Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked)          
                VALUES (@puuid, @gamerName, @tagLine, @iconId, @level, @lastChecked);
                SELECT ROW_COUNT();             
            ";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@gamerName", gamerName);
            cmd.Parameters.AddWithValue("@iconId", iconId);
            cmd.Parameters.AddWithValue("@level", level);
            cmd.Parameters.AddWithValue("@tagLine", tagLine);
            cmd.Parameters.AddWithValue("@lastChecked", DateTime.MinValue);
            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return false;

            return true;
        }

        public async Task<bool> UpdateGamerAsync(Gamer gamer)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"UPDATE Gamer 
                                 SET ProfileIconId = @iconId, SummonerLevel = @level, LastChecked = @lastChecked
                                 WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@iconId", gamer.IconId);
            cmd.Parameters.AddWithValue("@level", gamer.Level);
            cmd.Parameters.AddWithValue("@lastChecked", gamer.LastChecked);
            cmd.Parameters.AddWithValue("@puuid", gamer.Puuid);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Gamer?> GetByPuuIdAsync(string puuid)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked FROM Gamer WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Gamer
                {
                    Puuid = reader.GetString(0),
                    GamerName = reader.GetString(1),
                    Tagline = reader.GetString(2),
                    IconId = reader.GetInt32(3),
                    Level = reader.GetInt64(4),
                    LastChecked = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                };
            }
            return null;
        }
    }
}