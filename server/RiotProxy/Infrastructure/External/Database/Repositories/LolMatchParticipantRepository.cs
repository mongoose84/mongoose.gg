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
            const string sql = "INSERT IGNORE INTO LolMatchParticipant (MatchId, Puuid, TeamId, Win, Role, TeamPosition, Lane, ChampionId, ChampionName, Kills, Deaths, Assists, DoubleKills, TripleKills, QuadraKills, PentaKills, GoldEarned, TimeBeingDeadSeconds, CreepScore) " +
                               "VALUES (@matchId, @puuid, @teamId, @win, @role, @teamPosition, @lane, @championId, @championName, @kills, @deaths, @assists, @doubleKills, @tripleKills, @quadraKills, @pentaKills, @goldEarned, @timeBeingDeadSeconds, @creepScore)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", participant.MatchId);
            cmd.Parameters.AddWithValue("@puuid", participant.PuuId);
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
            cmd.Parameters.AddWithValue("@timeBeingDeadSeconds", participant.TimeBeingDeadSeconds);
            cmd.Parameters.AddWithValue("@creepScore", participant.CreepScore);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IList<string>> GetMatchIdsForPuuidAsync(string puuId)
        {
            var matchIds = new List<string>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matchIds.Add(reader.GetString(0));
            }
            return matchIds;
        }

        internal async Task<long> GetTotalDurationPlayedByPuuidAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT SUM(DurationSeconds) FROM LolMatch WHERE MatchId IN (SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt64(result) : 0L;
        }

        internal async Task<int> GetWinsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid AND Win = TRUE";
            var wins = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return wins;
        }

        internal async Task<int> GetMatchesCountByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalMatches = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalMatches;
        }

        internal async Task<int> GetTotalAssistsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Assists) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalAssists = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalAssists;
        }

        internal async Task<int> GetTotalDeathsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Deaths) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalDeaths = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalDeaths;
        }

        internal async Task<int> GetTotalKillsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Kills) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalKills = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalKills;
        }

        internal async Task<int> GetTotalCreepScoreByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(CreepScore) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalCreepScore = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalCreepScore;
        }

        internal async Task<int> GetTotalGoldEarnedByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(GoldEarned) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalGoldEarned = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalGoldEarned;
        }

        private async Task<int> GetIntegerValueFromPuuIdAsync(string puuId, string sqlQuery)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sqlQuery, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result);
        }
    }
}