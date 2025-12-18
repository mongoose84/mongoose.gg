using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LolMatchRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddMatchAsync(LolMatch match)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "INSERT INTO LolMatch (MatchId, InfoFetched, GameMode, GameEndTimestamp) VALUES (@matchId, @infoFetched, @gameMode, @endTs)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", match.MatchId);
            cmd.Parameters.AddWithValue("@infoFetched", match.InfoFetched);
            cmd.Parameters.AddWithValue("@gameMode", match.GameMode ?? string.Empty);
            
            // Use a sentinel date instead of NULL if column is NOT NULL
            cmd.Parameters.AddWithValue("@endTs", 
                match.GameEndTimestamp == DateTime.MinValue 
                    ? new DateTime(1970, 1, 1) // Or DateTime.UtcNow as placeholder
                    : match.GameEndTimestamp);
            
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateMatchAsync(LolMatch match)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "UPDATE LolMatch SET InfoFetched = @infoFetched, DurationSeconds = @durationSeconds, GameMode = @gameMode,  GameEndTimestamp = @gameEndTimestamp WHERE MatchId = @matchId";
            await using var cmd = new MySqlCommand(sql, conn);
    
            cmd.Parameters.AddWithValue("@matchId", match.MatchId);
            cmd.Parameters.AddWithValue("@infoFetched", match.InfoFetched);
            cmd.Parameters.AddWithValue("@durationSeconds", match.DurationSeconds);
            cmd.Parameters.AddWithValue("@gameMode", match.GameMode);
            
            cmd.Parameters.AddWithValue("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp);
            await cmd.ExecuteNonQueryAsync();
        }

        internal async Task<IList<LolMatch>> GetUnprocessedMatchesAsync()
        {
            var matches = new List<LolMatch>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT MatchId, InfoFetched, GameMode, GameEndTimestamp FROM LolMatch WHERE InfoFetched = FALSE";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matches.Add(new LolMatch
                {
                    MatchId = reader.GetString(0),
                    InfoFetched = reader.GetBoolean(1),
                    GameMode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    GameEndTimestamp = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3)
                });
            }
            return matches;
        }

        public async Task<IList<LolMatch>> GetExistingMatchesAsync(IList<string> matchIds)
        {
            if (matchIds.Count == 0) return new List<LolMatch>();

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var parameterNames = matchIds.Select((id, index) => $"@id{index}").ToList();
            var sql = $"SELECT MatchId, InfoFetched, GameMode FROM `LolMatch` WHERE MatchId IN ({string.Join(",", parameterNames)})";
            
            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < matchIds.Count; i++)
            {
                cmd.Parameters.AddWithValue(parameterNames[i], matchIds[i]);
            }
            await using var reader = await cmd.ExecuteReaderAsync();
            
            var matches = new List<LolMatch>();
            while (await reader.ReadAsync())
            {
                matches.Add(new LolMatch
                {
                    MatchId = reader.GetString(0),
                    InfoFetched = reader.GetBoolean(1),
                    GameMode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                });
            }
            
            return matches;
        }

        public async Task AddMatchIfNotExistsAsync(LolMatch match)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            const string sql = "INSERT IGNORE INTO LolMatch (MatchId, InfoFetched, GameMode, DurationSeconds, GameEndTimestamp) " +
                               "VALUES (@matchId, @infoFetched, @gameMode, @durationSeconds, @gameEndTimestamp)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", match.MatchId);
            cmd.Parameters.AddWithValue("@infoFetched", match.InfoFetched);
            cmd.Parameters.AddWithValue("@gameMode", match.GameMode ?? string.Empty);
            cmd.Parameters.AddWithValue("@durationSeconds", match.DurationSeconds);
            cmd.Parameters.AddWithValue("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}