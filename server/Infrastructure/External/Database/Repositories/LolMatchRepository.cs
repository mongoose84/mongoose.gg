using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchRepository : RepositoryBase
    {
        public LolMatchRepository(IDbConnectionFactory factory) : base(factory)
        {
        }

        public async Task AddMatchAsync(LolMatch match)
        {
            if (string.IsNullOrWhiteSpace(match.MatchId))
                throw new ArgumentException("MatchId cannot be null or empty", nameof(match));

            const string sql = "INSERT INTO LolMatch (MatchId, InfoFetched, GameMode, GameEndTimestamp) VALUES (@matchId, @infoFetched, @gameMode, @endTs)";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@gameMode", match.GameMode ?? (object)DBNull.Value),
                ("@endTs", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }

        public async Task UpdateMatchAsync(LolMatch match)
        {
            const string sql = "UPDATE LolMatch SET InfoFetched = @infoFetched, DurationSeconds = @durationSeconds, GameMode = @gameMode, GameEndTimestamp = @gameEndTimestamp WHERE MatchId = @matchId";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@durationSeconds", match.DurationSeconds),
                ("@gameMode", match.GameMode),
                ("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }

        internal async Task<IList<LolMatch>> GetUnprocessedMatchesAsync()
        {
            const string sql = "SELECT MatchId, InfoFetched, GameMode, GameEndTimestamp FROM LolMatch WHERE InfoFetched = FALSE";
            return await ExecuteListAsync(sql, r => new LolMatch
            {
                MatchId = r.GetString(0),
                InfoFetched = r.GetBoolean(1),
                GameMode = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                GameEndTimestamp = r.IsDBNull(3) ? DateTime.MinValue : r.GetDateTime(3)
            });
        }

        public async Task<IList<LolMatch>> GetExistingMatchesAsync(IList<string> matchIds)
        {
            if (matchIds == null || matchIds.Count == 0)
                return new List<LolMatch>();

            const int maxBatchSize = 1000;
            if (matchIds.Count > maxBatchSize)
            {
                Console.WriteLine($"Warning: GetExistingMatchesAsync called with {matchIds.Count} IDs. Consider batching.");
            }

            return await ExecuteWithConnectionAsync(async (conn, cmd) =>
            {
                var parameterNames = matchIds.Select((_, index) => $"@id{index}").ToList();
                cmd.CommandText = $"SELECT MatchId, InfoFetched, GameMode FROM `LolMatch` WHERE MatchId IN ({string.Join(",", parameterNames)})";

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
            });
        }

        public async Task AddMatchIfNotExistsAsync(LolMatch match)
        {
            if (string.IsNullOrWhiteSpace(match.MatchId))
                throw new ArgumentException("MatchId cannot be null or empty", nameof(match));

            const string sql = "INSERT IGNORE INTO LolMatch (MatchId, InfoFetched, GameMode, DurationSeconds, GameEndTimestamp) " +
                               "VALUES (@matchId, @infoFetched, @gameMode, @durationSeconds, @gameEndTimestamp)";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@gameMode", match.GameMode ?? (object)DBNull.Value),
                ("@durationSeconds", match.DurationSeconds),
                ("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }
    }
}