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

        public async Task<IList<Gamer>> GetGamersByUserIdAsync(int userId)
        {
            var gamers = new List<Gamer>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT Puuid, UserId, GamerName, TagLine, ProfileIconId, SummonerLevel, Wins, Losses, LastChecked FROM Gamer WHERE UserId = @userId";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gamers.Add(new Gamer
                {
                    Puuid = reader.GetString(0),
                    UserId = reader.GetInt32(1),
                    GamerName = reader.GetString(2),
                    Tagline = reader.GetString(3),
                    IconId = reader.GetInt32(4),
                    Level = reader.GetInt64(5),
                    Wins = reader.GetInt32(6),
                    Losses = reader.GetInt32(7),
                    LastChecked = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8)
                });
            }
            return gamers;
        }

        public async Task<IList<Gamer>> GetAllGamersAsync()
        {
            var gamers = new List<Gamer>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT Puuid, UserId, GamerName, TagLine, ProfileIconId, SummonerLevel, Wins, Losses, LastChecked FROM Gamer";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gamers.Add(new Gamer
                {
                    Puuid = reader.GetString(0),
                    UserId = reader.GetInt32(1),
                    GamerName = reader.GetString(2),
                    Tagline = reader.GetString(3),
                    IconId = reader.GetInt32(4),
                    Level = reader.GetInt64(5),
                    Wins = reader.GetInt32(6),
                    Losses = reader.GetInt32(7),
                    LastChecked = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8)
                });
            }
            return gamers;
        }

        public async Task<bool> CreateGamerAsync(int userId, string puuid, string gamerName, string tagLine, int iconId, long level)
        {
            Console.WriteLine($"Creating gamer {gamerName}#{tagLine} in database...");

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO Gamer (UserId, Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked)          
                VALUES (@userId, @puuid, @gamerName, @tagLine, @iconId, @level, @lastChecked);
                SELECT LAST_INSERT_ID();             
            ";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
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
                                 SET Wins = @wins, Losses = @losses, ProfileIconId = @iconId, SummonerLevel = @level, LastChecked = @lastChecked
                                 WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@wins", gamer.Wins);
            cmd.Parameters.AddWithValue("@losses", gamer.Losses);
            cmd.Parameters.AddWithValue("@iconId", gamer.IconId);
            cmd.Parameters.AddWithValue("@level", gamer.Level);
            cmd.Parameters.AddWithValue("@lastChecked", gamer.LastChecked);
            cmd.Parameters.AddWithValue("@puuid", gamer.Puuid);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}