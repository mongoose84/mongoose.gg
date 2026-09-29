using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <inheritdoc cref="IDeathDetailBackfillRepository" />
public class DeathDetailBackfillRepository : RepositoryBase, IDeathDetailBackfillRepository
{
    public DeathDetailBackfillRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<IList<string>> GetAccountsToBackfillAsync(DateTime activeSinceUtc, int limit)
    {
        // Same "active" rule as RankSnapshotJob: a linked, active user who logged in recently
        const string sql = @"SELECT ra.puuid
            FROM riot_accounts ra
            INNER JOIN user_riot_accounts ura ON ura.puuid = ra.puuid
            INNER JOIN users u ON u.user_id = ura.user_id
            WHERE ra.death_detail_backfilled_at IS NULL
            AND u.is_active = TRUE
            AND u.last_login_at >= @active_since
            GROUP BY ra.puuid
            ORDER BY MAX(u.last_login_at) DESC
            LIMIT @limit";

        return ExecuteListAsync(sql, r => r.GetString(0), ("@active_since", activeSinceUtc), ("@limit", limit));
    }

    public Task<IList<BackfillMatch>> GetRecentMatchesAsync(string puuid, int limit)
    {
        // Done: skipped, or written by DeathDetailWriter (sync or backfill), which sets the row's
        // riot_participant_id and every death's timestamp_sec
        var queueIds = string.Join(", ", GameConstants.SummonersRiftQueueIds);
        var sql = $@"SELECT
                m.match_id,
                (
                    EXISTS (SELECT 1 FROM death_detail_backfill_skips s WHERE s.match_id = m.match_id)
                    OR (
                        p.riot_participant_id IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1 FROM participants p2
                            INNER JOIN participant_death_events d ON d.participant_id = p2.id
                            WHERE p2.match_id = m.match_id AND d.timestamp_sec IS NULL)
                    )
                ) AS done
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
            AND m.queue_id IN ({queueIds})
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            ORDER BY m.game_start_time DESC
            LIMIT @limit";

        return ExecuteListAsync(sql,
            r => new BackfillMatch(r.GetString(0), Convert.ToInt64(r.GetValue(1)) != 0),
            ("@puuid", puuid), ("@limit", limit));
    }

    public Task MarkSkippedAsync(string matchId, string reason, DateTime skippedAtUtc)
    {
        const string sql = @"INSERT INTO death_detail_backfill_skips (match_id, reason, skipped_at)
            VALUES (@match_id, @reason, @skipped_at) AS new
            ON DUPLICATE KEY UPDATE reason = new.reason, skipped_at = new.skipped_at";

        return ExecuteNonQueryAsync(sql, ("@match_id", matchId), ("@reason", reason), ("@skipped_at", skippedAtUtc));
    }

    public Task MarkAccountDoneAsync(string puuid, DateTime doneAtUtc)
    {
        const string sql = "UPDATE riot_accounts SET death_detail_backfilled_at = @done_at WHERE puuid = @puuid";
        return ExecuteNonQueryAsync(sql, ("@puuid", puuid), ("@done_at", doneAtUtc));
    }

    public async Task<bool> IsSyncActiveAsync()
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM riot_accounts WHERE sync_status IN ('pending', 'syncing'))";
        return await ExecuteScalarAsync<long>(sql) != 0;
    }
}
