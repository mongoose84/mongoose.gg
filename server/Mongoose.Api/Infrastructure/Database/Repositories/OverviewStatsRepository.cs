using MySqlConnector;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for overview page statistics.
/// Provides primary queue detection, last 20 games metrics, and last match data.
/// </summary>
public class OverviewStatsRepository : RepositoryBase, IOverviewStatsRepository
{
    private readonly ILogger<OverviewStatsRepository> _logger;

    // Queue priority order for tie-breaking: Ranked Solo/Duo → Ranked Flex → Normal Draft → ARAM → other
    private static readonly Dictionary<int, int> QueuePriority = new()
    {
        { 420, 1 },  // Ranked Solo/Duo (highest priority)
        { 440, 2 },  // Ranked Flex
        { 400, 3 },  // Normal Draft
        { 430, 3 },  // Normal Blind (same priority as Draft)
        { 450, 4 },  // ARAM
        { 1700, 4 }, // ARAM: Mayhem (same priority as regular ARAM)
    };

    // The current season is the latest season_code that has matches.
    private const string CurrentSeasonSubquery = @"
        SELECT ranked.season_code
        FROM (
            SELECT m2.season_code, MAX(m2.game_start_time) AS last_game_start
            FROM matches m2
            WHERE m2.season_code IS NOT NULL
            GROUP BY m2.season_code
            ORDER BY last_game_start DESC
            LIMIT 1
        ) ranked";

    // Ranked Solo/Duo and Ranked Flex
    private const string RankedQueueIds = "420, 440";

    public OverviewStatsRepository(IDbConnectionFactory factory, ILogger<OverviewStatsRepository> logger) : base(factory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines the primary queue based on match count in recent window (last 50 matches or 30 days).
    /// Returns the queue_id with highest match count, using tie-breaker order if counts are equal.
    /// </summary>
    public virtual async Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid)
        => await GetPrimaryQueueAsync([puuid]);

