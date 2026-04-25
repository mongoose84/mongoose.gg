using MySqlConnector;
using Mongoose.Api.Application.QueryModels;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Helpers;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class MatchesRepository : RepositoryBase, IMatchesRepository
{
    public MatchesRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(Match match)
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

	    public async Task<long> GetTotalMatchCountAsync()
	    {
	        const string sql = "SELECT COUNT(*) FROM matches";
	        var result = await ExecuteScalarAsync<long>(sql);
	        return result;
	    }

    public Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit)
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

        return ExecuteListAsync(sql, MatchDataMapper.MapMatch, parameters.ToArray());
    }

    /// <summary>
    /// Gets the last 20 matches with full participant stats for the match list view.
    /// Includes trend badge computation based on role baselines.
    /// </summary>
    public async Task<IList<MatchListItem>> GetMatchListAsync(
        string puuid,
        string queueFilter,
        int limit = 20,
        Dictionary<string, RoleBaseline>? baselines = null)
        => await GetMatchListAsync([puuid], queueFilter, limit, baselines);

    public async Task<IList<MatchListItem>> GetMatchListAsync(
        IReadOnlyList<string> puuids,
        string queueFilter,
        int limit = 20,
        Dictionary<string, RoleBaseline>? baselines = null)
    {
        if (puuids.Count == 0)
        {
            return new List<MatchListItem>();
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                m.match_id,
                p.puuid,
                m.queue_id,
                p.champion_id,
                p.champion_name,
                COALESCE(p.role, 'UNKNOWN') as role,
                p.lane,
                p.win,
                p.kills,
                p.deaths,
                p.assists,
                p.creep_score,
                p.gold_earned,
                m.game_duration_sec,
                m.game_start_time,
                COALESCE(pm.damage_dealt, 0) as damage_dealt,
                COALESCE(pm.damage_taken, 0) as damage_taken,
                COALESCE(pm.vision_score, 0) as vision_score,
                COALESCE(pm.kill_participation_pct, 0) as kill_participation,
                COALESCE(pm.damage_share_pct, 0) as damage_share,
                COALESCE(pm.deaths_pre_10, 0) as deaths_pre_10,
                p.team_id,
                COALESCE((SELECT SUM(p2.kills) FROM participants p2 WHERE p2.match_id = p.match_id AND p2.team_id = p.team_id), 0) as team_kills,
                COALESCE((SELECT SUM(p2.kills) FROM participants p2 WHERE p2.match_id = p.match_id AND p2.team_id != p.team_id), 0) as enemy_team_kills,
                pc15.gold_diff_vs_lane as gold_diff_at_15,
                -- Team comparison data
                COALESCE((SELECT SUM(pm2.damage_dealt) FROM participants p2 INNER JOIN participant_metrics pm2 ON pm2.participant_id = p2.id WHERE p2.match_id = p.match_id AND p2.team_id = p.team_id), 0) as team_total_damage,
                COALESCE((SELECT SUM(pm2.damage_dealt) FROM participants p2 INNER JOIN participant_metrics pm2 ON pm2.participant_id = p2.id WHERE p2.match_id = p.match_id AND p2.team_id != p.team_id), 0) as enemy_team_total_damage,
                tmm.gold_lead_at_15 as team_gold_lead_at_15,
                COALESCE(tobj.dragons_taken, 0) as team_dragons,
                COALESCE(tobj_enemy.dragons_taken, 0) as enemy_team_dragons,
                COALESCE(tobj.barons_taken, 0) as team_barons,
                COALESCE(tobj_enemy.barons_taken, 0) as enemy_team_barons,
                COALESCE(tobj.towers_taken, 0) as team_towers,
                COALESCE(tobj_enemy.towers_taken, 0) as enemy_team_towers
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN participant_checkpoints pc15 ON pc15.participant_id = p.id AND pc15.minute_mark = 15
            LEFT JOIN team_match_metrics tmm ON tmm.match_id = p.match_id AND tmm.team_id = p.team_id
            LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
            LEFT JOIN team_objectives tobj_enemy ON tobj_enemy.match_id = p.match_id AND tobj_enemy.team_id != p.team_id
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            {queueFilter}
            ORDER BY m.game_start_time DESC
            LIMIT @limit";

        var parameters = new List<(string name, object? value)>(puuidParams)
        {
            ("@limit", limit)
        };

        var rawData = await ExecuteListAsync(sql, MatchDataMapper.MapMatchListRaw, parameters.ToArray());

        // Transform to MatchListItem with computed fields
        var items = new List<MatchListItem>();
        foreach (var raw in rawData)
        {
            var durationMin = raw.GameDurationSec / 60.0;
            var csPerMin = durationMin > 0 ? Math.Round(raw.CreepScore / durationMin, 1) : 0;
            var goldPerMin = durationMin > 0 ? Math.Round(raw.GoldEarned / durationMin, 0) : 0;

            // Compute trend badge if baselines available
            TrendBadge? trendBadge = null;
            if (baselines != null && baselines.TryGetValue(raw.Role, out var baseline))
            {
                trendBadge = TrendBadgeCalculator.ComputeTrendBadge(raw, baseline);
            }

            items.Add(new MatchListItem(
                MatchId: raw.MatchId,
                QueueId: raw.QueueId,
                QueueType: LeagueDataHelper.GetQueueLabelShort(raw.QueueId),
                ChampionId: raw.ChampionId,
                ChampionName: raw.ChampionName,
                ChampionIconUrl: LeagueDataHelper.GetChampionIconUrl(raw.ChampionName),
                Role: raw.Role,
                Lane: raw.Lane,
                Win: raw.Win,
                Kills: raw.Kills,
                Deaths: raw.Deaths,
                Assists: raw.Assists,
                CreepScore: raw.CreepScore,
                GoldEarned: raw.GoldEarned,
                GameDurationSec: raw.GameDurationSec,
                GameStartTime: raw.GameStartTime,
                DamageDealt: raw.DamageDealt,
                DamageTaken: raw.DamageTaken,
                VisionScore: raw.VisionScore,
                KillParticipation: (double)raw.KillParticipation,
                DamageShare: (double)raw.DamageShare,
                DeathsPre10: raw.DeathsPre10,
                CsPerMin: csPerMin,
                GoldPerMin: goldPerMin,
                TeamKills: raw.TeamKills,
                EnemyTeamKills: raw.EnemyTeamKills,
                GoldDiffAt15: raw.GoldDiffAt15,
                TeamTotalDamage: raw.TeamTotalDamage,
                EnemyTeamTotalDamage: raw.EnemyTeamTotalDamage,
                TeamGoldLeadAt15: raw.TeamGoldLeadAt15,
                TeamDragons: raw.TeamDragons,
                EnemyTeamDragons: raw.EnemyTeamDragons,
                TeamBarons: raw.TeamBarons,
                EnemyTeamBarons: raw.EnemyTeamBarons,
                TeamTowers: raw.TeamTowers,
                EnemyTeamTowers: raw.EnemyTeamTowers,
                TrendBadge: trendBadge
            ));
        }

        return items;
    }

    /// <summary>
    /// Gets lightweight match summaries for the list view.
    /// Only fetches data needed to render match rows - no correlated subqueries for team stats.
    /// </summary>
    public async Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
        string puuid,
        string queueFilter,
        int limit = 20,
        Dictionary<string, RoleBaseline>? baselines = null)
        => await GetMatchListSummaryAsync([puuid], queueFilter, limit, baselines);

    public async Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
        IReadOnlyList<string> puuids,
        string queueFilter,
        int limit = 20,
        Dictionary<string, RoleBaseline>? baselines = null)
    {
        if (puuids.Count == 0)
        {
            return new List<MatchListSummaryItem>();
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                m.match_id as match_id,
                ra.game_name as account_game_name,
                ra.tag_line as account_tag_line,
                ra.region as account_region,
                m.queue_id as queue_id,
                p.champion_id as champion_id,
                p.champion_name as champion_name,
                COALESCE(p.role, 'UNKNOWN') as role,
                p.lane as lane,
                p.win as win,
                p.kills as kills,
                p.deaths as deaths,
                p.assists as assists,
                p.creep_score as creep_score,
                p.gold_earned as gold_earned,
                m.game_duration_sec as game_duration_sec,
                m.game_start_time as game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN riot_accounts ra ON ra.puuid = p.puuid
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec}
            {queueFilter}
            ORDER BY m.game_start_time DESC
            LIMIT @limit";

        var parameters = new List<(string name, object? value)>(puuidParams)
        {
            ("@limit", limit)
        };

        var rawData = await ExecuteListAsync(sql, MatchDataMapper.MapMatchListSummaryRaw, parameters.ToArray());

        // Transform to MatchListSummaryItem with computed fields
        var items = new List<MatchListSummaryItem>();
        foreach (var raw in rawData)
        {
            var durationMin = raw.GameDurationSec / 60.0;
            var csPerMin = durationMin > 0 ? Math.Round(raw.CreepScore / durationMin, 1) : 0;
            var goldPerMin = durationMin > 0 ? Math.Round(raw.GoldEarned / durationMin, 0) : 0;

            // Compute trend badge if baselines available
            TrendBadge? trendBadge = null;
            if (baselines != null && baselines.TryGetValue(raw.Role, out var baseline))
            {
                trendBadge = TrendBadgeCalculator.ComputeTrendBadgeSummary(raw, baseline);
            }

            items.Add(new MatchListSummaryItem(
                MatchId: raw.MatchId,
                AccountGameName: raw.AccountGameName,
                AccountTagLine: raw.AccountTagLine,
                AccountRegion: raw.AccountRegion,
                QueueId: raw.QueueId,
                QueueType: LeagueDataHelper.GetQueueLabelShort(raw.QueueId),
                ChampionId: raw.ChampionId,
                ChampionName: raw.ChampionName,
                ChampionIconUrl: LeagueDataHelper.GetChampionIconUrl(raw.ChampionName),
                Role: raw.Role,
                Lane: raw.Lane,
                Win: raw.Win,
                Kills: raw.Kills,
                Deaths: raw.Deaths,
                Assists: raw.Assists,
                CreepScore: raw.CreepScore,
                GoldEarned: raw.GoldEarned,
                GameDurationSec: raw.GameDurationSec,
                GameStartTime: raw.GameStartTime,
                CsPerMin: csPerMin,
                GoldPerMin: goldPerMin,
                TrendBadge: trendBadge
            ));
        }

        return items;
    }

    /// <summary>
    /// Gets full match details for a single match.
    /// Uses CTEs to pre-aggregate team stats, avoiding correlated subqueries.
    /// </summary>
    public async Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid)
    {
        const string sql = @"
            WITH TeamKills AS (
                SELECT
                    match_id,
                    team_id,
                    SUM(kills) as team_kills
                FROM participants
                WHERE match_id = @matchId
                GROUP BY match_id, team_id
            ),
            TeamDamage AS (
                SELECT
                    p.match_id,
                    p.team_id,
                    COALESCE(SUM(pm.damage_dealt), 0) as team_damage
                FROM participants p
                LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
                WHERE p.match_id = @matchId
                GROUP BY p.match_id, p.team_id
            )
            SELECT
                m.match_id,
                m.queue_id,
                p.champion_id,
                p.champion_name,
                COALESCE(p.role, 'UNKNOWN') as role,
                p.lane,
                p.win,
                p.kills,
                p.deaths,
                p.assists,
                p.creep_score,
                p.gold_earned,
                m.game_duration_sec,
                m.game_start_time,
                COALESCE(pm.damage_dealt, 0) as damage_dealt,
                COALESCE(pm.damage_taken, 0) as damage_taken,
                COALESCE(pm.vision_score, 0) as vision_score,
                COALESCE(pm.kill_participation_pct, 0) as kill_participation,
                COALESCE(pm.damage_share_pct, 0) as damage_share,
                COALESCE(pm.deaths_pre_10, 0) as deaths_pre_10,
                p.team_id,
                pc15.gold_diff_vs_lane as gold_diff_at_15,
                COALESCE(tk.team_kills, 0) as team_kills,
                COALESCE(tk_enemy.team_kills, 0) as enemy_team_kills,
                COALESCE(td.team_damage, 0) as team_total_damage,
                COALESCE(td_enemy.team_damage, 0) as enemy_team_total_damage,
                tmm.gold_lead_at_15 as team_gold_lead_at_15,
                COALESCE(tobj.dragons_taken, 0) as team_dragons,
                COALESCE(tobj_enemy.dragons_taken, 0) as enemy_team_dragons,
                COALESCE(tobj.barons_taken, 0) as team_barons,
                COALESCE(tobj_enemy.barons_taken, 0) as enemy_team_barons,
                COALESCE(tobj.towers_taken, 0) as team_towers,
                COALESCE(tobj_enemy.towers_taken, 0) as enemy_team_towers,
                COALESCE(po.dragons_participated, 0) as dragons_participated
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN participant_checkpoints pc15 ON pc15.participant_id = p.id AND pc15.minute_mark = 15
            LEFT JOIN team_match_metrics tmm ON tmm.match_id = p.match_id AND tmm.team_id = p.team_id
            LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
            LEFT JOIN team_objectives tobj_enemy ON tobj_enemy.match_id = p.match_id AND tobj_enemy.team_id != p.team_id
            LEFT JOIN participant_objectives po ON po.participant_id = p.id
            LEFT JOIN TeamKills tk ON tk.match_id = p.match_id AND tk.team_id = p.team_id
            LEFT JOIN TeamKills tk_enemy ON tk_enemy.match_id = p.match_id AND tk_enemy.team_id != p.team_id
            LEFT JOIN TeamDamage td ON td.match_id = p.match_id AND td.team_id = p.team_id
            LEFT JOIN TeamDamage td_enemy ON td_enemy.match_id = p.match_id AND td_enemy.team_id != p.team_id
            WHERE p.match_id = @matchId AND p.puuid = @puuid
            LIMIT 1";

        var rawData = await ExecuteSingleAsync(sql, MatchDataMapper.MapMatchDetailsRaw,
            ("@matchId", matchId),
            ("@puuid", puuid));

        if (rawData == null) return null;

        var durationMin = rawData.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? Math.Round(rawData.CreepScore / durationMin, 1) : 0;
        var goldPerMin = durationMin > 0 ? Math.Round(rawData.GoldEarned / durationMin, 0) : 0;

        return new MatchDetailsItem(
            MatchId: rawData.MatchId,
            QueueId: rawData.QueueId,
            QueueType: LeagueDataHelper.GetQueueLabelShort(rawData.QueueId),
            ChampionId: rawData.ChampionId,
            ChampionName: rawData.ChampionName,
            ChampionIconUrl: LeagueDataHelper.GetChampionIconUrl(rawData.ChampionName),
            Role: rawData.Role,
            Lane: rawData.Lane,
            Win: rawData.Win,
            Kills: rawData.Kills,
            Deaths: rawData.Deaths,
            Assists: rawData.Assists,
            CreepScore: rawData.CreepScore,
            GoldEarned: rawData.GoldEarned,
            GameDurationSec: rawData.GameDurationSec,
            GameStartTime: rawData.GameStartTime,
            DamageDealt: rawData.DamageDealt,
            DamageTaken: rawData.DamageTaken,
            VisionScore: rawData.VisionScore,
            KillParticipation: (double)rawData.KillParticipation,
            DamageShare: (double)rawData.DamageShare,
            DeathsPre10: rawData.DeathsPre10,
            CsPerMin: csPerMin,
            GoldPerMin: goldPerMin,
            TeamKills: rawData.TeamKills,
            EnemyTeamKills: rawData.EnemyTeamKills,
            GoldDiffAt15: rawData.GoldDiffAt15,
            TeamTotalDamage: rawData.TeamTotalDamage,
            EnemyTeamTotalDamage: rawData.EnemyTeamTotalDamage,
            TeamGoldLeadAt15: rawData.TeamGoldLeadAt15,
            TeamDragons: rawData.TeamDragons,
            EnemyTeamDragons: rawData.EnemyTeamDragons,
            TeamBarons: rawData.TeamBarons,
            EnemyTeamBarons: rawData.EnemyTeamBarons,
            TeamTowers: rawData.TeamTowers,
            EnemyTeamTowers: rawData.EnemyTeamTowers,
            DragonsParticipated: rawData.DragonsParticipated
        );
    }

    /// <summary>
    /// Gets baseline averages per role from the last 10 games in each role.
    /// Used for trend comparisons in the match list.
    /// </summary>
    public async Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter)
        => await GetRoleBaselinesAsync([puuid], queueFilter);

    public async Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(IReadOnlyList<string> puuids, string queueFilter)
    {
        if (puuids.Count == 0)
        {
            return new Dictionary<string, RoleBaseline>();
        }

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            WITH RankedMatches AS (
                SELECT
                    COALESCE(p.role, 'UNKNOWN') as role,
                    p.kills,
                    p.deaths,
                    p.assists,
                    p.creep_score,
                    p.gold_earned,
                    p.win,
                    m.game_duration_sec,
                    COALESCE(pm.damage_dealt, 0) as damage_dealt,
                    COALESCE(pm.damage_taken, 0) as damage_taken,
                    COALESCE(pm.vision_score, 0) as vision_score,
                    COALESCE(pm.kill_participation_pct, 0) as kill_participation,
                    ROW_NUMBER() OVER (PARTITION BY COALESCE(p.role, 'UNKNOWN') ORDER BY m.game_start_time DESC) as rn
                FROM participants p
                INNER JOIN matches m ON m.match_id = p.match_id
                LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
                WHERE {puuidPredicate}
                AND m.game_duration_sec >= {MinValidGameDurationSec}
                {queueFilter}
            )
            SELECT
                role,
                COUNT(*) as games_count,
                AVG(kills) as avg_kills,
                AVG(deaths) as avg_deaths,
                AVG(assists) as avg_assists,
                AVG(CASE WHEN deaths = 0 THEN kills + assists ELSE (kills + assists) / deaths END) as avg_kda,
                AVG(creep_score) as avg_creep_score,
                AVG(CASE WHEN game_duration_sec > 0 THEN creep_score / (game_duration_sec / 60.0) ELSE 0 END) as avg_cs_per_min,
                AVG(gold_earned) as avg_gold_earned,
                AVG(CASE WHEN game_duration_sec > 0 THEN gold_earned / (game_duration_sec / 60.0) ELSE 0 END) as avg_gold_per_min,
                AVG(damage_dealt) as avg_damage_dealt,
                AVG(damage_taken) as avg_damage_taken,
                AVG(vision_score) as avg_vision_score,
                AVG(kill_participation) as avg_kill_participation,
                AVG(game_duration_sec) as avg_game_duration_sec,
                AVG(CASE WHEN win THEN 1.0 ELSE 0.0 END) * 100 as win_rate
            FROM RankedMatches
            WHERE rn <= 10
            GROUP BY role";

        var baselines = new Dictionary<string, RoleBaseline>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var role = reader.GetString(0);
                baselines[role] = new RoleBaseline(
                    Role: role,
                    GamesCount: reader.GetInt32(1),
                    AvgKills: reader.GetDouble(2),
                    AvgDeaths: reader.GetDouble(3),
                    AvgAssists: reader.GetDouble(4),
                    AvgKda: reader.GetDouble(5),
                    AvgCreepScore: reader.GetDouble(6),
                    AvgCsPerMin: reader.GetDouble(7),
                    AvgGoldEarned: reader.GetDouble(8),
                    AvgGoldPerMin: reader.GetDouble(9),
                    AvgDamageDealt: reader.GetDouble(10),
                    AvgDamageTaken: reader.GetDouble(11),
                    AvgVisionScore: reader.GetDouble(12),
                    AvgKillParticipation: reader.GetDouble(13),
                    AvgGameDurationSec: reader.GetDouble(14),
                    WinRate: reader.GetDouble(15)
                );
            }
            return 0;
        });

        return baselines;
    }

    /// <summary>
    /// Gets all 10 participants for a match with their metrics and 10-minute checkpoints.
    /// Used for the Match Narrative feature to show lane matchups.
    /// </summary>
    public async Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId)
    {
        const string sql = @"
            SELECT
                p.id as participant_id,
                p.puuid,
                p.champion_id,
                p.champion_name,
                p.team_id,
                COALESCE(p.role, 'UNKNOWN') as role,
                p.win,
                p.kills,
                p.deaths,
                p.assists,
                p.creep_score,
                p.gold_earned,
                COALESCE(pm.kill_participation_pct, 0) as kill_participation,
                COALESCE(pm.damage_share_pct, 0) as damage_share,
                COALESCE(pm.vision_score, 0) as vision_score,
                COALESCE(pm.deaths_pre_10, 0) as deaths_pre_10,
                pc10.gold as gold_at_10,
                pc10.cs as cs_at_10,
                pc10.gold_diff_vs_lane as gold_diff_at_10,
                pc10.cs_diff_vs_lane as cs_diff_at_10
            FROM participants p
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN participant_checkpoints pc10 ON pc10.participant_id = p.id AND pc10.minute_mark = 10
            WHERE p.match_id = @match_id
            ORDER BY p.team_id,
                CASE p.role
                    WHEN 'TOP' THEN 1
                    WHEN 'JUNGLE' THEN 2
                    WHEN 'MIDDLE' THEN 3
                    WHEN 'BOTTOM' THEN 4
                    WHEN 'UTILITY' THEN 5
                    ELSE 6
                END";

        return await ExecuteListAsync(sql, MatchDataMapper.MapMatchupParticipantRaw, ("@match_id", matchId));
    }

    /// <summary>
    /// Deletes matches older than the specified cutoff date in batches.
    /// Uses CASCADE DELETE to automatically remove related records from all child tables:
    /// - participants
    /// - participant_metrics
    /// - participant_checkpoints
    /// - team_objectives
    /// - participant_objectives
    /// - team_match_metrics
    /// - team_role_responsibilities
    /// - duo_metrics
    /// </summary>
    public async Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize)
    {
        // Step 1: Find old match IDs to delete
        const string selectSql = @"
            SELECT match_id
            FROM matches
            WHERE game_start_time < @cutoff
            LIMIT @limit";

        var matchIds = await ExecuteListAsync(
            selectSql,
            r => r.GetString(0),
            ("@cutoff", cutoffTimestamp),
            ("@limit", batchSize));

        if (matchIds.Count == 0)
            return 0;

        // Step 2: Delete matches (CASCADE will handle all related tables automatically)
        var placeholders = string.Join(",", matchIds.Select((_, i) => $"@id{i}"));
        var deleteSql = $"DELETE FROM matches WHERE match_id IN ({placeholders})";

        var parameters = matchIds
            .Select((id, i) => ($"@id{i}", (object?)id))
            .ToArray();

        await ExecuteNonQueryAsync(deleteSql, parameters);

        return matchIds.Count;
    }
}
