using System.Text.Json;
using FluentAssertions;
using Mongoose.Api.Infrastructure.Riot.Mappers;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Tests for RiotMatchMapper - Riot API match-v5 JSON to domain entity transformation.
/// These tests validate data extraction and transformation from Riot's match-v5 format.
/// </summary>
public class RiotMatchMapperTests
{
    #region Test Data Helpers

    /// <summary>
    /// Creates a minimal valid match JSON structure for testing.
    /// </summary>
    private static JsonElement CreateMatchJson(
        string matchId = "EUW1_7234567890",
        int queueId = 420,
        int gameDuration = 1800,
        long gameStartTimestamp = 1704067200000,
        string gameVersion = "14.24.123456",
        List<ParticipantData>? participants = null,
        List<TeamData>? teams = null)
    {
        participants ??= CreateDefaultParticipants();
        teams ??= CreateDefaultTeams();

        var matchJson = new
        {
            metadata = new { matchId },
            info = new
            {
                queueId,
                gameDuration,
                gameStartTimestamp,
                gameVersion,
                participants = participants.Select(p => new
                {
                    puuid = p.Puuid,
                    teamId = p.TeamId,
                    teamPosition = p.TeamPosition,
                    lane = p.Lane,
                    championId = p.ChampionId,
                    championName = p.ChampionName,
                    win = p.Win,
                    kills = p.Kills,
                    deaths = p.Deaths,
                    assists = p.Assists,
                    totalMinionsKilled = p.TotalMinionsKilled,
                    neutralMinionsKilled = p.NeutralMinionsKilled,
                    goldEarned = p.GoldEarned,
                    totalTimeSpentDead = p.TotalTimeSpentDead,
                    totalDamageDealtToChampions = p.TotalDamageDealtToChampions,
                    totalDamageTaken = p.TotalDamageTaken,
                    damageSelfMitigated = p.DamageSelfMitigated,
                    visionScore = p.VisionScore
                }).ToList(),
                teams = teams.Select(t => new
                {
                    teamId = t.TeamId,
                    objectives = new
                    {
                        dragon = new { kills = t.Dragons },
                        riftHerald = new { kills = t.Heralds },
                        baron = new { kills = t.Barons },
                        tower = new { kills = t.Towers }
                    }
                }).ToList()
            }
        };

        var jsonString = JsonSerializer.Serialize(matchJson);
        return JsonDocument.Parse(jsonString).RootElement;
    }

    private static List<ParticipantData> CreateDefaultParticipants()
    {
        return new List<ParticipantData>
        {
            new("puuid-1", 100, "TOP", "TOP", 1, "Darius", true, 5, 2, 8, 150, 30, 12000, 120, 18000, 25000, 8000, 45),
            new("puuid-2", 100, "JUNGLE", "JUNGLE", 28, "Jarvan IV", true, 3, 4, 12, 80, 120, 10000, 180, 12000, 20000, 6000, 52),
            new("puuid-3", 100, "MIDDLE", "MIDDLE", 7, "LeBlanc", true, 8, 3, 5, 180, 10, 13000, 90, 25000, 12000, 3000, 38),
            new("puuid-4", 100, "BOTTOM", "BOTTOM", 51, "Jinx", true, 12, 1, 4, 220, 5, 15000, 30, 35000, 10000, 2000, 28),
            new("puuid-5", 100, "UTILITY", "UTILITY", 12, "Thresh", true, 2, 2, 18, 20, 0, 8000, 150, 5000, 15000, 4000, 85),
            new("puuid-6", 200, "TOP", "TOP", 54, "Malphite", false, 1, 5, 3, 140, 25, 9000, 250, 8000, 35000, 15000, 35),
            new("puuid-7", 200, "JUNGLE", "JUNGLE", 64, "Lee Sin", false, 2, 6, 4, 70, 130, 8500, 280, 10000, 18000, 5000, 42),
            new("puuid-8", 200, "MIDDLE", "MIDDLE", 157, "Yasuo", false, 4, 7, 2, 160, 15, 11000, 320, 18000, 14000, 4000, 30),
            new("puuid-9", 200, "BOTTOM", "BOTTOM", 222, "Zeri", false, 3, 6, 3, 200, 8, 12000, 350, 15000, 11000, 2500, 25),
            new("puuid-10", 200, "UTILITY", "UTILITY", 223, "Tahm Kench", false, 0, 6, 6, 25, 0, 7000, 280, 3000, 28000, 12000, 65)
        };
    }

