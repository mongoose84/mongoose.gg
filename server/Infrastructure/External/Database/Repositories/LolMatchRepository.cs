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

            const string sql = "INSERT INTO LolMatch (MatchId, InfoFetched, GameMode, QueueId, GameEndTimestamp) VALUES (@matchId, @infoFetched, @gameMode, @queueId, @endTs)";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@gameMode", match.GameMode ?? (object)DBNull.Value),
                ("@queueId", match.QueueId ?? (object)DBNull.Value),
                ("@endTs", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }

        public async Task UpdateMatchAsync(LolMatch match)
        {
            const string sql = "UPDATE LolMatch SET InfoFetched = @infoFetched, GameMode = @gameMode, QueueId = @queueId, DurationSeconds = @durationSeconds, GameEndTimestamp = @gameEndTimestamp WHERE MatchId = @matchId";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@gameMode", match.GameMode),
                ("@queueId", match.QueueId ?? (object)DBNull.Value),
                ("@durationSeconds", match.DurationSeconds),
                ("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }

        public async Task UpdateMatchQueueIdTimestampAndDurationAsync(string matchId, int? queueId, DateTime gameEndTimestamp, long durationSeconds)
        {
            const string sql = "UPDATE LolMatch SET QueueId = @queueId, GameEndTimestamp = @gameEndTimestamp, DurationSeconds = @durationSeconds WHERE MatchId = @matchId";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", matchId),
                ("@queueId", queueId ?? (object)DBNull.Value),
                ("@gameEndTimestamp", gameEndTimestamp == DateTime.MinValue ? DBNull.Value : gameEndTimestamp),
                ("@durationSeconds", durationSeconds));
        }

        internal async Task<IList<LolMatch>> GetUnprocessedMatchesAsync()
        {
            const string sql = "SELECT MatchId, InfoFetched, GameMode, QueueId, GameEndTimestamp FROM LolMatch WHERE InfoFetched = FALSE";
            return await ExecuteListAsync(sql, r => new LolMatch
            {
                MatchId = r.GetString(0),
                InfoFetched = r.GetBoolean(1),
                GameMode = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                QueueId = r.IsDBNull(3) ? null : r.GetInt32(3),
                GameEndTimestamp = r.IsDBNull(4) ? DateTime.MinValue : r.GetDateTime(4)
            });
        }

        internal async Task<IList<LolMatch>> GetProcessedMatchesMissingMetadataAsync()
        {
            const string sql = "SELECT MatchId, InfoFetched, GameMode, QueueId, DurationSeconds, GameEndTimestamp FROM LolMatch WHERE InfoFetched = TRUE AND (QueueId IS NULL OR GameEndTimestamp IS NULL)";
            return await ExecuteListAsync(sql, r => new LolMatch
            {
                MatchId = r.GetString(0),
                InfoFetched = r.GetBoolean(1),
                GameMode = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                QueueId = r.IsDBNull(3) ? null : r.GetInt32(3),
                DurationSeconds = r.IsDBNull(4) ? 0L : r.GetInt64(4),
                GameEndTimestamp = r.IsDBNull(5) ? DateTime.MinValue : r.GetDateTime(5)
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
                cmd.CommandText = $"SELECT MatchId, InfoFetched, GameMode, QueueId FROM `LolMatch` WHERE MatchId IN ({string.Join(",", parameterNames)})";

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
                        GameMode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        QueueId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
                    });
                }
                return matches;
            });
        }

        public async Task AddMatchIfNotExistsAsync(LolMatch match)
        {
            if (string.IsNullOrWhiteSpace(match.MatchId))
                throw new ArgumentException("MatchId cannot be null or empty", nameof(match));

            const string sql = "INSERT IGNORE INTO LolMatch (MatchId, InfoFetched, GameMode, QueueId, DurationSeconds, GameEndTimestamp) " +
                               "VALUES (@matchId, @infoFetched, @gameMode, @queueId, @durationSeconds, @gameEndTimestamp)";
            await ExecuteNonQueryAsync(sql,
                ("@matchId", match.MatchId),
                ("@infoFetched", match.InfoFetched),
                ("@gameMode", match.GameMode ?? (object)DBNull.Value),
                ("@queueId", match.QueueId ?? (object)DBNull.Value),
                ("@durationSeconds", match.DurationSeconds),
                ("@gameEndTimestamp", match.GameEndTimestamp == DateTime.MinValue ? DBNull.Value : match.GameEndTimestamp));
        }
    }
}