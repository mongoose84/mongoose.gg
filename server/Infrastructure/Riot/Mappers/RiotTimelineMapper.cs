using System.Text.Json;
using RiotProxy.Core.Entities;

namespace RiotProxy.Infrastructure.Riot.Mappers;

/// <summary>
/// Maps Riot API match-v5 timeline JSON to domain entities.
/// Reference: https://developer.riotgames.com/apis#match-v5/GET_getTimeline
/// </summary>
public static class RiotTimelineMapper
{
    /// <summary>
    /// Maps timeline participantFrames to ParticipantCheckpoint entities.
    /// Creates checkpoints at specific minute marks (0, 5, 10, 15, 20, 25, 30).
    /// </summary>
    /// <param name="timelineRoot">The timeline JSON root element</param>
    /// <param name="participantIdMap">Maps Riot participantId (1-10) to database participant.Id</param>
    /// <param name="participantTeams">Maps Riot participantId (1-10) to teamId (100/200)</param>
    /// <param name="participantRoles">Maps Riot participantId (1-10) to role for lane opponent calculation</param>
    public static IList<ParticipantCheckpoint> MapCheckpoints(
        JsonElement timelineRoot,
        Dictionary<int, long> participantIdMap,
        Dictionary<int, int> participantTeams,
        Dictionary<int, string?> participantRoles)
    {
        var checkpoints = new List<ParticipantCheckpoint>();
        var info = timelineRoot.GetProperty("info");
        var frames = info.GetProperty("frames");

        // Target minute marks for checkpoints
        int[] minuteMarks = [0, 5, 10, 15, 20, 25, 30];
        var minuteMarkSet = new HashSet<int>(minuteMarks);

        foreach (var frame in frames.EnumerateArray())
        {
            var timestamp = frame.GetProperty("timestamp").GetInt64();
            var minute = (int)(timestamp / 60000);

            if (!minuteMarkSet.Contains(minute)) continue;

            var participantFrames = frame.GetProperty("participantFrames");

            // Build frame data for lane opponent calculation
            var frameData = new Dictionary<int, (int gold, int cs, int xp, int team, string? role)>();
            foreach (var pf in participantFrames.EnumerateObject())
            {
                if (!int.TryParse(pf.Name, out var participantId)) continue;
                var gold = pf.Value.GetProperty("totalGold").GetInt32();
                var minions = pf.Value.GetProperty("minionsKilled").GetInt32();
                var jungle = pf.Value.GetProperty("jungleMinionsKilled").GetInt32();
                var xp = pf.Value.GetProperty("xp").GetInt32();
                var team = participantTeams.GetValueOrDefault(participantId, 0);
                var role = participantRoles.GetValueOrDefault(participantId);
                frameData[participantId] = (gold, minions + jungle, xp, team, role);
            }

            // Create checkpoints with lane opponent diffs
            foreach (var (participantId, data) in frameData)
            {
                if (!participantIdMap.TryGetValue(participantId, out var dbParticipantId))
                    continue;

                // Find lane opponent (same role, opposite team)
                int? goldDiff = null, csDiff = null;
                bool? isAhead = null;

                if (!string.IsNullOrEmpty(data.role) && data.role != "JUNGLE")
                {
                    var opponent = frameData.Values
                        .Where(o => o.team != data.team && o.role == data.role)
                        .FirstOrDefault();

                    if (opponent.team != 0)
                    {
                        goldDiff = data.gold - opponent.gold;
                        csDiff = data.cs - opponent.cs;
                        isAhead = goldDiff > 0;
                    }
                }

                checkpoints.Add(new ParticipantCheckpoint
                {
                    ParticipantId = dbParticipantId,
                    MinuteMark = minute,
                    Gold = data.gold,
                    Cs = data.cs,
                    Xp = data.xp,
                    GoldDiffVsLane = goldDiff,
                    CsDiffVsLane = csDiff,
                    IsAhead = isAhead,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return checkpoints;
    }

    /// <summary>
    /// Extracts death timing data from timeline events.
    /// Returns a dictionary mapping participantId to (deathsPre10, deaths10To20, deaths20To30, deaths30Plus, firstDeathMinute).
    /// </summary>
    public static Dictionary<int, DeathTimingData> ExtractDeathTimings(JsonElement timelineRoot)
    {
        var result = new Dictionary<int, DeathTimingData>();
        var info = timelineRoot.GetProperty("info");

        foreach (var frame in info.GetProperty("frames").EnumerateArray())
        {
            if (!frame.TryGetProperty("events", out var events)) continue;

            foreach (var evt in events.EnumerateArray())
            {
                if (evt.GetProperty("type").GetString() != "CHAMPION_KILL") continue;

                var victimId = evt.GetProperty("victimId").GetInt32();
                var timestamp = evt.GetProperty("timestamp").GetInt64();
                var minute = (int)(timestamp / 60000);

                if (!result.ContainsKey(victimId))
                    result[victimId] = new DeathTimingData();

                var data = result[victimId];
                data.FirstDeathMinute ??= minute;

                if (minute < 10) data.DeathsPre10++;
                else if (minute < 20) data.Deaths10To20++;
                else if (minute < 30) data.Deaths20To30++;
                else data.Deaths30Plus++;
            }
        }

        return result;
    }

    public class DeathTimingData
    {
        public int DeathsPre10 { get; set; }
        public int Deaths10To20 { get; set; }
        public int Deaths20To30 { get; set; }
        public int Deaths30Plus { get; set; }
        public int? FirstDeathMinute { get; set; }
    }

    /// <summary>
    /// Extracts objective participation from timeline events.
    /// Counts ELITE_MONSTER_KILL (dragon, baron, herald) and BUILDING_KILL (tower) participations per player.
    /// </summary>
    public static Dictionary<int, ObjectiveParticipationData> ExtractObjectiveParticipation(JsonElement timelineRoot)
    {
        var result = new Dictionary<int, ObjectiveParticipationData>();
        var info = timelineRoot.GetProperty("info");

        foreach (var frame in info.GetProperty("frames").EnumerateArray())
        {
            if (!frame.TryGetProperty("events", out var events)) continue;

            foreach (var evt in events.EnumerateArray())
            {
                var eventType = evt.GetProperty("type").GetString();

                if (eventType == "ELITE_MONSTER_KILL")
                {
                    var monsterType = evt.TryGetProperty("monsterType", out var mt) ? mt.GetString() : null;
                    var killerId = evt.TryGetProperty("killerId", out var kid) ? kid.GetInt32() : 0;

                    // Get assisting participants
                    var participants = new List<int>();
                    if (killerId > 0) participants.Add(killerId);
                    if (evt.TryGetProperty("assistingParticipantIds", out var assists))
                    {
                        foreach (var a in assists.EnumerateArray())
                            participants.Add(a.GetInt32());
                    }

                    foreach (var pid in participants)
                    {
                        if (!result.ContainsKey(pid))
                            result[pid] = new ObjectiveParticipationData();

                        var data = result[pid];
                        switch (monsterType)
                        {
                            case "DRAGON": data.Dragons++; break;
                            case "RIFTHERALD": data.Heralds++; break;
                            case "BARON_NASHOR": data.Barons++; break;
                        }
                    }
                }
                else if (eventType == "BUILDING_KILL")
                {
                    var buildingType = evt.TryGetProperty("buildingType", out var bt) ? bt.GetString() : null;
                    if (buildingType != "TOWER_BUILDING") continue;

                    var killerId = evt.TryGetProperty("killerId", out var kid) ? kid.GetInt32() : 0;
                    var participants = new List<int>();
                    if (killerId > 0) participants.Add(killerId);
                    if (evt.TryGetProperty("assistingParticipantIds", out var assists))
                    {
                        foreach (var a in assists.EnumerateArray())
                            participants.Add(a.GetInt32());
                    }

                    foreach (var pid in participants)
                    {
                        if (!result.ContainsKey(pid))
                            result[pid] = new ObjectiveParticipationData();
                        result[pid].Towers++;
                    }
                }
            }
        }

        return result;
    }

    public class ObjectiveParticipationData
    {
        public int Dragons { get; set; }
        public int Heralds { get; set; }
        public int Barons { get; set; }
        public int Towers { get; set; }
    }

    /// <summary>
    /// Extracts team gold metrics from timeline frames for team_match_metrics table.
    /// </summary>
    public static Dictionary<int, TeamGoldMetrics> ExtractTeamGoldMetrics(
        JsonElement timelineRoot,
        Dictionary<int, int> participantTeams,
        Dictionary<int, bool> teamWins)
    {
        var result = new Dictionary<int, TeamGoldMetrics>
        {
            { 100, new TeamGoldMetrics { TeamId = 100 } },
            { 200, new TeamGoldMetrics { TeamId = 200 } }
        };

        var info = timelineRoot.GetProperty("info");
        var frames = info.GetProperty("frames");

        int? goldAt15Team100 = null;
        int? goldAt20Team100 = null;
        int largestLead100 = 0;
        int largestLead200 = 0;

        foreach (var frame in frames.EnumerateArray())
        {
            var timestamp = frame.GetProperty("timestamp").GetInt64();
            var minute = (int)(timestamp / 60000);
            var participantFrames = frame.GetProperty("participantFrames");

            // Calculate team gold totals
            int team100Gold = 0, team200Gold = 0;
            foreach (var pf in participantFrames.EnumerateObject())
            {
                if (!int.TryParse(pf.Name, out var participantId)) continue;
                var gold = pf.Value.GetProperty("totalGold").GetInt32();
                var team = participantTeams.GetValueOrDefault(participantId, 0);
                if (team == 100) team100Gold += gold;
                else if (team == 200) team200Gold += gold;
            }

            var goldDiff = team100Gold - team200Gold;

            // Track largest leads
            if (goldDiff > largestLead100) largestLead100 = goldDiff;
            if (-goldDiff > largestLead200) largestLead200 = -goldDiff;

            // Capture gold at minute 15
            if (minute == 15)
                goldAt15Team100 = goldDiff;

            // Capture gold at minute 20
            if (minute == 20)
                goldAt20Team100 = goldDiff;
        }

        // Team 100 metrics
        result[100].GoldLeadAt15 = goldAt15Team100;
        result[100].LargestGoldLead = largestLead100 > 0 ? largestLead100 : null;
        if (goldAt20Team100.HasValue && goldAt15Team100.HasValue)
            result[100].GoldSwingPost20 = goldAt20Team100.Value - goldAt15Team100.Value;
        if (goldAt20Team100.HasValue && teamWins.TryGetValue(100, out var win100))
            result[100].WinWhenAheadAt20 = goldAt20Team100 > 0 ? win100 : null;

        // Team 200 metrics (inverted perspective)
        result[200].GoldLeadAt15 = goldAt15Team100.HasValue ? -goldAt15Team100 : null;
        result[200].LargestGoldLead = largestLead200 > 0 ? largestLead200 : null;
        if (goldAt20Team100.HasValue && goldAt15Team100.HasValue)
            result[200].GoldSwingPost20 = -(goldAt20Team100.Value - goldAt15Team100.Value);
        if (goldAt20Team100.HasValue && teamWins.TryGetValue(200, out var win200))
            result[200].WinWhenAheadAt20 = goldAt20Team100 < 0 ? win200 : null;

        return result;
    }

    public class TeamGoldMetrics
    {
        public int TeamId { get; set; }
        public int? GoldLeadAt15 { get; set; }
        public int? LargestGoldLead { get; set; }
        public int? GoldSwingPost20 { get; set; }
        public bool? WinWhenAheadAt20 { get; set; }
    }
}