    private static List<TeamData> CreateDefaultTeams()
    {
        return new List<TeamData>
        {
            new(100, 3, 2, 1, 9),
            new(200, 1, 0, 0, 3)
        };
    }

    private record ParticipantData(
        string Puuid, int TeamId, string TeamPosition, string Lane, int ChampionId, string ChampionName,
        bool Win, int Kills, int Deaths, int Assists, int TotalMinionsKilled, int NeutralMinionsKilled,
        int GoldEarned, int TotalTimeSpentDead, int TotalDamageDealtToChampions, int TotalDamageTaken,
        int DamageSelfMitigated, int VisionScore);

    private record TeamData(int TeamId, int Dragons, int Heralds, int Barons, int Towers);

    #endregion

    #region MapMatch Tests

    [Fact]
    public void MapMatch_ExtractsMatchId()
    {
        var json = CreateMatchJson(matchId: "EUW1_9999999999");
        var result = RiotMatchMapper.MapMatch(json);
        result.MatchId.Should().Be("EUW1_9999999999");
    }

    [Fact]
    public void MapMatch_ExtractsQueueId()
    {
        var json = CreateMatchJson(queueId: 440);
        var result = RiotMatchMapper.MapMatch(json);
        result.QueueId.Should().Be(440);
    }

    [Fact]
    public void MapMatch_ExtractsGameDuration()
    {
        var json = CreateMatchJson(gameDuration: 2400);
        var result = RiotMatchMapper.MapMatch(json);
        result.GameDurationSec.Should().Be(2400);
    }

    [Fact]
    public void MapMatch_ExtractsGameStartTimestamp()
    {
        var json = CreateMatchJson(gameStartTimestamp: 1704067200000);
        var result = RiotMatchMapper.MapMatch(json);
        result.GameStartTime.Should().Be(1704067200000);
    }

    [Theory]
    [InlineData("14.24.123456", "14.24")]
    [InlineData("15.1.456789", "15.1")]
    [InlineData("14.10.987654", "14.10")]
    public void MapMatch_ExtractsPatchVersion_FromGameVersion(string gameVersion, string expectedPatch)
    {
        var json = CreateMatchJson(gameVersion: gameVersion);
        var result = RiotMatchMapper.MapMatch(json);
        result.PatchVersion.Should().Be(expectedPatch);
    }

