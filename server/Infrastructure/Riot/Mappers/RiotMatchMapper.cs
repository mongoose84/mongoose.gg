using System.Text.Json;
using RiotProxy.Core.Entities;

namespace RiotProxy.Infrastructure.Riot.Mappers;

/// <summary>
/// Maps Riot API match-v5 info JSON to domain entities.
/// Reference: https://developer.riotgames.com/apis#match-v5/GET_getMatch
/// </summary>
public static class RiotMatchMapper
{
    /// <summary>
    /// Maps match-v5 info to Match entity.
    /// </summary>
    public static Match MapMatch(JsonElement matchRoot)
    {
        var info = matchRoot.GetProperty("info");
        var metadata = matchRoot.GetProperty("metadata");

        return new Match
        {
            MatchId = metadata.GetProperty("matchId").GetString() ?? string.Empty,
            QueueId = info.GetProperty("queueId").GetInt32(),
            GameDurationSec = info.GetProperty("gameDuration").GetInt32(),
            GameStartTime = info.GetProperty("gameStartTimestamp").GetInt64(),
            PatchVersion = ExtractPatchVersion(info.GetProperty("gameVersion").GetString()),
            SeasonCode = null, // Derived from patch version by application logic
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps match-v5 info.participants[] to Participant entities.
    /// </summary>
    public static IList<Participant> MapParticipants(JsonElement matchRoot)
    {
        var info = matchRoot.GetProperty("info");
        var metadata = matchRoot.GetProperty("metadata");
        var matchId = metadata.GetProperty("matchId").GetString() ?? string.Empty;
        var participants = new List<Participant>();

        foreach (var p in info.GetProperty("participants").EnumerateArray())
        {
            participants.Add(new Participant
            {
                MatchId = matchId,
                Puuid = p.GetProperty("puuid").GetString() ?? string.Empty,
                TeamId = p.GetProperty("teamId").GetInt32(),
                Role = GetStringOrNull(p, "teamPosition"), // TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY
                Lane = GetStringOrNull(p, "lane"),
                ChampionId = p.GetProperty("championId").GetInt32(),
                ChampionName = p.GetProperty("championName").GetString() ?? string.Empty,
                Win = p.GetProperty("win").GetBoolean(),
                Kills = p.GetProperty("kills").GetInt32(),
                Deaths = p.GetProperty("deaths").GetInt32(),
                Assists = p.GetProperty("assists").GetInt32(),
                CreepScore = p.GetProperty("totalMinionsKilled").GetInt32() + p.GetProperty("neutralMinionsKilled").GetInt32(),
                GoldEarned = p.GetProperty("goldEarned").GetInt32(),
                TimeDeadSec = p.GetProperty("totalTimeSpentDead").GetInt32(),
                CreatedAt = DateTime.UtcNow
            });
        }

        return participants;
    }

    /// <summary>
    /// Maps match-v5 info.teams[].objectives to TeamObjective entities.
    /// </summary>
    public static IList<TeamObjective> MapTeamObjectives(JsonElement matchRoot)
    {
        var info = matchRoot.GetProperty("info");
        var metadata = matchRoot.GetProperty("metadata");
        var matchId = metadata.GetProperty("matchId").GetString() ?? string.Empty;
        var objectives = new List<TeamObjective>();

        foreach (var team in info.GetProperty("teams").EnumerateArray())
        {
            var teamId = team.GetProperty("teamId").GetInt32();
            var obj = team.GetProperty("objectives");

            objectives.Add(new TeamObjective
            {
                MatchId = matchId,
                TeamId = teamId,
                DragonsTaken = obj.GetProperty("dragon").GetProperty("kills").GetInt32(),
                HeraldsTaken = obj.GetProperty("riftHerald").GetProperty("kills").GetInt32(),
                BaronsTaken = obj.GetProperty("baron").GetProperty("kills").GetInt32(),
                TowersTaken = obj.GetProperty("tower").GetProperty("kills").GetInt32(),
                CreatedAt = DateTime.UtcNow
            });
        }

        return objectives;
    }

    /// <summary>
    /// Maps participant data to ParticipantMetric entities (info-derived fields only).
    /// Timeline-derived fields (death timing, first kill) should be populated by RiotTimelineMapper.
    /// </summary>
    public static ParticipantMetric MapParticipantMetricFromInfo(JsonElement participantJson, int gameDurationSec, int teamTotalKills, int teamTotalDamage)
    {
        var kills = participantJson.GetProperty("kills").GetInt32();
        var assists = participantJson.GetProperty("assists").GetInt32();
        var damage = participantJson.GetProperty("totalDamageDealtToChampions").GetInt32();
        var visionScore = participantJson.GetProperty("visionScore").GetInt32();
        var durationMin = gameDurationSec / 60.0m;

        return new ParticipantMetric
        {
            KillParticipationPct = teamTotalKills > 0 ? (decimal)(kills + assists) / teamTotalKills * 100 : 0,
            DamageSharePct = teamTotalDamage > 0 ? (decimal)damage / teamTotalDamage * 100 : 0,
            DamageDealt = damage,
            DamageTaken = participantJson.GetProperty("totalDamageTaken").GetInt32(),
            DamageMitigated = participantJson.GetProperty("damageSelfMitigated").GetInt32(),
            VisionScore = visionScore,
            VisionPerMin = durationMin > 0 ? Math.Round(visionScore / durationMin, 2) : 0,
            // Timeline-derived fields - will be populated by RiotTimelineMapper
            DeathsPre10 = 0,
            Deaths10To20 = 0,
            Deaths20To30 = 0,
            Deaths30Plus = 0,
            FirstDeathMinute = null,
            FirstKillParticipationMinute = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string ExtractPatchVersion(string? gameVersion)
    {
        // gameVersion is like "14.24.123456" - we want "14.24"
        if (string.IsNullOrEmpty(gameVersion)) return string.Empty;
        var parts = gameVersion.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : gameVersion;
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var value = prop.GetString();
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    /// <summary>
    /// Maps participant data to TeamRoleResponsibility entities.
    /// Calculates deaths/gold/damage share per role within each team.
    /// </summary>
    public static IList<TeamRoleResponsibility> MapTeamRoleResponsibilities(JsonElement matchRoot)
    {
        var result = new List<TeamRoleResponsibility>();
        var info = matchRoot.GetProperty("info");
        var matchId = matchRoot.GetProperty("metadata").GetProperty("matchId").GetString()!;

        // Collect per-team totals
        var teamTotals = new Dictionary<int, (int deaths, int gold, int damage)>
        {
            { 100, (0, 0, 0) },
            { 200, (0, 0, 0) }
        };

        // Collect per-team-role stats
        var roleStats = new Dictionary<(int team, string role), (int deaths, int gold, int damage)>();

        foreach (var p in info.GetProperty("participants").EnumerateArray())
        {
            var teamId = p.GetProperty("teamId").GetInt32();
            var role = p.TryGetProperty("teamPosition", out var tp) ? tp.GetString() : null;
            if (string.IsNullOrEmpty(role)) continue;

            var deaths = p.GetProperty("deaths").GetInt32();
            var gold = p.GetProperty("goldEarned").GetInt32();
            var damage = p.GetProperty("totalDamageDealtToChampions").GetInt32();

            // Add to team totals
            var current = teamTotals[teamId];
            teamTotals[teamId] = (current.deaths + deaths, current.gold + gold, current.damage + damage);

            // Add to role stats
            var key = (teamId, role);
            if (!roleStats.ContainsKey(key))
                roleStats[key] = (0, 0, 0);
            var roleData = roleStats[key];
            roleStats[key] = (roleData.deaths + deaths, roleData.gold + gold, roleData.damage + damage);
        }

        // Create responsibility records
        foreach (var ((teamId, role), stats) in roleStats)
        {
            var totals = teamTotals[teamId];
            result.Add(new TeamRoleResponsibility
            {
                MatchId = matchId,
                TeamId = teamId,
                Role = role,
                DeathsSharePct = totals.deaths > 0 ? Math.Round((decimal)stats.deaths / totals.deaths * 100, 2) : 0,
                GoldSharePct = totals.gold > 0 ? Math.Round((decimal)stats.gold / totals.gold * 100, 2) : 0,
                DamageSharePct = totals.damage > 0 ? Math.Round((decimal)stats.damage / totals.damage * 100, 2) : 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        return result;
    }
}

