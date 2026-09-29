using MySqlConnector;
using Mongoose.Api.Core;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services;
using Mongoose.Api.Core.Services.Solo;
using Mongoose.Api.Core.ValueObjects;

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
        // Same placeholders on the window's alias; the parameters are already in puuidParams
        var (previousRankPuuidPredicate, _) = BuildStringInClause("p2.puuid", puuids, "puuid");
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
                p.rank_after,
                prev_rank.prev_lp_after,
                prev_rank.prev_tier_after,
                prev_rank.prev_rank_after
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_checkpoints pc15 ON pc15.participant_id = p.id AND pc15.minute_mark = 15
            LEFT JOIN participant_objectives po ON po.participant_id = p.id
            LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN ({PreviousRankSql.For(previousRankPuuidPredicate)}) prev_rank ON prev_rank.participant_id = p.id
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
                var win = reader.GetBoolean(7);
                var rankAfter = ReadRank(reader, 16);
                rows.Add(new SoloMatchRow(
                    MatchId: reader.GetString(0),
                    GameStartTime: reader.GetInt64(1),
                    DurationSec: reader.GetInt32(2),
                    QueueId: reader.GetInt32(3),
                    Role: reader.GetString(4),
                    ChampionId: reader.GetInt32(5),
                    ChampionName: reader.GetString(6),
                    Win: win,
                    Deaths: reader.GetInt32(8),
                    CreepScore: reader.GetInt32(9),
                    GoldDiffAt15: reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    DragonsParticipated: reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    TeamDragons: reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    VisionPerMin: reader.IsDBNull(13) ? null : (double)reader.GetDecimal(13),
                    KillParticipationPct: reader.IsDBNull(14) ? null : (double)reader.GetDecimal(14),
                    TeamKills: Convert.ToInt32(reader.GetValue(15)),
                    LpAfter: rankAfter.Lp,
                    TierAfter: rankAfter.Tier,
                    RankAfter: rankAfter.Division,
                    LpChange: LpChangeCalculator.Compute(ReadRank(reader, 19), rankAfter, win)));
            }
            return 0;
        });

        // Read newest first so LIMIT keeps the most recent matches; the rules want oldest first.
        rows.Reverse();
        return rows;
    }

    /// <inheritdoc />
    public async Task<SoloDeathData> GetDeathDataAsync(IReadOnlyList<string> puuids, IReadOnlyList<string> matchIds)
    {
        if (puuids.Count == 0 || matchIds.Count == 0) return new SoloDeathData([], [], []);

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var (matchPredicate, matchParams) = BuildStringInClause("p.match_id", matchIds, "match");
        var (objectiveMatchPredicate, _) = BuildStringInClause("o.match_id", matchIds, "match");

        var deathsSql = $@"
            SELECT p.match_id, p.team_id, COALESCE(p.role, 'UNKNOWN'), p.riot_participant_id,
                d.position_x, d.position_y, d.timestamp_sec, d.killer_participant_id,
                d.assisting_participant_ids, d.allies_nearby
            FROM participant_death_events d
            INNER JOIN participants p ON p.id = d.participant_id
            WHERE {puuidPredicate} AND {matchPredicate}";

        var participantsSql = $@"
            SELECT p.match_id, p.riot_participant_id, p.team_id, COALESCE(p.role, 'UNKNOWN')
            FROM participants p
            WHERE {matchPredicate} AND p.riot_participant_id IS NOT NULL";

        var objectivesSql = $@"
            SELECT o.match_id, o.team_id, o.type, o.timestamp_sec
            FROM match_objective_events o
            WHERE {objectiveMatchPredicate}";

        var deaths = await ExecuteListAsync(deathsSql, r => new SoloDeathRow(
                r.GetString(0),
                r.GetInt32(1),
                r.GetString(2),
                r.IsDBNull(3) ? null : r.GetInt32(3),
                r.GetInt32(4),
                r.GetInt32(5),
                r.IsDBNull(6) ? null : r.GetInt32(6),
                r.IsDBNull(7) ? null : r.GetInt32(7),
                ParseIds(r.IsDBNull(8) ? null : r.GetString(8)),
                r.IsDBNull(9) ? null : r.GetInt32(9)),
            puuidParams.Concat(matchParams).ToArray());

        var participants = await ExecuteListAsync(participantsSql,
            r => new MatchParticipantRole(r.GetString(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3)),
            matchParams.ToArray());

        var objectives = await ExecuteListAsync(objectivesSql,
            r => new SoloObjectiveRow(r.GetString(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3)),
            matchParams.ToArray());

        return new SoloDeathData(deaths.ToList(), participants.ToList(), objectives.ToList());
    }

    /// <summary>"8,9" to [8, 9]; empty for null or blank.</summary>
    private static IReadOnlyList<int> ParseIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDeathDetailPendingAccountsAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0) return [];

        var (puuidPredicate, puuidParams) = BuildStringInClause("ra.puuid", puuids, "puuid");
        var sql = $@"SELECT ra.puuid FROM riot_accounts ra
            WHERE {puuidPredicate}
            AND ra.death_detail_backfilled_at IS NULL";

        var pending = await ExecuteListAsync(sql, r => r.GetString(0), puuidParams.ToArray());
        return pending.ToList();
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

    /// <summary>Reads lp_after, tier_after and rank_after starting at <paramref name="lpOrdinal"/>.</summary>
    private static RankSnapshot ReadRank(MySqlDataReader reader, int lpOrdinal) => new(
        reader.IsDBNull(lpOrdinal + 1) ? null : reader.GetString(lpOrdinal + 1),
        reader.IsDBNull(lpOrdinal + 2) ? null : reader.GetString(lpOrdinal + 2),
        reader.IsDBNull(lpOrdinal) ? null : reader.GetInt32(lpOrdinal));

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
