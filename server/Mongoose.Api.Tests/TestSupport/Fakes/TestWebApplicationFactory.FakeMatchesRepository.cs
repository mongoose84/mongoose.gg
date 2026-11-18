using System.Collections.Concurrent;
using Mongoose.Api.Core;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Storage record for fake match data in tests.
    /// </summary>
    public record FakeMatchData(
        string MatchId,
        int QueueId,
        long GameStartTime,
        int GameDurationSec
    );

    /// <summary>
    /// Storage record for fake participant data in tests.
    /// </summary>
    public record FakeParticipantData(
        string MatchId,
        string Puuid,
        int ChampionId,
        string ChampionName,
        string Role,
        string? Lane,
        bool Win,
        int Kills,
        int Deaths,
        int Assists,
        int CreepScore,
        int GoldEarned,
        int TeamId,
        int? GoldDiffAt10 = null,
        int? GoldAt10 = null,
        int? CsAt10 = null,
        int? CsDiffAt10 = null,
        int DamageDealt = 0,
        int DamageTaken = 0,
        int VisionScore = 0,
        decimal KillParticipation = 0,
        decimal DamageShare = 0,
        int DeathsPre10 = 0,
        int? LpChange = null,
        int? LpAfter = null,
        string? TierAfter = null,
        string? RankAfter = null
    );

    /// <summary>
    /// Fake matches repository for testing match endpoints.
    /// Implements IMatchesRepository directly since MatchesRepository methods aren't virtual.
    /// </summary>
    internal sealed class FakeMatchesRepository : IMatchesRepository
    {
        private readonly ConcurrentDictionary<string, FakeMatchData> _matches = new();
        private readonly ConcurrentDictionary<string, List<FakeParticipantData>> _participants = new();
        private readonly ConcurrentDictionary<string, Dictionary<string, RoleBaseline>> _baselines = new();
        private readonly FakeRiotAccountsRepository _riotAccountsRepo;

        public FakeMatchesRepository(FakeRiotAccountsRepository riotAccountsRepo)
        {
            _riotAccountsRepo = riotAccountsRepo;
        }

        /// <summary>
        /// Helper to add a match for testing.
        /// </summary>
        public void AddMatch(string matchId, int queueId = 420, long? gameStartTime = null, int gameDurationSec = 1800)
        {
            var match = new FakeMatchData(
                MatchId: matchId,
                QueueId: queueId,
                GameStartTime: gameStartTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GameDurationSec: gameDurationSec
            );
            _matches[matchId] = match;
            _participants[matchId] = new List<FakeParticipantData>();
        }

        /// <summary>
        /// Helper to add a participant to a match.
        /// </summary>
        public void AddParticipant(FakeParticipantData participant)
        {
            if (!_participants.TryGetValue(participant.MatchId, out var list))
            {
                list = new List<FakeParticipantData>();
                _participants[participant.MatchId] = list;
            }
            list.Add(participant);
        }

        /// <summary>
        /// Helper to set role baselines for a puuid.
        /// </summary>
        public void SetBaselines(string puuid, Dictionary<string, RoleBaseline> baselines)
        {
            _baselines[puuid] = baselines;
        }

        public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
            string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
        {
            var result = _matches
                .OrderByDescending(m => m.Value.GameStartTime)
                .Select(matchKvp =>
                {
                    var match = matchKvp.Value;
                    if (!_participants.TryGetValue(match.MatchId, out var participants))
                        return null;

                    var participant = participants.FirstOrDefault(p => p.Puuid == puuid);
                    if (participant == null)
                        return null;

                    // Apply queue filter
                    if (!MatchesQueueFilter(match.QueueId, queueFilter))
                        return null;

                    var durationMin = match.GameDurationSec / 60.0;
                    var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
                    var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

                    return new MatchListSummaryItem(
                        MatchId: match.MatchId,
                        AccountGameName: null,
                        AccountTagLine: null,
                        AccountRegion: null,
                        QueueId: match.QueueId,
                        QueueType: GetQueueType(match.QueueId),
                        ChampionId: participant.ChampionId,
                        ChampionName: participant.ChampionName,
                        ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                        Role: participant.Role,
                        Lane: participant.Lane,
                        Win: participant.Win,
                        Kills: participant.Kills,
                        Deaths: participant.Deaths,
                        Assists: participant.Assists,
                        CreepScore: participant.CreepScore,
                        GoldEarned: participant.GoldEarned,
                        GameDurationSec: match.GameDurationSec,
                        GameStartTime: match.GameStartTime,
                        CsPerMin: csPerMin,
                        GoldPerMin: goldPerMin,
                        LpChange: participant.LpChange,
                        LpAfter: participant.LpAfter,
                        TierAfter: participant.TierAfter,
                        RankAfter: participant.RankAfter
                    );
                })
                .Where(item => item != null)
                .Take(limit)
                .ToList();

            return Task.FromResult<IList<MatchListSummaryItem>>(result!);
        }

        public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
            IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
        {
            if (puuids.Count == 0)
                return Task.FromResult<IList<MatchListSummaryItem>>(new List<MatchListSummaryItem>());

            var allowed = puuids.ToHashSet(StringComparer.Ordinal);
            var result = _matches
                .OrderByDescending(m => m.Value.GameStartTime)
                .Select(matchKvp =>
                {
                    var match = matchKvp.Value;
                    if (!_participants.TryGetValue(match.MatchId, out var participants))
                        return null;

                    var participant = participants.FirstOrDefault(p => allowed.Contains(p.Puuid));
                    if (participant == null)
                        return null;

                    if (!MatchesQueueFilter(match.QueueId, queueFilter))
                        return null;

                    var durationMin = match.GameDurationSec / 60.0;
                    var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
                    var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

                    // Look up account info — only populate when multiple accounts are in scope
                    var account = _riotAccountsRepo.GetByPuuid(participant.Puuid);
                    var accountGameName = allowed.Count > 1 ? account?.GameName : null;
                    var accountTagLine = allowed.Count > 1 ? account?.TagLine : null;
                    var accountRegion = allowed.Count > 1 ? account?.Region : null;

                    return new MatchListSummaryItem(
                        MatchId: match.MatchId,
                        AccountGameName: accountGameName,
                        AccountTagLine: accountTagLine,
                        AccountRegion: accountRegion,
                        QueueId: match.QueueId,
                        QueueType: GetQueueType(match.QueueId),
                        ChampionId: participant.ChampionId,
                        ChampionName: participant.ChampionName,
                        ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                        Role: participant.Role,
                        Lane: participant.Lane,
                        Win: participant.Win,
                        Kills: participant.Kills,
                        Deaths: participant.Deaths,
                        Assists: participant.Assists,
                        CreepScore: participant.CreepScore,
                        GoldEarned: participant.GoldEarned,
                        GameDurationSec: match.GameDurationSec,
                        GameStartTime: match.GameStartTime,
                        CsPerMin: csPerMin,
                        GoldPerMin: goldPerMin,
                        LpChange: participant.LpChange,
                        LpAfter: participant.LpAfter,
                        TierAfter: participant.TierAfter,
                        RankAfter: participant.RankAfter
                    );
                })
                .Where(item => item != null)
                .Take(limit)
                .ToList();

            return Task.FromResult<IList<MatchListSummaryItem>>(result!);
        }

        public Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid)
        {
            if (!_matches.TryGetValue(matchId, out var match))
                return Task.FromResult<MatchDetailsItem?>(null);

            if (!_participants.TryGetValue(matchId, out var participants))
                return Task.FromResult<MatchDetailsItem?>(null);

            var participant = participants.FirstOrDefault(p => p.Puuid == puuid);
            if (participant == null)
                return Task.FromResult<MatchDetailsItem?>(null);

            var durationMin = match.GameDurationSec / 60.0;
            var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
            var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

            // Calculate team stats
            var allyParticipants = participants.Where(p => p.TeamId == participant.TeamId).ToList();
            var enemyParticipants = participants.Where(p => p.TeamId != participant.TeamId).ToList();
            var teamKills = allyParticipants.Sum(p => p.Kills);
            var enemyTeamKills = enemyParticipants.Sum(p => p.Kills);
            var teamDamage = allyParticipants.Sum(p => p.DamageDealt);
            var enemyTeamDamage = enemyParticipants.Sum(p => p.DamageDealt);

            var result = new MatchDetailsItem(
                MatchId: match.MatchId,
                QueueId: match.QueueId,
                QueueType: GetQueueType(match.QueueId),
                ChampionId: participant.ChampionId,
                ChampionName: participant.ChampionName,
                ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                Role: participant.Role,
                Lane: participant.Lane,
                Win: participant.Win,
                Kills: participant.Kills,
                Deaths: participant.Deaths,
                Assists: participant.Assists,
                CreepScore: participant.CreepScore,
                GoldEarned: participant.GoldEarned,
                GameDurationSec: match.GameDurationSec,
                GameStartTime: match.GameStartTime,
                DamageDealt: participant.DamageDealt,
                DamageTaken: participant.DamageTaken,
                VisionScore: participant.VisionScore,
                KillParticipation: (double)participant.KillParticipation,
                DamageShare: (double)participant.DamageShare,
                DeathsPre10: participant.DeathsPre10,
                CsPerMin: csPerMin,
                GoldPerMin: goldPerMin,
                TeamKills: teamKills,
                EnemyTeamKills: enemyTeamKills,
                GoldDiffAt15: participant.GoldDiffAt10,
                TeamTotalDamage: teamDamage,
                EnemyTeamTotalDamage: enemyTeamDamage,
                TeamGoldLeadAt15: null,
                TeamDragons: 0,
                EnemyTeamDragons: 0,
                TeamBarons: 0,
                EnemyTeamBarons: 0,
                TeamTowers: 0,
                EnemyTeamTowers: 0,
                DragonsParticipated: 0,
                GoldDiffAt10: participant.GoldDiffAt10,
                CsAt10: participant.CsAt10,
                LpChange: participant.LpChange,
                LpAfter: participant.LpAfter,
                TierAfter: participant.TierAfter,
                RankAfter: participant.RankAfter
            );

            return Task.FromResult<MatchDetailsItem?>(result);
        }

        public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter)
        {
            if (_baselines.TryGetValue(puuid, out var baselines))
                return Task.FromResult(baselines);

            return Task.FromResult(new Dictionary<string, RoleBaseline>());
        }

        public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(IReadOnlyList<string> puuids, string queueFilter)
        {
            foreach (var puuid in puuids)
            {
                if (_baselines.TryGetValue(puuid, out var baselines))
                    return Task.FromResult(baselines);
            }

            return Task.FromResult(new Dictionary<string, RoleBaseline>());
        }

        public Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId)
        {
            if (!_participants.TryGetValue(matchId, out var participants))
                return Task.FromResult<IList<MatchupParticipantRaw>>(new List<MatchupParticipantRaw>());

            var result = participants.Select((p, i) => new MatchupParticipantRaw(
                ParticipantId: i + 1,
                Puuid: p.Puuid,
                ChampionId: p.ChampionId,
                ChampionName: p.ChampionName,
                TeamId: p.TeamId,
                Role: p.Role,
                Win: p.Win,
                Kills: p.Kills,
                Deaths: p.Deaths,
                Assists: p.Assists,
                CreepScore: p.CreepScore,
                GoldEarned: p.GoldEarned,
                KillParticipation: p.KillParticipation,
                DamageShare: p.DamageShare,
                VisionScore: p.VisionScore,
                DeathsPre10: p.DeathsPre10,
                GoldAt10: p.GoldAt10,
                CsAt10: p.CsAt10,
                GoldDiffAt10: p.GoldDiffAt10,
                CsDiffAt10: p.CsDiffAt10
            )).ToList();

            return Task.FromResult<IList<MatchupParticipantRaw>>(result);
        }

        private readonly ConcurrentDictionary<string, GoldTimelineData> _goldTimelines = new();

        /// <summary>Sets the gold frames and objectives a match's gold-lead chart is built from.</summary>
        public void SetGoldTimeline(string matchId, GoldTimelineData data) => _goldTimelines[matchId] = data;

        /// <summary>Null unless the puuid played the match and its gold timeline was set.</summary>
        public Task<GoldTimelineData?> GetGoldTimelineDataAsync(string matchId, string puuid)
        {
            var played = _participants.TryGetValue(matchId, out var participants) && participants.Any(p => p.Puuid == puuid);
            return Task.FromResult(played && _goldTimelines.TryGetValue(matchId, out var data) ? data : null);
        }

        /// <summary>
        /// Mirrors <see cref="MatchesRepository.GetStatUsualsAsync"/>: up to 20 most recent matches
        /// strictly before <paramref name="beforeGameStartTime"/>, same puuid + role, Summoner's
        /// Rift queue, at least the minimum valid duration.
        /// </summary>
        public Task<Dictionary<string, StatUsual>> GetStatUsualsAsync(string puuid, string role, long beforeGameStartTime)
        {
            var rows = _matches.Values
                .Where(m => m.GameStartTime < beforeGameStartTime
                    && m.GameDurationSec >= GameConstants.MinValidGameDurationSec
                    && GameConstants.SummonersRiftQueueIds.Contains(m.QueueId))
                .OrderByDescending(m => m.GameStartTime)
                .SelectMany(m => _participants.TryGetValue(m.MatchId, out var participants)
                    ? participants.Where(p => p.Puuid == puuid && p.Role == role).Select(p => (Match: m, Participant: p))
                    : Enumerable.Empty<(FakeMatchData Match, FakeParticipantData Participant)>())
                .Take(20)
                .ToList();

            var usuals = new Dictionary<string, StatUsual>();
            AddUsual(usuals, "goldLeadAt10", rows.Select(r => (double?)r.Participant.GoldDiffAt10));
            AddUsual(usuals, "csAt10", rows.Select(r => (double?)r.Participant.CsAt10));
            AddUsual(usuals, "deathsBefore10", rows.Select(r => (double?)r.Participant.DeathsPre10));
            AddUsual(usuals, "killParticipation", rows.Select(r => (double?)(double)r.Participant.KillParticipation));
            AddUsual(usuals, "visionPerMin", rows.Select(r => r.Match.GameDurationSec > 0
                ? (double?)(r.Participant.VisionScore / (r.Match.GameDurationSec / 60.0))
                : null));

            return Task.FromResult(usuals);
        }

        private static void AddUsual(Dictionary<string, StatUsual> usuals, string stat, IEnumerable<double?> values)
        {
            var nonNull = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (nonNull.Count == 0)
            {
                usuals[stat] = new StatUsual(0, 0, 0);
                return;
            }

            var average = nonNull.Average();
            var stdDev = 0.0;
            if (nonNull.Count > 1)
            {
                var variance = nonNull.Sum(v => (v - average) * (v - average)) / (nonNull.Count - 1);
                stdDev = Math.Sqrt(variance);
            }

            usuals[stat] = new StatUsual(average, stdDev, nonNull.Count);
        }

        // Required interface methods with minimal implementation
        public Task UpsertAsync(Match match) => Task.CompletedTask;
        public Task<long> GetTotalMatchCountAsync() => Task.FromResult((long)_matches.Count);
        public Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit)
            => Task.FromResult<IList<Match>>(new List<Match>());
        public Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
            => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
        public Task<IList<MatchListItem>> GetMatchListAsync(IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
            => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
        public Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize) => Task.FromResult(0);

        private static bool MatchesQueueFilter(int queueId, string queueFilter)
        {
            if (string.IsNullOrEmpty(queueFilter) || queueFilter == "AND 1=1")
                return true;
            if (queueFilter.Contains("420") && queueId == 420) return true;
            if (queueFilter.Contains("440") && queueId == 440) return true;
            if (queueFilter.Contains("450") && queueId == 450) return true;
            if (queueFilter.Contains("400") && queueId == 400) return true;
            return queueFilter == "AND 1=1";
        }

        private static string GetQueueType(int queueId) => queueId switch
        {
            420 => "ranked_solo",
            440 => "ranked_flex",
            450 => "aram",
            400 => "normal",
            _ => "other"
        };
    }
}
