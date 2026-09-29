using MySqlConnector;
using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Per-match rows for the Solo page (features/solo-trends.spec.md). One query per range; the rules
/// that read the rows live in <c>Core/Services/Solo</c>.
/// </summary>
public class SoloTrendsRepository : RepositoryBase, ISoloTrendsRepository
{
    private readonly IQueryFilterBuilder _filterBuilder;

    public SoloTrendsRepository(IDbConnectionFactory factory, IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SoloMatchRow>> GetMatchRowsAsync(IReadOnlyList<string> puuids, string queueType, SoloRange range)
    {
        if (puuids.Count == 0) return Array.Empty<SoloMatchRow>();

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var (queuePredicate, queueParams) = BuildQueuePredicate(queueType);

        var seasonFilter = string.Empty;
        TimeRangeFilter? season = null;
        if (range == SoloRange.Season)
        {
            season = await _filterBuilder.ResolveTimeRangeAsync("current_season");
            seasonFilter = _filterBuilder.BuildTimeRangeFilter(season);
        }

        var limit = SoloScope.RangeLimit(range);
        var limitClause = limit.HasValue ? "LIMIT @limit" : string.Empty;

        // Team kills come from a correlated subquery on idx_match_id; a grouped derived table would scan
        // every participant row.
        var sql = $@"
            SELECT
                p.match_id,
                m.game_start_time,
                m.game_duration_sec,
                m.queue_id,
                COALESCE(p.role, 'UNKNOWN') AS role,
                p.champion_id,
                p.champion_name,
                p.win,
                p.deaths,
                p.creep_score,
                pc15.gold_diff_vs_lane,
                po.dragons_participated,
                tobj.dragons_taken,
                pm.vision_per_min,
                pm.kill_participation_pct,
                (SELECT COALESCE(SUM(t.kills), 0) FROM participants t
                 WHERE t.match_id = p.match_id AND t.team_id = p.team_id) AS team_kills,
                p.lp_after,
                p.tier_after,
                p.rank_after
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_checkpoints pc15 ON pc15.participant_id = p.id AND pc15.minute_mark = 15
            LEFT JOIN participant_objectives po ON po.participant_id = p.id
            LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            AND {queuePredicate}
            {seasonFilter}
            ORDER BY m.game_start_time DESC
            {limitClause}";

        var rows = new List<SoloMatchRow>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams.Concat(queueParams))
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            if (season != null) _filterBuilder.AddTimeRangeParameters(cmd, season);
            if (limit.HasValue) cmd.Parameters.AddWithValue("@limit", limit.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new SoloMatchRow(
                    MatchId: reader.GetString(0),
                    GameStartTime: reader.GetInt64(1),
                    DurationSec: reader.GetInt32(2),
                    QueueId: reader.GetInt32(3),
                    Role: reader.GetString(4),
                    ChampionId: reader.GetInt32(5),
                    ChampionName: reader.GetString(6),
                    Win: reader.GetBoolean(7),
                    Deaths: reader.GetInt32(8),
                    CreepScore: reader.GetInt32(9),
                    GoldDiffAt15: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    DragonsParticipated: reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    TeamDragons: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    VisionPerMin: reader.IsDBNull(13) ? null : (double)reader.GetDecimal(13),
                    KillParticipationPct: reader.IsDBNull(14) ? null : (double)reader.GetDecimal(14),
                    TeamKills: Convert.ToInt32(reader.GetValue(15)),
                    LpAfter: reader.IsDBNull(16) ? null : reader.GetInt32(16),
                    TierAfter: reader.IsDBNull(17) ? null : reader.GetString(17),
                    RankAfter: reader.IsDBNull(18) ? null : reader.GetString(18)));
            }
            return 0;
        });

        // Read newest first so LIMIT keeps the most recent matches; the rules want oldest first.
        rows.Reverse();
        return rows;
    }

    /// <inheritdoc />
    public async Task<SoloQueueCounts> GetSeasonQueueCountsAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0) return new SoloQueueCounts(0, 0);

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var season = await _filterBuilder.ResolveTimeRangeAsync("current_season");
        var seasonFilter = _filterBuilder.BuildTimeRangeFilter(season);

        var sql = $@"
            SELECT
                COALESCE(SUM(m.queue_id = 420), 0),
                COALESCE(SUM(m.queue_id = 440), 0)
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            AND m.queue_id IN (420, 440)
            {seasonFilter}";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, season);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return new SoloQueueCounts(0, 0);
            return new SoloQueueCounts(Convert.ToInt32(reader.GetValue(0)), Convert.ToInt32(reader.GetValue(1)));
        });
    }

    /// <summary>
    /// The Solo queue scope as a parameterized predicate. "all" means the Summoner's Rift set here, not
    /// every queue as in <see cref="IQueryFilterBuilder.BuildQueueFilter"/>, so ARAM and Arena stay out.
    /// </summary>
    private static (string Predicate, List<(string name, object? value)> Parameters) BuildQueuePredicate(string queueType)
    {
        IReadOnlyList<int> queueIds = queueType switch
        {
            SoloScope.RankedSolo => [420],
            SoloScope.RankedFlex => [440],
            _ => GameConstants.SummonersRiftQueueIds
        };

        var parameters = queueIds.Select((id, i) => ($"@queue{i}", (object?)id)).ToList();
        return ($"m.queue_id IN ({string.Join(", ", parameters.Select(p => p.Item1))})", parameters);
    }
}