    public virtual async Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0)
        {
            return (420, "Ranked Solo/Duo", 0);
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var (subqueryPuuidPredicate, subqueryPuuidParams) = BuildStringInClause("p2.puuid", puuids, "puuid_sub");
        // Get match counts per queue for last 50 matches OR last 30 days (whichever gives more games)
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();

        var sql = $@"
            SELECT 
                m.queue_id,
                COUNT(*) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_duration_sec >= {MinValidGameDurationSec}
              AND (
                  m.game_start_time >= @thirty_days_ago
                  OR p.match_id IN (
                      SELECT match_id FROM (
                          SELECT p2.match_id 
                          FROM participants p2
                          INNER JOIN matches m2 ON m2.match_id = p2.match_id
                          WHERE {subqueryPuuidPredicate}
                          AND m2.game_duration_sec >= {MinValidGameDurationSec}
                          ORDER BY m2.game_start_time DESC
                          LIMIT 50
                      ) recent_matches
                  )
              )
            GROUP BY m.queue_id
            ORDER BY match_count DESC";

        var queueCounts = new List<QueueMatchCount>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            foreach (var (name, value) in subqueryPuuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.Parameters.AddWithValue("@thirty_days_ago", thirtyDaysAgo);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                queueCounts.Add(new QueueMatchCount(
                    QueueId: reader.GetInt32(0),
                    MatchCount: reader.GetInt32(1)
                ));
            }
            return 0;
        });

        if (queueCounts.Count == 0)
        {
            // Default to Ranked Solo/Duo if no matches found
            return (420, "Ranked Solo/Duo", 0);
        }

        // Find queue with highest count, using priority for tie-breaking
        var primaryQueue = queueCounts
            .OrderByDescending(q => q.MatchCount)
            .ThenBy(q => QueuePriority.GetValueOrDefault(q.QueueId, 99))
            .First();

        var label = LeagueDataHelper.GetQueueLabel(primaryQueue.QueueId);
        return (primaryQueue.QueueId, label, primaryQueue.MatchCount);
    }

    /// <summary>
    /// Gets the last 20 matches for the specified queue with win/loss and LP data.
    /// Returns newest first (index 0 = most recent).
    /// </summary>
    public virtual async Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId)
        => await GetLast20MatchesAsync([puuid], queueId);

    public virtual async Task<List<MatchResultData>> GetLast20MatchesAsync(IReadOnlyList<string> puuids, int queueId)
    {
        if (puuids.Count == 0)
        {
            return new List<MatchResultData>();
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT 
                p.match_id,
                p.win,
                p.lp_after,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_duration_sec >= {MinValidGameDurationSec}
              AND m.queue_id = @queue_id
            ORDER BY m.game_start_time DESC
            LIMIT 20";

        var matches = new List<MatchResultData>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.Parameters.AddWithValue("@queue_id", queueId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matches.Add(new MatchResultData(
                    MatchId: reader.GetString(0),
                    Win: reader.GetBoolean(1),
                    LpAfter: reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    GameStartTime: reader.GetInt64(3)
                ));
            }
            return 0;
        });

        return matches;
    }

    /// <summary>
    /// Gets the most recent match for the player across all queues.
    /// </summary>
    public virtual async Task<LastMatchData?> GetLastMatchAsync(string puuid)
        => await GetLastMatchAsync([puuid]);

    public virtual async Task<LastMatchData?> GetLastMatchAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0)
        {
            return null;
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                p.match_id,
                p.champion_id,
                p.champion_name,
                p.win,
                p.kills,
                p.deaths,
                p.assists,
                m.game_start_time,
                m.queue_id
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            ORDER BY m.game_start_time DESC
            LIMIT 1";

        LastMatchData? result = null;

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new LastMatchData(
                    MatchId: reader.GetString(0),
                    ChampionId: reader.GetInt32(1),
                    ChampionName: reader.GetString(2),
                    Win: reader.GetBoolean(3),
                    Kills: reader.GetInt32(4),
                    Deaths: reader.GetInt32(5),
                    Assists: reader.GetInt32(6),
                    GameStartTime: reader.GetInt64(7),
                    QueueId: reader.GetInt32(8)
                );
            }
            return 0;
        });

        return result;
    }

    /// <summary>
    /// Gets the most played champion for the player in the current season.
    /// Returns null when season data is unavailable or no matches exist.
    /// </summary>
    public virtual async Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid)
        => await GetMostPlayedChampionAsync([puuid]);

    public virtual async Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0)
        {
            return null;
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                p.champion_name,
                COUNT(*) AS games_played
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_duration_sec >= {MinValidGameDurationSec}
              AND m.season_code = ({CurrentSeasonSubquery})
            GROUP BY p.champion_name
            ORDER BY games_played DESC, MAX(m.game_start_time) DESC
            LIMIT 1";

        MostPlayedChampionData? result = null;

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new MostPlayedChampionData(
                    ChampionName: reader.GetString(0),
                    GamesPlayed: reader.GetInt32(1)
                );
            }

            return 0;
        });

        return result;
    }

    /// <summary>
    /// Per-champion ranked aggregates (Solo/Duo + Flex, current season) for the Overview champion pool,
    /// plus matches per champion and role so the caller can find each champion's primary role.
    /// </summary>
    public virtual async Task<ChampionPoolData> GetChampionPoolStatsAsync(IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0)
        {
            return new ChampionPoolData([], []);
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var matchFilter = $@"
            WHERE {puuidPredicate}
              AND m.game_duration_sec >= {MinValidGameDurationSec}
              AND m.queue_id IN ({RankedQueueIds})
              AND m.season_code = ({CurrentSeasonSubquery})";

        var championSql = $@"
            SELECT
                p.champion_id,
                MAX(p.champion_name) AS champion_name,
                COUNT(*) AS games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) AS wins,
                AVG(p.kills) AS avg_kills,
                AVG(p.deaths) AS avg_deaths,
                AVG(p.assists) AS avg_assists,
                AVG(p.creep_score / (m.game_duration_sec / 60.0)) AS avg_cs_per_min,
                AVG(cp15.gold_diff_vs_lane) AS avg_gold_diff_15,
                COUNT(cp15.gold_diff_vs_lane) AS gold_diff_15_samples,
                AVG(pm.deaths_pre_10) AS avg_deaths_pre_10,
                AVG(pm.vision_per_min) AS avg_vision_per_min,
                AVG(pm.damage_share_pct) AS avg_damage_share_pct,
                AVG(pm.kill_participation_pct) AS avg_kill_participation_pct,
                COUNT(pm.participant_id) AS metric_samples,
                MAX(m.game_start_time) AS last_played
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_checkpoints cp15 ON cp15.participant_id = p.id AND cp15.minute_mark = 15
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            {matchFilter}
            GROUP BY p.champion_id";

        var roleSql = $@"
            SELECT
                p.champion_id,
                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') AS role,
                COUNT(*) AS games,
                MAX(m.game_start_time) AS last_played
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            {matchFilter}
            GROUP BY p.champion_id, role";

        var champions = new List<ChampionPoolStatsData>();
        var roleCounts = new List<ChampionRoleCountData>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using (var cmd = new MySqlCommand(championSql, conn))
            {
                foreach (var (name, value) in puuidParams)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    champions.Add(new ChampionPoolStatsData(
                        ChampionId: reader.GetInt32(0),
                        ChampionName: reader.GetString(1),
                        Games: reader.GetInt32(2),
                        Wins: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        AvgKills: reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                        AvgDeaths: reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                        AvgAssists: reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                        AvgCsPerMin: reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                        AvgGoldDiff15: reader.IsDBNull(8) ? null : reader.GetDouble(8),
                        GoldDiff15Samples: reader.GetInt32(9),
                        AvgDeathsPre10: reader.IsDBNull(10) ? null : reader.GetDouble(10),
                        AvgVisionPerMin: reader.IsDBNull(11) ? null : reader.GetDouble(11),
                        AvgDamageSharePct: reader.IsDBNull(12) ? null : reader.GetDouble(12),
                        AvgKillParticipationPct: reader.IsDBNull(13) ? null : reader.GetDouble(13),
                        MetricSamples: reader.GetInt32(14),
                        LastPlayed: reader.GetInt64(15)
                    ));
                }
            }

            await using (var cmd = new MySqlCommand(roleSql, conn))
            {
                foreach (var (name, value) in puuidParams)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    roleCounts.Add(new ChampionRoleCountData(
                        ChampionId: reader.GetInt32(0),
                        Role: reader.GetString(1),
                        Games: reader.GetInt32(2),
                        LastPlayed: reader.GetInt64(3)
                    ));
                }
            }

            return 0;
        });

        return new ChampionPoolData(champions, roleCounts);
    }

    /// <summary>
    /// Gets the current LP for the player in the specified ranked queue.
    /// Returns null if no LP data is available.
    /// </summary>
    public virtual async Task<int?> GetCurrentLpAsync(string puuid, int queueId)
    {
        // Get the most recent LP from a ranked match
        const string sql = @"
            SELECT p.lp_after
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND m.queue_id = @queue_id
              AND p.lp_after IS NOT NULL
            ORDER BY m.game_start_time DESC
            LIMIT 1";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@queue_id", queueId);

            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        });
    }

    /// <summary>
    /// Returns per-PUUID session breakdown for today and the last 7 days.
    /// Uses conditional aggregation so both time windows are covered in a single stats query,
    /// plus a second query for per-champion breakdowns today.
    /// </summary>
    public virtual async Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc)
    {
        if (puuids.Count == 0)
            return new SessionStatsData(Array.Empty<PerAccountSessionData>());

        var todayStart = new DateTime(todayUtc.Year, todayUtc.Month, todayUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayStartMs = new DateTimeOffset(todayStart).ToUnixTimeMilliseconds();
        var weekStartMs = new DateTimeOffset(todayStart.AddDays(-7)).ToUnixTimeMilliseconds();

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var statsSql = $@"
            SELECT
                p.puuid,
                SUM(CASE WHEN m.game_start_time >= @today_start THEN 1 ELSE 0 END) AS games_today,
                SUM(CASE WHEN m.game_start_time >= @today_start AND p.win = 1 THEN 1 ELSE 0 END) AS wins_today,
                SUM(CASE WHEN m.game_start_time >= @today_start AND p.win = 0 THEN 1 ELSE 0 END) AS losses_today,
                AVG(CASE WHEN m.game_start_time >= @today_start THEN (p.kills + p.assists) / GREATEST(p.deaths, 1) ELSE NULL END) AS avg_kda_today,
                COUNT(*) AS games_this_week,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) AS wins_this_week,
                SUM(CASE WHEN p.win = 0 THEN 1 ELSE 0 END) AS losses_this_week,
                AVG((p.kills + p.assists) / GREATEST(p.deaths, 1)) AS avg_kda_this_week
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_start_time >= @week_start
              AND m.game_duration_sec >= {MinValidGameDurationSec}
            GROUP BY p.puuid";

        var (puuidPredicate2, puuidParams2) = BuildStringInClause("p.puuid", puuids, "puuid2");
        var championSql = $@"
            SELECT
                p.puuid,
                p.champion_name,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) AS wins,
                SUM(CASE WHEN p.win = 0 THEN 1 ELSE 0 END) AS losses,
                AVG((p.kills + p.assists) / GREATEST(p.deaths, 1)) AS avg_kda
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate2}
              AND m.game_start_time >= @today_start2
              AND m.game_duration_sec >= {MinValidGameDurationSec}
            GROUP BY p.puuid, p.champion_name";

        var statsRows = new Dictionary<string, (int GamesToday, int WinsToday, int LossesToday, double? AvgKdaToday, int GamesThisWeek, int WinsThisWeek, int LossesThisWeek, double? AvgKdaThisWeek)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(statsSql, conn);
            foreach (var (name, value) in puuidParams)
                cmd.Parameters.AddWithValue(name, value);
            cmd.Parameters.AddWithValue("@today_start", todayStartMs);
            cmd.Parameters.AddWithValue("@week_start", weekStartMs);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var puuid = reader.GetString(0);
                statsRows[puuid] = (
                    GamesToday: reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                    WinsToday: reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                    LossesToday: reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                    AvgKdaToday: reader.IsDBNull(4) ? (double?)null : reader.GetDouble(4),
                    GamesThisWeek: reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                    WinsThisWeek: reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6)),
                    LossesThisWeek: reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetValue(7)),
                    AvgKdaThisWeek: reader.IsDBNull(8) ? (double?)null : reader.GetDouble(8)
                );
            }
            return 0;
        });

        var championRows = new Dictionary<string, List<(string ChampionName, int Wins, int Losses, double AvgKda)>>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(championSql, conn);
            foreach (var (name, value) in puuidParams2)
                cmd.Parameters.AddWithValue(name, value);
            cmd.Parameters.AddWithValue("@today_start2", todayStartMs);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var puuid = reader.GetString(0);
                if (!championRows.TryGetValue(puuid, out var list))
                {
                    list = new List<(string, int, int, double)>();
                    championRows[puuid] = list;
                }
                list.Add((
                    reader.GetString(1),
                    reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                    reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                    reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4)
                ));
            }
            return 0;
        });

        var perAccount = new List<PerAccountSessionData>();
        foreach (var puuid in puuids)
        {
            if (!statsRows.TryGetValue(puuid, out var stats))
                continue;

            championRows.TryGetValue(puuid, out var champions);

            string? bestChampionName = null;
            var bestChampionWins = 0;
            var bestChampionLosses = 0;
            var bestChampionAvgKda = 0.0;

            if (champions != null && champions.Count > 0)
            {
                var best = champions
                    .OrderByDescending(c => c.Wins + c.Losses > 0 ? (double)c.Wins / (c.Wins + c.Losses) : 0.0)
                    .ThenByDescending(c => c.AvgKda)
                    .First();
                bestChampionName = best.ChampionName;
                bestChampionWins = best.Wins;
                bestChampionLosses = best.Losses;
                bestChampionAvgKda = best.AvgKda;
            }

            perAccount.Add(new PerAccountSessionData(
                Puuid: puuid,
                GamesToday: stats.GamesToday,
                WinsToday: stats.WinsToday,
                LossesToday: stats.LossesToday,
                AvgKdaToday: stats.AvgKdaToday,
                BestChampionName: bestChampionName,
                BestChampionWins: bestChampionWins,
                BestChampionLosses: bestChampionLosses,
                BestChampionAvgKda: bestChampionAvgKda,
                GamesThisWeek: stats.GamesThisWeek,
                WinsThisWeek: stats.WinsThisWeek,
                LossesThisWeek: stats.LossesThisWeek,
                AvgKdaThisWeek: stats.AvgKdaThisWeek
            ));
        }

        return new SessionStatsData(perAccount);
    }

    /// <summary>
    /// Returns survival statistics computed from the last N games across all specified PUUIDs.
    /// Death buckets use rank-adaptive thresholds: games with deaths &lt;= lowDeathThreshold are the
    /// "low" bucket; games with deaths &gt; highDeathThreshold are the "high" bucket.
    /// Win rates are null when a bucket has zero games (not 0.0).
    /// </summary>
    public virtual async Task<SurvivalStatsData> GetSurvivalStatsAsync(
        IReadOnlyList<string> puuids,
        int lowDeathThreshold,
        int highDeathThreshold,
        int lastNGames = 20)
    {
        if (puuids.Count == 0)
            return new SurvivalStatsData(0, null, null, 0, 0, 0);

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                p.win,
                p.deaths
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_duration_sec >= {MinValidGameDurationSec}
            ORDER BY m.game_start_time DESC
            LIMIT @last_n_games";

        var rows = new List<(bool Win, int Deaths)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
                cmd.Parameters.AddWithValue(name, value);
            cmd.Parameters.AddWithValue("@last_n_games", lastNGames);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add((
                    Win: reader.GetBoolean(0),
                    Deaths: reader.GetInt32(1)
                ));
            }
            return 0;
        });

        if (rows.Count == 0)
            return new SurvivalStatsData(0, null, null, 0, 0, 0);

        var totalGames = rows.Count;
        var avgDeathsPerGame = rows.Average(r => (double)r.Deaths);

        var lowDeaths = rows.Where(r => r.Deaths <= lowDeathThreshold).ToList();
        var highDeaths = rows.Where(r => r.Deaths > highDeathThreshold).ToList();
        var winRateLowDeaths = lowDeaths.Count > 0
            ? lowDeaths.Count(r => r.Win) / (double)lowDeaths.Count
            : (double?)null;
        var winRateHighDeaths = highDeaths.Count > 0
            ? highDeaths.Count(r => r.Win) / (double)highDeaths.Count
            : (double?)null;

        return new SurvivalStatsData(
            AvgDeathsPerGame: avgDeathsPerGame,
            WinRateLowDeaths: winRateLowDeaths,
            WinRateHighDeaths: winRateHighDeaths,
            GamesLowDeaths: lowDeaths.Count,
            GamesHighDeaths: highDeaths.Count,
            TotalGames: totalGames
        );
    }
}

