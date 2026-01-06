using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class GamerRepository : RepositoryBase
    {
        public GamerRepository(IDbConnectionFactory factory) : base(factory)
        {
        }

        public async Task<IList<Gamer>> GetAllGamersAsync()
        {
            const string sql = "SELECT Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked FROM Gamer";
            return await ExecuteListAsync(sql, r => new Gamer
            {
                Puuid = r.GetString(0),
                GamerName = r.GetString(1),
                Tagline = r.GetString(2),
                IconId = r.GetInt32(3),
                Level = r.GetInt64(4),
                LastChecked = r.IsDBNull(5) ? DateTime.MinValue : r.GetDateTime(5)
            });
        }

        public async Task<bool> CreateGamerAsync(string puuid, string gamerName, string tagLine, int iconId, long level)
        {
            Console.WriteLine($"Creating gamer {gamerName}#{tagLine} in database...");

            const string sql = @"
                INSERT IGNORE INTO Gamer (Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked)          
                VALUES (@puuid, @gamerName, @tagLine, @iconId, @level, @lastChecked);
                SELECT ROW_COUNT();             
            ";
            
            var result = await ExecuteScalarAsync<long>(sql,
                ("@puuid", puuid),
                ("@gamerName", gamerName),
                ("@tagLine", tagLine),
                ("@iconId", iconId),
                ("@level", level),
                ("@lastChecked", DateTime.MinValue));

            return result > 0;
        }

        public async Task<bool> UpdateGamerAsync(Gamer gamer)
        {
            const string sql = @"UPDATE Gamer 
                                 SET ProfileIconId = @iconId, SummonerLevel = @level, LastChecked = @lastChecked
                                 WHERE Puuid = @puuid";
            var rows = await ExecuteNonQueryAsync(sql,
                ("@iconId", gamer.IconId),
                ("@level", gamer.Level),
                ("@lastChecked", gamer.LastChecked),
                ("@puuid", gamer.Puuid));
            return rows > 0;
        }

        public async Task<Gamer?> GetByPuuIdAsync(string puuid)
        {
            const string sql = "SELECT Puuid, GamerName, TagLine, ProfileIconId, SummonerLevel, LastChecked FROM Gamer WHERE Puuid = @puuid";
            return await ExecuteSingleAsync(sql, r => new Gamer
            {
                Puuid = r.GetString(0),
                GamerName = r.GetString(1),
                Tagline = r.GetString(2),
                IconId = r.GetInt32(3),
                Level = r.GetInt64(4),
                LastChecked = r.IsDBNull(5) ? DateTime.MinValue : r.GetDateTime(5)
            }, ("@puuid", puuid));
        }
    }
}