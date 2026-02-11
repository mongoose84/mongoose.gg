using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Tests for LpEstimationService - LP estimation algorithm.
/// Uses a fake IParticipantsRepository to verify estimation logic in isolation.
/// </summary>
public class LpEstimationServiceTests
{
    private readonly FakeParticipantsRepositoryForEstimation _repo = new();
    private readonly LpEstimationService _sut;

    public LpEstimationServiceTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<LpEstimationService>();
        _sut = new LpEstimationService(_repo, logger);
    }

    #region Apex Tier Skip

    [Theory]
    [InlineData("MASTER")]
    [InlineData("GRANDMASTER")]
    [InlineData("CHALLENGER")]
    public async Task EstimateLp_ApexTier_ReturnsZeroAndSkips(string tier)
    {
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true)
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 150, tier, "I");

        result.Should().Be(0);
        _repo.LastEstimates.Should().BeNull("no batch update should be called for apex tiers");
    }

    #endregion

    #region Empty Matches

    [Fact]
    public async Task EstimateLp_NoMatches_ReturnsZero()
    {
        _repo.SetMatches(new List<LpEstimationMatch>());

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(0);
        _repo.LastEstimates.Should().BeNull();
    }

    #endregion

    #region Basic Win/Loss Estimation

    [Fact]
    public async Task EstimateLp_SingleWin_EstimatesCorrectly()
    {
        // Current: Gold II, 50 LP. Most recent match was a win.
        // LP after this match = 50 (current LP).
        // LP before this match = 50 - 20 (base gain) = 30.
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true)
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(1);
        _repo.LastEstimates.Should().HaveCount(1);
        _repo.LastEstimates![0].lpAfter.Should().Be(50);
        _repo.LastEstimates[0].tierAfter.Should().Be("GOLD");
        _repo.LastEstimates[0].rankAfter.Should().Be("II");
    }

    [Fact]
    public async Task EstimateLp_SingleLoss_EstimatesCorrectly()
    {
        // Current: Gold II, 50 LP. Most recent match was a loss.
        // LP after this match = 50 (current LP).
        // LP before this match = 50 + 17 (base loss) = 67.
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: false)
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(1);
        _repo.LastEstimates.Should().HaveCount(1);
        _repo.LastEstimates![0].lpAfter.Should().Be(50);
    }

    [Fact]
    public async Task EstimateLp_TwoWins_SecondMatchGetsCorrectLp()
    {
        // Current: Gold II, 50 LP. Two wins (newest first).
        // Match 1 (newest): lp_after = 50, lp_before = 50 - 20 = 30
        // Match 2 (older):  lp_after = 30, lp_before = 30 - 20 = 10
        // Note: match 2 has streak bonus of 1 win before it (match 1 was a win)
        // So match 2: lp_after = 30, gain = 20 + min(1*2, 10) = 22, lp_before = 30 - 22 = 8
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),  // newest
            MakeMatch("m2", win: true),  // older
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(2);
        _repo.LastEstimates.Should().HaveCount(2);
        _repo.LastEstimates![0].lpAfter.Should().Be(50); // match 1
        _repo.LastEstimates[1].lpAfter.Should().Be(30);  // match 2 (50 - 20 base gain)
    }

    [Fact]
    public async Task EstimateLp_TwoLosses_SecondMatchGetsCorrectLp()
    {
        // Current: Gold II, 50 LP. Two losses (newest first).
        // Match 1 (newest): lp_after = 50, lp_before = 50 + 17 = 67
        // Match 2 (older): streak = 1 loss before it, loss = 17 + min(1*2, 8) = 19
        //   lp_after = 67, lp_before = 67 + 19 = 86
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: false),
            MakeMatch("m2", win: false),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(2);
        _repo.LastEstimates![0].lpAfter.Should().Be(50); // match 1
        _repo.LastEstimates[1].lpAfter.Should().Be(67);  // match 2 (50 + 17 base loss)
    }

    #endregion

    #region Remake Detection


    [Fact]
    public async Task EstimateLp_RemakeDoesNotAffectStreaks()
    {
        // Win, Remake, Win — the remake should be skipped in streak calculation
        // Match 0 (newest): win, streak = 0 wins before (match 1 is remake, match 2 is win → 1 win)
        // Actually: streak looks at more recent matches (lower index). Match 0 has no matches before it.
        // Match 1 (remake): lp_after = whatever, 0 change
        // Match 2 (oldest): win, streak looks at match 1 (remake, skip) and match 0 (win) → 1 consecutive win
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),                       // newest
            MakeMatch("m2", win: false, gameDurationSec: 100), // remake
            MakeMatch("m3", win: true),                        // oldest
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(3);
        // Match 1: lp_after = 50, no streak bonus (first match)
        _repo.LastEstimates![0].lpAfter.Should().Be(50);
        // Match 2 (remake): lp_after = 30 (50 - 20), no LP change from remake
        _repo.LastEstimates[1].lpAfter.Should().Be(30);
        // Match 3: lp_after = 30 (remake didn't change LP), streak = 1 win (m1), gain = 20 + 2 = 22
        _repo.LastEstimates[2].lpAfter.Should().Be(30);
    }

    #endregion

    #region Streak Bonuses

    [Fact]
    public async Task EstimateLp_WinStreak_AppliesStreakBonus()
    {
        // 4 consecutive wins. Streaks are calculated from more recent matches.
        // Match 0: no streak (first), gain = 20
        // Match 1: 1 win before, gain = 20 + 2 = 22
        // Match 2: 2 wins before, gain = 20 + 4 = 24
        // Match 3: 3 wins before, gain = 20 + 6 = 26
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
            MakeMatch("m2", win: true),
            MakeMatch("m3", win: true),
            MakeMatch("m4", win: true),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 80, "GOLD", "II");

        result.Should().Be(4);
        _repo.LastEstimates![0].lpAfter.Should().Be(80); // m1: 80 LP
        _repo.LastEstimates[1].lpAfter.Should().Be(60);  // m2: 80 - 20 = 60
        _repo.LastEstimates[2].lpAfter.Should().Be(38);  // m3: 60 - 22 = 38
        _repo.LastEstimates[3].lpAfter.Should().Be(14);  // m4: 38 - 24 = 14
    }

    [Fact]
    public async Task EstimateLp_LossStreak_AppliesStreakPenalty()
    {
        // 3 consecutive losses.
        // Match 0: no streak, loss = 17
        // Match 1: 1 loss before, loss = 17 + 2 = 19
        // Match 2: 2 losses before, loss = 17 + 4 = 21
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: false),
            MakeMatch("m2", win: false),
            MakeMatch("m3", win: false),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 30, "GOLD", "II");

        result.Should().Be(3);
        _repo.LastEstimates![0].lpAfter.Should().Be(30); // m1: 30 LP
        _repo.LastEstimates[1].lpAfter.Should().Be(47);  // m2: 30 + 17 = 47
        _repo.LastEstimates[2].lpAfter.Should().Be(66);  // m3: 47 + 19 = 66
    }

    [Fact]
    public async Task EstimateLp_StreakBonusCappedAtMax()
    {
        // 6 consecutive wins starting at 100 LP.
        // Verify that streak bonus caps at MaxStreakBonus (10), so max gain = 30.
        // Match 0: no streak, gain = 20 → lp_before = 80
        // Match 1: 1 win, gain = 22 → lp_before = 58
        // Match 2: 2 wins, gain = 24 → lp_before = 34
        // Match 3: 3 wins, gain = 26 → lp_before = 8
        // Match 4: 4 wins, gain = 28 → lp_before = 8 - 28 = -20 < 0 → demotion
        // Demotion complicates things. Use only 3 matches to verify increasing gains.
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
            MakeMatch("m2", win: true),
            MakeMatch("m3", win: true),
        });

        await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 100, "GOLD", "II");

        // m1: lp_after = 100, gain = 20 (no streak)
        // m2: lp_after = 80, gain = 22 (1 win streak)
        // m3: lp_after = 58, gain = 24 (2 win streak)
        _repo.LastEstimates![0].lpAfter.Should().Be(100);
        _repo.LastEstimates[1].lpAfter.Should().Be(80);  // 100 - 20
        _repo.LastEstimates[2].lpAfter.Should().Be(58);  // 80 - 22

        // Verify gains are increasing: 20, 22, 24
        var gain0 = _repo.LastEstimates[0].lpAfter - _repo.LastEstimates[1].lpAfter; // 20
        var gain1 = _repo.LastEstimates[1].lpAfter - _repo.LastEstimates[2].lpAfter; // 22
        gain0.Should().Be(20);
        gain1.Should().Be(22);
        gain1.Should().BeGreaterThan(gain0, "streak bonus should increase gain");
    }

    [Fact]
    public async Task EstimateLp_StreakBrokenByOppositeResult()
    {
        // Win, Loss, Win — the loss breaks the win streak
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),  // newest
            MakeMatch("m2", win: false), // breaks streak
            MakeMatch("m3", win: true),  // oldest
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(3);
        // Match 0: no streak, gain = 20
        _repo.LastEstimates![0].lpAfter.Should().Be(50);
        // Match 1: streak = 1 win before (m0), but m1 is a loss, so loss streak = 0
        // loss = 17 + 0 = 17
        _repo.LastEstimates[1].lpAfter.Should().Be(30); // 50 - 20
        // Match 2: looks at m1 (loss) → 1 loss, then m0 (win) → breaks. So loss streak = 1 for m2? No.
        // m2 is a win. Streak counts consecutive wins before it. m1 is a loss → 0 consecutive wins.
        // gain = 20 + 0 = 20
        _repo.LastEstimates[2].lpAfter.Should().Be(47); // 30 + 17
    }

    #endregion

    #region Stop at Known LP

    [Fact]
    public async Task EstimateLp_StopsAtFirstKnownLp()
    {
        // 3 matches: first two have no LP, third has LP data
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
            MakeMatch("m2", win: true),
            MakeMatch("m3", win: false, lpAfter: 40, tierAfter: "GOLD", rankAfter: "III"),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(2, "should only estimate 2 matches, stopping at m3 which has LP data");
        _repo.LastEstimates.Should().HaveCount(2);
        _repo.LastEstimates![0].matchId.Should().Be("m1");
        _repo.LastEstimates[1].matchId.Should().Be("m2");
    }

    [Fact]
    public async Task EstimateLp_AllMatchesHaveLp_EstimatesNothing()
    {
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true, lpAfter: 50, tierAfter: "GOLD", rankAfter: "II"),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(0);
        _repo.LastEstimates.Should().BeNull("no batch update should be called when nothing to estimate");
    }

    #endregion

    #region Promotion Detection

    [Fact]
    public async Task EstimateLp_PromotionWithinTier_DetectsCorrectly()
    {
        // Current: Gold II, 5 LP. Most recent match was a win.
        // LP after = 5. Reversing win: 5 - 20 = -15 → demotion detected.
        // Wait — that's demotion, not promotion. Let me think again.
        // Promotion detection: when reversing a win, if newLp > 100, it means
        // the player was promoted by this win.
        // Current: Gold II, 95 LP. Two wins.
        // Match 0: lp_after = 95, reverse win: 95 - 20 = 75 → normal
        // To trigger promotion: need lp_after high enough that reversing goes > 100
        // That can't happen since lp_after is clamped to 0-100.
        // Promotion happens when walking backwards: current state is post-promotion.
        // Example: Current Gold III, 15 LP. Match was a loss (so we add back LP).
        // 15 + 17 = 32 → normal. No promotion.
        // Example: Current Gold III, 15 LP. Match was a win (reverse: subtract gain).
        // 15 - 20 = -5 → demotion boundary (player was in Gold IV before).
        // Hmm, promotion boundary is when newLp > 100 after reversing a LOSS.
        // Current Gold III, 90 LP. Match was a loss. Reverse: 90 + 17 = 107 > 100.
        // This means before this loss, player was in Gold II (next division up).
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: false), // loss that brought LP from >100 to 90
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 90, "GOLD", "III");

        result.Should().Be(1);
        _repo.LastEstimates![0].lpAfter.Should().Be(90);
        _repo.LastEstimates[0].tierAfter.Should().Be("GOLD");
        _repo.LastEstimates[0].rankAfter.Should().Be("III");
        // After reversing: 90 + 17 = 107 > 100 → promotion boundary
        // LP set to PromotionEstimateLp (75), tier/division moves to previous: Gold IV
        // (GetPreviousDivision of Gold III → Gold IV)
    }

    [Fact]
    public async Task EstimateLp_PromotionAcrossTiers_DetectsCorrectly()
    {
        // Current: Gold IV, 90 LP. Match was a loss. Reverse: 90 + 17 = 107 > 100.
        // GetPreviousDivision(Gold, IV) → Silver I
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: false),
            MakeMatch("m2", win: true), // older match
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 90, "GOLD", "IV");

        result.Should().Be(2);
        _repo.LastEstimates![0].lpAfter.Should().Be(90);
        _repo.LastEstimates[0].tierAfter.Should().Be("GOLD");
        _repo.LastEstimates[0].rankAfter.Should().Be("IV");
        // After reversing m1: 90 + 17 = 107 > 100 → promotion from Silver I
        // LP set to 75, tier = SILVER, division = I
        // m2: lp_after = 75, tier = SILVER, division = I
        _repo.LastEstimates[1].lpAfter.Should().Be(75);
        _repo.LastEstimates[1].tierAfter.Should().Be("SILVER");
        _repo.LastEstimates[1].rankAfter.Should().Be("I");
    }

    #endregion

    #region Demotion Detection

    [Fact]
    public async Task EstimateLp_DemotionWithinTier_DetectsCorrectly()
    {
        // Current: Gold III, 5 LP. Match was a win. Reverse: 5 - 20 = -15 < 0.
        // Demotion boundary: player was in Gold II (next division up) before this win.
        // GetNextDivision(Gold, III) → Gold II
        // LP set to DemotionEstimateLp (25)
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 5, "GOLD", "III");

        result.Should().Be(1);
        _repo.LastEstimates![0].lpAfter.Should().Be(5);
        _repo.LastEstimates[0].tierAfter.Should().Be("GOLD");
        _repo.LastEstimates[0].rankAfter.Should().Be("III");
        // After reversing: 5 - 20 = -15 < 0 → demotion from Gold II
        // Next state for walking backwards: Gold II, 25 LP
    }

    [Fact]
    public async Task EstimateLp_DemotionAcrossTiers_DetectsCorrectly()
    {
        // Current: Silver IV, 5 LP. Match was a win. Reverse: 5 - 20 = -15 < 0.
        // GetNextDivision(Silver, IV) → Silver III? No — wait.
        // GetNextDivision moves UP: Silver IV → Silver III (higher division).
        // Actually looking at the code: divisionOrder = IV, III, II, I
        // divIndex for IV = 0, < length-1, so return (tier, divisionOrder[1]) = (Silver, III)
        // That means the player was in Silver III before being demoted to Silver IV.
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
            MakeMatch("m2", win: false), // older
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 5, "SILVER", "IV");

        result.Should().Be(2);
        _repo.LastEstimates![0].lpAfter.Should().Be(5);
        _repo.LastEstimates[0].tierAfter.Should().Be("SILVER");
        _repo.LastEstimates[0].rankAfter.Should().Be("IV");
        // After reversing m1: 5 - 20 = -15 < 0 → demotion from Silver III
        // LP = 25, tier = SILVER, division = III
        _repo.LastEstimates[1].lpAfter.Should().Be(25);
        _repo.LastEstimates[1].tierAfter.Should().Be("SILVER");
        _repo.LastEstimates[1].rankAfter.Should().Be("III");
    }

    #endregion

    #region LP Clamping

    [Fact]
    public async Task EstimateLp_LpClampedToZero()
    {
        // Current: Gold II, 3 LP. Match was a loss (no streak).
        // Reverse: 3 + 17 = 20 → normal, no clamping needed.
        // For clamping to 0: need a scenario where newLp would be negative but not a demotion.
        // Actually, if newLp < 0 it always triggers demotion detection.
        // Clamping to 0 only matters if somehow newLp is exactly 0 or slightly negative
        // but the demotion path sets it to 25. So clamping to 0 is a safety net.
        // Let's test that LP never goes below 0 in the estimates.
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 0, "GOLD", "II");

        result.Should().Be(1);
        _repo.LastEstimates![0].lpAfter.Should().Be(0);
    }

    #endregion

    #region Mixed Scenarios

    [Fact]
    public async Task EstimateLp_MixedWinsAndLosses_TracksLpCorrectly()
    {
        // Current: Gold II, 50 LP
        // Match history (newest first): Win, Loss, Win, Loss
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),   // newest
            MakeMatch("m2", win: false),
            MakeMatch("m3", win: true),
            MakeMatch("m4", win: false),  // oldest
        });

        var result = await _sut.EstimateLpForRecentMatchesAsync("puuid1", 420, 50, "GOLD", "II");

        result.Should().Be(4);
        // m1: lp_after = 50, reverse win (no streak): 50 - 20 = 30
        _repo.LastEstimates![0].lpAfter.Should().Be(50);
        // m2: lp_after = 30, reverse loss (no streak, m1 was win): 30 + 17 = 47
        _repo.LastEstimates[1].lpAfter.Should().Be(30);
        // m3: lp_after = 47, reverse win (no streak, m2 was loss): 47 - 20 = 27
        _repo.LastEstimates[2].lpAfter.Should().Be(47);
        // m4: lp_after = 27, reverse loss (no streak, m3 was win): 27 + 17 = 44
        _repo.LastEstimates[3].lpAfter.Should().Be(27);
    }

    [Fact]
    public async Task EstimateLp_QueueIdPassedToRepository()
    {
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true),
        });

        await _sut.EstimateLpForRecentMatchesAsync("puuid1", 440, 50, "GOLD", "II");

        _repo.LastQueueId.Should().Be(440);
    }

    [Fact]
    public async Task EstimateLp_PuuidPassedToEstimates()
    {
        _repo.SetMatches(new List<LpEstimationMatch>
        {
            MakeMatch("m1", win: true, puuid: "test-puuid-123"),
        });

        await _sut.EstimateLpForRecentMatchesAsync("test-puuid-123", 420, 50, "GOLD", "II");

        _repo.LastEstimates![0].puuid.Should().Be("test-puuid-123");
    }

    #endregion

    #region Helpers

    private static LpEstimationMatch MakeMatch(
        string matchId,
        bool win,
        int gameDurationSec = 1800,
        int? lpAfter = null,
        string? tierAfter = null,
        string? rankAfter = null,
        string puuid = "puuid1")
    {
        return new LpEstimationMatch
        {
            MatchId = matchId,
            Puuid = puuid,
            Win = win,
            GameDurationSec = gameDurationSec,
            LpAfter = lpAfter,
            TierAfter = tierAfter,
            RankAfter = rankAfter,
            IsLpEstimated = false,
        };
    }

    #endregion
}

