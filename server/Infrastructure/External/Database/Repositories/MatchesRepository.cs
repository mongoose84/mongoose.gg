using MySqlConnector;
using RiotProxy.Application.DTOs.Matches;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class MatchesRepository : RepositoryBase
{
    private const string DataDragonVersion = "16.1.1";

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

        return ExecuteListAsync(sql, Map, parameters.ToArray());
    }

    private static Match Map(MySqlDataReader r) => new()
    {
        MatchId = r.GetString(0),
        QueueId = r.GetInt32(1),
        GameDurationSec = r.GetInt32(2),
        GameStartTime = r.GetInt64(3),
        PatchVersion = r.GetString(4),
        SeasonCode = r.IsDBNull(5) ? null : r.GetString(5),
        CreatedAt = r.GetDateTimeUtc(6)
    };

    /// <summary>
    /// Gets the last 20 matches with full participant stats for the match list view.
    /// Includes trend badge computation based on role baselines.
    /// </summary>
    public async Task<IList<MatchListItem>> GetMatchListAsync(
        string puuid,
        string queueFilter,
        int limit = 20,
        Dictionary<string, RoleBaseline>? baselines = null)
    {
        var sql = $@"
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
            WHERE p.puuid = @puuid
            {queueFilter}
            ORDER BY m.game_start_time DESC
            LIMIT @limit";

        var rawData = await ExecuteListAsync(sql, MapMatchListRaw,
            ("@puuid", puuid),
            ("@limit", limit));

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
                trendBadge = ComputeTrendBadge(raw, baseline);
            }

            items.Add(new MatchListItem(
                MatchId: raw.MatchId,
                QueueId: raw.QueueId,
                QueueType: GetQueueLabel(raw.QueueId),
                ChampionId: raw.ChampionId,
                ChampionName: raw.ChampionName,
                ChampionIconUrl: GetChampionIconUrl(raw.ChampionName),
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
    /// Gets baseline averages per role from the last 10 games in each role.
    /// Used for trend comparisons in the match list.
    /// </summary>
    public async Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter)
    {
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
                WHERE p.puuid = @puuid
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
            cmd.Parameters.AddWithValue("@puuid", puuid);
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
    /// Computes the most notable trend badge for a match compared to role baseline.
    /// </summary>
    private static TrendBadge? ComputeTrendBadge(MatchListRawData match, RoleBaseline baseline)
    {
        if (baseline.GamesCount < 3) return null; // Not enough data for meaningful comparison

        var durationMin = match.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? match.CreepScore / durationMin : 0;

        // Calculate deviations from baseline (as percentage difference)
        var insights = new List<(string text, string type, string stat, double deviation)>();

        // Damage dealt comparison
        if (baseline.AvgDamageDealt > 0)
        {
            var damageDeviation = (match.DamageDealt - baseline.AvgDamageDealt) / baseline.AvgDamageDealt;
            if (damageDeviation > 0.2)
                insights.Add(("Above avg damage", "positive", "damageDealt", damageDeviation));
            else if (damageDeviation < -0.2)
                insights.Add(("Below avg damage", "neutral", "damageDealt", Math.Abs(damageDeviation)));
        }

        // Damage taken comparison (higher can be good for tanks)
        if (baseline.AvgDamageTaken > 0)
        {
            var takenDeviation = (match.DamageTaken - baseline.AvgDamageTaken) / baseline.AvgDamageTaken;
            if (takenDeviation > 0.25)
                insights.Add(("Tankier than usual", "positive", "damageTaken", takenDeviation));
        }

        // Deaths comparison (lower is better)
        if (baseline.AvgDeaths > 0)
        {
            var deathDeviation = (match.Deaths - baseline.AvgDeaths) / baseline.AvgDeaths;
            if (deathDeviation > 0.3)
                insights.Add(("Higher deaths vs trend", "neutral", "deaths", deathDeviation));
            else if (deathDeviation < -0.3 && match.Deaths <= 3)
                insights.Add(("Clean game", "positive", "deaths", Math.Abs(deathDeviation)));
        }

        // Vision score comparison (for support/jungle)
        if (baseline.AvgVisionScore > 10 && match.VisionScore > 0)
        {
            var visionDeviation = (match.VisionScore - baseline.AvgVisionScore) / baseline.AvgVisionScore;
            if (visionDeviation > 0.25)
                insights.Add(("Strong vision control", "positive", "visionScore", visionDeviation));
        }

        // CS comparison (for laners)
        if (baseline.AvgCsPerMin > 4 && csPerMin > 0)
        {
            var csDeviation = (csPerMin - baseline.AvgCsPerMin) / baseline.AvgCsPerMin;
            if (csDeviation > 0.15)
                insights.Add(("High CS efficiency", "positive", "csPerMin", csDeviation));
        }

        // Kill participation
        if (baseline.AvgKillParticipation > 0)
        {
            var kpDeviation = ((double)match.KillParticipation - baseline.AvgKillParticipation) / baseline.AvgKillParticipation;
            if (kpDeviation > 0.2)
                insights.Add(("High kill participation", "positive", "killParticipation", kpDeviation));
        }

        // Return the most significant insight (highest deviation)
        if (insights.Count == 0) return null;

        var best = insights.OrderByDescending(i => i.deviation).First();
        return new TrendBadge(best.text, best.type, best.stat);
    }

    private static MatchListRawData MapMatchListRaw(MySqlDataReader r) => new(
        MatchId: r.GetString(0),
        QueueId: r.GetInt32(1),
        ChampionId: r.GetInt32(2),
        ChampionName: r.GetString(3),
        Role: r.GetString(4),
        Lane: r.IsDBNull(5) ? null : r.GetString(5),
        Win: r.GetBoolean(6),
        Kills: r.GetInt32(7),
        Deaths: r.GetInt32(8),
        Assists: r.GetInt32(9),
        CreepScore: r.GetInt32(10),
        GoldEarned: r.GetInt32(11),
        GameDurationSec: r.GetInt32(12),
        GameStartTime: r.GetInt64(13),
        DamageDealt: r.GetInt32(14),
        DamageTaken: r.GetInt32(15),
        VisionScore: r.GetInt32(16),
        KillParticipation: r.GetDecimal(17),
        DamageShare: r.GetDecimal(18),
        DeathsPre10: r.GetInt32(19),
        TeamId: r.GetInt32(20),
        TeamKills: r.GetInt32(21),
        EnemyTeamKills: r.GetInt32(22),
        GoldDiffAt15: r.IsDBNull(23) ? null : r.GetInt32(23),
        TeamTotalDamage: r.GetInt32(24),
        EnemyTeamTotalDamage: r.GetInt32(25),
        TeamGoldLeadAt15: r.IsDBNull(26) ? null : r.GetInt32(26),
        TeamDragons: r.GetInt32(27),
        EnemyTeamDragons: r.GetInt32(28),
        TeamBarons: r.GetInt32(29),
        EnemyTeamBarons: r.GetInt32(30),
        TeamTowers: r.GetInt32(31),
        EnemyTeamTowers: r.GetInt32(32)
    );

    private static string GetQueueLabel(int queueId) => queueId switch
    {
        420 => "Ranked Solo",
        440 => "Ranked Flex",
        400 => "Normal Draft",
        430 => "Normal Blind",
        450 => "ARAM",
        _ => $"Queue {queueId}"
    };

    private static string GetChampionIconUrl(string championName)
    {
        var normalized = championName.Replace(" ", "").Replace("'", "");
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/champion/{normalized}.png";
    }

    public static string BuildQueueFilter(string queueType)
    {
        return queueType switch
        {
            "ranked_solo" => "AND m.queue_id = 420",
            "ranked_flex" => "AND m.queue_id = 440",
            "normal" => "AND m.queue_id IN (430, 400)",
            "aram" => "AND m.queue_id = 450",
            _ => ""  // all
        };
    }

    public static string ValidateQueueType(string? queueType)
    {
        var normalized = queueType?.ToLowerInvariant() ?? "all";
        return normalized switch
        {
            "ranked_solo" or "ranked_flex" or "normal" or "aram" or "all" => normalized,
            _ => "all"
        };
    }

    /// <summary>
    /// Gets all 10 participants for a match with their metrics and 15-minute checkpoints.
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
                pc15.gold as gold_at_15,
                pc15.cs as cs_at_15,
                pc15.gold_diff_vs_lane as gold_diff_at_15,
                pc15.cs_diff_vs_lane as cs_diff_at_15
            FROM participants p
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN participant_checkpoints pc15 ON pc15.participant_id = p.id AND pc15.minute_mark = 15
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

        return await ExecuteListAsync(sql, MapMatchupParticipantRaw, ("@match_id", matchId));
    }

    private static MatchupParticipantRaw MapMatchupParticipantRaw(MySqlDataReader r) => new(
        ParticipantId: r.GetInt64(0),
        Puuid: r.GetString(1),
        ChampionId: r.GetInt32(2),
        ChampionName: r.GetString(3),
        TeamId: r.GetInt32(4),
        Role: r.GetString(5),
        Win: r.GetBoolean(6),
        Kills: r.GetInt32(7),
        Deaths: r.GetInt32(8),
        Assists: r.GetInt32(9),
        CreepScore: r.GetInt32(10),
        GoldEarned: r.GetInt32(11),
        KillParticipation: r.GetDecimal(12),
        DamageShare: r.GetDecimal(13),
        VisionScore: r.GetInt32(14),
        GoldAt15: r.IsDBNull(15) ? null : r.GetInt32(15),
        CsAt15: r.IsDBNull(16) ? null : r.GetInt32(16),
        GoldDiffAt15: r.IsDBNull(17) ? null : r.GetInt32(17),
        CsDiffAt15: r.IsDBNull(18) ? null : r.GetInt32(18)
    );
}
