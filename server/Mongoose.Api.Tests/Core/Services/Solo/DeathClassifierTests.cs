using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;
using Xunit;

namespace Mongoose.Api.Tests;

public class DeathClassifierTests
{
    // ───────────────────────── Helpers ─────────────────────────

    /// <summary>Blue side: 1 TOP, 2 JUNGLE, 3 MIDDLE, 4 BOTTOM, 5 UTILITY; red side 6-10 in the same order.</summary>
    private static readonly IReadOnlyDictionary<int, MatchParticipantRole> Participants =
        new[] { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" }
            .SelectMany((role, i) => new[]
            {
                new MatchParticipantRole("M", i + 1, 100, role),
                new MatchParticipantRole("M", i + 6, 200, role)
            })
            .ToDictionary(p => p.ParticipantId);

    private static SoloDeathRow Death(
        int timestampSec = 600,
        int? killer = 6,
        int[]? assisters = null,
        int allies = 1,
        string role = "TOP",
        double u = 0.05,
        double v = 0.50) =>
        new("M", 100, role, 1, (int)(u * MapRegions.MapSize), (int)(v * MapRegions.MapSize),
            timestampSec, killer, assisters ?? [], allies);

    private static string How(SoloDeathRow death)
    {
        var (u, v) = MapRegions.Normalize(death.PositionX, death.PositionY, death.VictimTeamId);
        return DeathClassifier.How(death, MapRegions.Classify(u, v), Participants);
    }

    // ───────────────────────── FR 31, 34 ─────────────────────────

    [Fact]
    public void IsCounted_NeedsTimeAndAllies_ButNotAKiller()
    {
        DeathClassifier.IsCounted(Death(killer: null)).Should().BeTrue();
        DeathClassifier.IsCounted(Death() with { TimestampSec = null }).Should().BeFalse();
        DeathClassifier.IsCounted(Death() with { AlliesNearby = null }).Should().BeFalse();
    }

    [Theory]
    [InlineData(839, DeathClassifier.Early)]
    [InlineData(840, DeathClassifier.Mid)]
    [InlineData(1499, DeathClassifier.Mid)]
    [InlineData(1500, DeathClassifier.Late)]
    public void Phase_SplitsAt14And25Minutes(int timestampSec, string expected)
    {
        DeathClassifier.Phase(timestampSec).Should().Be(expected);
    }

    // ───────────────────────── FR 35 ─────────────────────────

    [Fact]
    public void How_TeamfightComesFirst_WithThreeInvolvedAndTwoAllies()
    {
        How(Death(killer: 6, assisters: [7, 8], allies: 2)).Should().Be(DeathClassifier.Teamfight);
        How(Death(killer: 6, assisters: [7], allies: 2)).Should().NotBe(DeathClassifier.Teamfight);
        How(Death(killer: 6, assisters: [7, 8], allies: 1)).Should().NotBe(DeathClassifier.Teamfight);
    }

    [Fact]
    public void How_GankedInLane_WhenSomeoneOtherThanTheLaneOpponentTookPart()
    {
        // Top laner dies in top lane before 14:00 to the enemy jungler (7), or with their help
        How(Death(killer: 7)).Should().Be(DeathClassifier.Ganked);
        How(Death(killer: 6, assisters: [7])).Should().Be(DeathClassifier.Ganked);
    }

    [Fact]
    public void How_NotGanked_WhenOnlyTheLaneOpponentKilled()
    {
        How(Death(killer: 6, allies: 1)).Should().Be(DeathClassifier.Other);
    }

    [Fact]
    public void How_NotGanked_AfterFourteenMinutes_OrOutsideTheLane()
    {
        How(Death(timestampSec: 840, killer: 7, allies: 1)).Should().Be(DeathClassifier.Other);
        How(Death(killer: 7, allies: 1, u: 0.25, v: 0.55)).Should().Be(DeathClassifier.Other); // own jungle
    }

    [Fact]
    public void How_CaughtAlone_WithNoAlliesNearby()
    {
        How(Death(killer: 6, allies: 0)).Should().Be(DeathClassifier.Alone);
        // A gank still reads as a gank, even alone
        How(Death(killer: 7, allies: 0)).Should().Be(DeathClassifier.Ganked);
    }

    // ───────────────────────── FR 36 ─────────────────────────

    private static SoloObjectiveRow Objective(string type, int team, int timestampSec) => new("M", team, type, timestampSec);

    [Theory]
    [InlineData(660, "dragon")]
    [InlineData(661, null)]
    [InlineData(599, null)]
    public void Cost_CountsAnEnemyObjectiveUpTo60SecondsAfter(int objectiveAt, string? expected)
    {
        DeathClassifier.Cost(Death(timestampSec: 600), [Objective("dragon", 200, objectiveAt)]).Should().Be(expected);
    }

    [Fact]
    public void Cost_IsTheFirstObjectiveOnly()
    {
        var objectives = new[] { Objective("tower", 200, 640), Objective("dragon", 200, 620) };

        DeathClassifier.Cost(Death(timestampSec: 600), objectives).Should().Be("dragon");
    }

    [Fact]
    public void Cost_IgnoresOwnTeamsObjectives_GrubsAndInhibitors()
    {
        var objectives = new[] { Objective("dragon", 100, 610), Objective("grubs", 200, 620), Objective("inhibitor", 200, 630) };

        DeathClassifier.Cost(Death(timestampSec: 600), objectives).Should().BeNull();
    }
}
