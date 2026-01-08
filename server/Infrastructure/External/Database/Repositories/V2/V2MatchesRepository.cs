using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2MatchesRepository : RepositoryBase
{
    public V2MatchesRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2Match match)
    {
        const string sql = @"INSERT INTO matches (match_id, queue_id, game_duration_sec, game_start_time, patch_version, season_code, created_at)
            VALUES (@match_id, @queue_id, @game_duration_sec, @game_start_time, @patch_version, @season_code, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                queue_id = new.queue_id,
                game_duration_sec = new.game_duration_sec,
                game_start_time = new.game_start_time,
                patch_version = new.patch_version,
                season_code = new.season_code;";

        return ExecuteNonQueryAsync(sql,
            ("@match_id", match.MatchId),
            ("@queue_id", match.QueueId),
            ("@game_duration_sec", match.GameDurationSec),
            ("@game_start_time", match.GameStartTime),
            ("@patch_version", match.PatchVersion),
            ("@season_code", match.SeasonCode ?? (object)DBNull.Value),
            ("@created_at", match.CreatedAt == default ? DateTime.UtcNow : match.CreatedAt));
    }

    public Task<IList<V2Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit)
    {
        var sql = @"SELECT m.* FROM matches m
            INNER JOIN participants p ON p.match_id = m.match_id
            WHERE p.puuid = @puuid";
        if (queueId.HasValue)
        {
            sql += " AND m.queue_id = @queue_id";
        }
        sql += " ORDER BY m.game_start_time DESC LIMIT @limit";

        var parameters = new List<(string, object?)>
        {
            ("@puuid", puuid),
            ("@limit", limit)
        };
        if (queueId.HasValue)
        {
            parameters.Add(("@queue_id", queueId.Value));
        }

        return ExecuteListAsync(sql, Map, parameters.ToArray());
    }

    private static V2Match Map(MySqlDataReader r) => new()
    {
        MatchId = r.GetString(0),
        QueueId = r.GetInt32(1),
        GameDurationSec = r.GetInt32(2),
        GameStartTime = r.GetInt64(3),
        PatchVersion = r.GetString(4),
        SeasonCode = r.IsDBNull(5) ? null : r.GetString(5),
        CreatedAt = r.GetDateTime(6)
    };
}
