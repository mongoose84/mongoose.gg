using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchParticipantRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LolMatchParticipantRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddParticipantIfNotExistsAsync(LolMatchParticipant participant)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            const string sql = "INSERT INTO LolMatchParticipant (MatchId, Puuid, TeamId, Win, Role, TeamPosition, Lane, ChampionId, ChampionName, Kills, Deaths, Assists, DoubleKills, TripleKills, QuadraKills, PentaKills, GoldEarned, CreepScore) " +
                               "VALUES (@matchId, @puuid, @teamId, @win, @role, @teamPosition, @lane, @championId, @championName, @kills, @deaths, @assists, @doubleKills, @tripleKills, @quadraKills, @pentaKills, @goldEarned, @creepScore)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", participant.MatchId);
            cmd.Parameters.AddWithValue("@puuid", participant.Puuid);
            cmd.Parameters.AddWithValue("@teamId", participant.TeamId);
            cmd.Parameters.AddWithValue("@win", participant.Win);
            cmd.Parameters.AddWithValue("@role", participant.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@teamPosition", participant.TeamPosition ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lane", participant.Lane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@championId", participant.ChampionId);
            cmd.Parameters.AddWithValue("@championName", participant.ChampionName);
            cmd.Parameters.AddWithValue("@kills", participant.Kills);
            cmd.Parameters.AddWithValue("@deaths", participant.Deaths);
            cmd.Parameters.AddWithValue("@assists", participant.Assists);
            cmd.Parameters.AddWithValue("@doubleKills", participant.DoubleKills);
            cmd.Parameters.AddWithValue("@tripleKills", participant.TripleKills);
            cmd.Parameters.AddWithValue("@quadraKills", participant.QuadraKills);
            cmd.Parameters.AddWithValue("@pentaKills", participant.PentaKills);
            cmd.Parameters.AddWithValue("@goldEarned", participant.GoldEarned);
            cmd.Parameters.AddWithValue("@creepScore", participant.CreepScore);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IList<string>> GetMatchIdsForPuuidAsync(string puuid)
        {
            var matchIds = new List<string>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matchIds.Add(reader.GetString(0));
            }
            return matchIds;
        }
    }
}