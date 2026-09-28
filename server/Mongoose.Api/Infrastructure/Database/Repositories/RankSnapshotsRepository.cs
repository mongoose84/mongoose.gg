using MySqlConnector;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Rank snapshots (League-v4 readings per ranked queue) and the riot_accounts.rank_checked_at
/// bookkeeping the snapshot job polls by.
/// </summary>
public class RankSnapshotsRepository : RepositoryBase, IRankSnapshotsRepository
{
    private const string Columns =
        "id, puuid, queue_id, tier, division, lp, wins, losses, captured_at, window_start_at, source, status, match_id";

    public RankSnapshotsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<RankSnapshotRecord?> GetLatestAsync(string puuid, int queueId)
    {
        const string sql = $@"SELECT {Columns}
            FROM rank_snapshots
            WHERE puuid = @puuid AND queue_id = @queue_id
            ORDER BY captured_at DESC, id DESC
            LIMIT 1";

        return ExecuteSingleAsync(sql, Map, ("@puuid", puuid), ("@queue_id", queueId));
    }

    public async Task<long> InsertAsync(RankSnapshotRecord snapshot)
    {
        const string sql = @"INSERT INTO rank_snapshots
            (puuid, queue_id, tier, division, lp, wins, losses, captured_at, window_start_at, source, status, match_id)
            VALUES (@puuid, @queue_id, @tier, @division, @lp, @wins, @losses, @captured_at, @window_start_at, @source, @status, @match_id);
            SELECT LAST_INSERT_ID();";

        var id = await ExecuteScalarAsync<long>(sql,
            ("@puuid", snapshot.Puuid),
            ("@queue_id", snapshot.QueueId),
            ("@tier", snapshot.Tier),
            ("@division", snapshot.Division),
            ("@lp", snapshot.Lp),
            ("@wins", snapshot.Wins),
            ("@losses", snapshot.Losses),
            ("@captured_at", snapshot.CapturedAt),
            ("@window_start_at", snapshot.WindowStartAt),
            ("@source", snapshot.Source),
            ("@status", snapshot.Status),
            ("@match_id", snapshot.MatchId));
        snapshot.Id = id;
        return id;
    }

    public Task<IList<RankSnapshotRecord>> GetPendingAsync(string? puuid, int limit)
    {
        const string sql = $@"SELECT {Columns}
            FROM rank_snapshots
            WHERE status = 'pending'
            AND (@puuid IS NULL OR puuid = @puuid)
            ORDER BY captured_at, id
            LIMIT @limit";

        return ExecuteListAsync(sql, Map, ("@puuid", puuid), ("@limit", limit));
    }

    public Task<IList<string>> GetMatchIdsEndedBetweenAsync(string puuid, int queueId, long fromMs, long toMs)
    {
        const string sql = @"SELECT m.match_id
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
            AND m.queue_id = @queue_id
            AND m.game_duration_sec >= @min_duration
            AND m.game_start_time + m.game_duration_sec * 1000 BETWEEN @from_ms AND @to_ms
            ORDER BY m.game_start_time";

        return ExecuteListAsync(sql, r => r.GetString(0),
            ("@puuid", puuid),
            ("@queue_id", queueId),
            ("@min_duration", MinValidGameDurationSec),
            ("@from_ms", fromMs),
            ("@to_ms", toMs));
    }

    public Task UpdateStatusAsync(long id, string status, string? matchId)
    {
        const string sql = "UPDATE rank_snapshots SET status = @status, match_id = @match_id WHERE id = @id";
        return ExecuteNonQueryAsync(sql, ("@id", id), ("@status", status), ("@match_id", matchId));
    }

    public Task<IList<string>> GetDueAccountPuuidsAsync(DateTime activeSinceUtc, DateTime checkedBeforeUtc, int limit)
    {
        const string sql = @"SELECT ra.puuid
            FROM riot_accounts ra
            WHERE (ra.rank_checked_at IS NULL OR ra.rank_checked_at < @checked_before)
            AND EXISTS (
                SELECT 1
                FROM user_riot_accounts ura
                INNER JOIN users u ON u.user_id = ura.user_id
                WHERE ura.puuid = ra.puuid
                AND u.is_active = TRUE
                AND u.last_login_at >= @active_since
            )
            ORDER BY ra.rank_checked_at IS NOT NULL, ra.rank_checked_at
            LIMIT @limit";

        return ExecuteListAsync(sql, r => r.GetString(0),
            ("@active_since", activeSinceUtc),
            ("@checked_before", checkedBeforeUtc),
            ("@limit", limit));
    }

    public Task MarkRankCheckedAsync(string puuid, DateTime checkedAtUtc)
    {
        const string sql = "UPDATE riot_accounts SET rank_checked_at = @checked_at WHERE puuid = @puuid";
        return ExecuteNonQueryAsync(sql, ("@puuid", puuid), ("@checked_at", checkedAtUtc));
    }

    public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc)
    {
        // The newest snapshot per account and queue stays: the next reading is compared with it
        const string sql = @"DELETE rs FROM rank_snapshots rs
            INNER JOIN (
                SELECT puuid, queue_id, MAX(id) AS newest_id
                FROM rank_snapshots
                GROUP BY puuid, queue_id
            ) newest ON newest.puuid = rs.puuid AND newest.queue_id = rs.queue_id
            WHERE rs.captured_at < @cutoff
            AND rs.id <> newest.newest_id";

        return ExecuteNonQueryAsync(sql, ("@cutoff", cutoffUtc));
    }

    private static RankSnapshotRecord Map(MySqlDataReader r) => new()
    {
        Id = Convert.ToInt64(r.GetValue(0)),
        Puuid = r.GetString(1),
        QueueId = r.GetInt32(2),
        Tier = r.GetString(3),
        Division = r.IsDBNull(4) ? null : r.GetString(4),
        Lp = r.GetInt32(5),
        Wins = r.GetInt32(6),
        Losses = r.GetInt32(7),
        CapturedAt = r.GetDateTimeUtc(8),
        WindowStartAt = r.GetDateTimeUtcOrNull(9),
        Source = r.GetString(10),
        Status = r.GetString(11),
        MatchId = r.IsDBNull(12) ? null : r.GetString(12)
    };
}