    [Fact]
    public void MapMatch_SetsSeasonCodeToNull()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapMatch(json);
        result.SeasonCode.Should().BeNull();
    }

    [Fact]
    public void MapMatch_SetsCreatedAtToCurrentTime()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapMatch(json);
        var after = DateTime.UtcNow.AddSeconds(1);

        result.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    #endregion

    #region MapParticipants Tests

    [Fact]
    public void MapParticipants_Returns10Participants()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapParticipants(json);
        result.Should().HaveCount(10);
    }

    [Fact]
    public void MapParticipants_ExtractsMatchIdForEachParticipant()
    {
        var json = CreateMatchJson(matchId: "EUW1_1234567890");
        var result = RiotMatchMapper.MapParticipants(json);
        result.Should().AllSatisfy(p => p.MatchId.Should().Be("EUW1_1234567890"));
    }

    [Fact]
    public void MapParticipants_ExtractsBasicStats()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapParticipants(json);

        var firstParticipant = result.First(p => p.Puuid == "puuid-1");
        firstParticipant.TeamId.Should().Be(100);
        firstParticipant.Role.Should().Be("TOP");
        firstParticipant.Lane.Should().Be("TOP");
        firstParticipant.ChampionId.Should().Be(1);
        firstParticipant.ChampionName.Should().Be("Darius");
        firstParticipant.Win.Should().BeTrue();
        firstParticipant.Kills.Should().Be(5);
        firstParticipant.Deaths.Should().Be(2);
        firstParticipant.Assists.Should().Be(8);
    }

    [Fact]
    public void MapParticipants_CalculatesCreepScore_FromMinionsAndNeutralMinions()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapParticipants(json);

        // puuid-1 has TotalMinionsKilled=150 + NeutralMinionsKilled=30 = 180
        var topLaner = result.First(p => p.Puuid == "puuid-1");
        topLaner.CreepScore.Should().Be(180);

        // puuid-2 (jungler) has TotalMinionsKilled=80 + NeutralMinionsKilled=120 = 200
        var jungler = result.First(p => p.Puuid == "puuid-2");
        jungler.CreepScore.Should().Be(200);
    }

    [Fact]
    public void MapParticipants_ExtractsGoldAndTimeSpentDead()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapParticipants(json);

        var adc = result.First(p => p.Puuid == "puuid-4");
        adc.GoldEarned.Should().Be(15000);
        adc.TimeDeadSec.Should().Be(30);
    }

    [Fact]
    public void MapParticipants_DistinguishesWinnersFromLosers()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapParticipants(json);

        var team100 = result.Where(p => p.TeamId == 100);
        var team200 = result.Where(p => p.TeamId == 200);

        team100.Should().AllSatisfy(p => p.Win.Should().BeTrue());
        team200.Should().AllSatisfy(p => p.Win.Should().BeFalse());
    }

    #endregion

    #region MapTeamObjectives Tests

    [Fact]
    public void MapTeamObjectives_ReturnsTwoTeams()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapTeamObjectives(json);
        result.Should().HaveCount(2);
    }

    [Fact]
    public void MapTeamObjectives_ExtractsMatchIdForEachTeam()
    {
        var json = CreateMatchJson(matchId: "NA1_5555555555");
        var result = RiotMatchMapper.MapTeamObjectives(json);
        result.Should().AllSatisfy(t => t.MatchId.Should().Be("NA1_5555555555"));
    }

    [Fact]
    public void MapTeamObjectives_ExtractsObjectivesCorrectly()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapTeamObjectives(json);

        var team100 = result.First(t => t.TeamId == 100);
        team100.DragonsTaken.Should().Be(3);
        team100.HeraldsTaken.Should().Be(2);
        team100.BaronsTaken.Should().Be(1);
        team100.TowersTaken.Should().Be(9);

        var team200 = result.First(t => t.TeamId == 200);
        team200.DragonsTaken.Should().Be(1);
        team200.HeraldsTaken.Should().Be(0);
        team200.BaronsTaken.Should().Be(0);
        team200.TowersTaken.Should().Be(3);
    }

    #endregion

    #region MapParticipantMetricFromInfo Tests

    [Fact]
    public void MapParticipantMetricFromInfo_CalculatesKillParticipation()
    {
        // Player with 5 kills + 10 assists on a team with 30 total kills = 50% KP
        var participantJson = CreateParticipantJsonElement(kills: 5, assists: 10, damage: 10000, vision: 40,
            damageTaken: 15000, damageMitigated: 5000);

        var result = RiotMatchMapper.MapParticipantMetricFromInfo(participantJson, gameDurationSec: 1800,
            teamTotalKills: 30, teamTotalDamage: 50000);

        result.KillParticipationPct.Should().Be(50m);
    }

    [Fact]
    public void MapParticipantMetricFromInfo_CalculatesDamageShare()
    {
        // Player with 10000 damage on team with 50000 total = 20% damage share
        var participantJson = CreateParticipantJsonElement(kills: 3, assists: 5, damage: 10000, vision: 30,
            damageTaken: 12000, damageMitigated: 4000);

        var result = RiotMatchMapper.MapParticipantMetricFromInfo(participantJson, gameDurationSec: 1800,
            teamTotalKills: 20, teamTotalDamage: 50000);

        result.DamageSharePct.Should().Be(20m);
    }

    [Fact]
    public void MapParticipantMetricFromInfo_CalculatesVisionPerMin()
    {
        // 30 minute game (1800 sec) with 60 vision score = 2.0 vision/min
        var participantJson = CreateParticipantJsonElement(kills: 2, assists: 8, damage: 8000, vision: 60,
            damageTaken: 10000, damageMitigated: 3000);

        var result = RiotMatchMapper.MapParticipantMetricFromInfo(participantJson, gameDurationSec: 1800,
            teamTotalKills: 25, teamTotalDamage: 40000);

        result.VisionPerMin.Should().Be(2.0m);
    }

    [Fact]
    public void MapParticipantMetricFromInfo_HandlesZeroTeamTotals()
    {
        var participantJson = CreateParticipantJsonElement(kills: 0, assists: 0, damage: 0, vision: 10,
            damageTaken: 5000, damageMitigated: 1000);

        var result = RiotMatchMapper.MapParticipantMetricFromInfo(participantJson, gameDurationSec: 300,
            teamTotalKills: 0, teamTotalDamage: 0);

        result.KillParticipationPct.Should().Be(0);
        result.DamageSharePct.Should().Be(0);
    }

    [Fact]
    public void MapParticipantMetricFromInfo_InitializesTimelineFieldsToDefaults()
    {
        var participantJson = CreateParticipantJsonElement(kills: 5, assists: 5, damage: 15000, vision: 40,
            damageTaken: 20000, damageMitigated: 8000);

        var result = RiotMatchMapper.MapParticipantMetricFromInfo(participantJson, gameDurationSec: 1800,
            teamTotalKills: 20, teamTotalDamage: 60000);

        // Timeline fields should be initialized to defaults
        result.DeathsPre10.Should().Be(0);
        result.Deaths10To20.Should().Be(0);
        result.Deaths20To30.Should().Be(0);
        result.Deaths30Plus.Should().Be(0);
        result.FirstDeathMinute.Should().BeNull();
        result.FirstKillParticipationMinute.Should().BeNull();
    }

    private static JsonElement CreateParticipantJsonElement(int kills, int assists, int damage, int vision,
        int damageTaken, int damageMitigated)
    {
        var participant = new
        {
            kills,
            assists,
            totalDamageDealtToChampions = damage,
            visionScore = vision,
            totalDamageTaken = damageTaken,
            damageSelfMitigated = damageMitigated
        };

        var jsonString = JsonSerializer.Serialize(participant);
        return JsonDocument.Parse(jsonString).RootElement;
    }

    #endregion

    #region MapTeamRoleResponsibilities Tests

    [Fact]
    public void MapTeamRoleResponsibilities_Returns10RoleResponsibilities()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapTeamRoleResponsibilities(json);

        // 5 roles per team × 2 teams = 10
        result.Should().HaveCount(10);
    }

    [Fact]
    public void MapTeamRoleResponsibilities_ExtractsMatchId()
    {
        var json = CreateMatchJson(matchId: "KR_1234567890");
        var result = RiotMatchMapper.MapTeamRoleResponsibilities(json);

        result.Should().AllSatisfy(r => r.MatchId.Should().Be("KR_1234567890"));
    }

    [Fact]
    public void MapTeamRoleResponsibilities_HasAllRolesForEachTeam()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapTeamRoleResponsibilities(json);

        var team100Roles = result.Where(r => r.TeamId == 100).Select(r => r.Role).ToList();
        var team200Roles = result.Where(r => r.TeamId == 200).Select(r => r.Role).ToList();

        team100Roles.Should().BeEquivalentTo(new[] { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" });
        team200Roles.Should().BeEquivalentTo(new[] { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" });
    }

    [Fact]
    public void MapTeamRoleResponsibilities_SharePercentagesSumTo100()
    {
        var json = CreateMatchJson();
        var result = RiotMatchMapper.MapTeamRoleResponsibilities(json);

        var team100 = result.Where(r => r.TeamId == 100).ToList();
        var team200 = result.Where(r => r.TeamId == 200).ToList();

        // Sum of percentages should be ~100 (allowing for rounding)
        team100.Sum(r => r.DeathsSharePct).Should().BeApproximately(100m, 0.1m);
        team100.Sum(r => r.GoldSharePct).Should().BeApproximately(100m, 0.1m);
        team100.Sum(r => r.DamageSharePct).Should().BeApproximately(100m, 0.1m);

        team200.Sum(r => r.DeathsSharePct).Should().BeApproximately(100m, 0.1m);
        team200.Sum(r => r.GoldSharePct).Should().BeApproximately(100m, 0.1m);
        team200.Sum(r => r.DamageSharePct).Should().BeApproximately(100m, 0.1m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MapMatch_HandlesMinimalValidData()
    {
        var minimalJson = CreateMatchJson(
            matchId: "TEST_1",
            queueId: 420,
            gameDuration: 0,
            gameStartTimestamp: 0,
            gameVersion: "14.1"
        );

        var result = RiotMatchMapper.MapMatch(minimalJson);

        result.MatchId.Should().Be("TEST_1");
        result.QueueId.Should().Be(420);
        result.GameDurationSec.Should().Be(0);
        result.PatchVersion.Should().Be("14.1");
    }

    #endregion
}