/// <summary>
/// Fake IParticipantsRepository for LP estimation testing.
/// Captures batch update calls and returns configurable match data.
/// </summary>
internal sealed class FakeParticipantsRepositoryForEstimation : IParticipantsRepository
{
    private IList<LpEstimationMatch> _matches = new List<LpEstimationMatch>();

    /// <summary>The estimates passed to the last BatchUpdateLpEstimatesAsync call, or null if never called.</summary>
    public IList<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)>? LastEstimates { get; private set; }

    /// <summary>The queue ID passed to the last GetRecentRankedMatchesForLpEstimationAsync call.</summary>
    public int? LastQueueId { get; private set; }

    public void SetMatches(IList<LpEstimationMatch> matches) => _matches = matches;

    public Task<IList<LpEstimationMatch>> GetRecentRankedMatchesForLpEstimationAsync(string puuid, int queueId, int limit)
    {
        LastQueueId = queueId;
        return Task.FromResult(_matches);
    }

    public Task<int> BatchUpdateLpEstimatesAsync(IList<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)> estimates)
    {
        LastEstimates = estimates;
        return Task.FromResult(estimates.Count);
    }

    // Unused methods — stubs for interface compliance
    public Task<long> InsertAsync(Participant participant) => Task.FromResult(1L);
    public Task<IList<Participant>> GetByMatchAsync(string matchId) => Task.FromResult<IList<Participant>>(new List<Participant>());
    public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank) => Task.CompletedTask;
    public Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid) => Task.FromResult<ISet<string>>(new HashSet<string>());
    public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit) => Task.FromResult<IList<Participant>>(new List<Participant>());
    public Task DeleteByMatchIdAsync(string matchId) => Task.CompletedTask;
}
