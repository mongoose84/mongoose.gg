# LP Estimation Implementation Guide

## Overview
This document describes the implementation strategy for calculating historical LP (League Points) values for ranked matches. The Riot API only provides current LP, not historical LP per match, so we need to estimate LP progression by working backwards from the current known LP.

## Problem Statement

### Current Situation
- The Riot API only provides **current LP**, not historical LP per match
- Historical matches in the `participants` table have `lp_after = NULL`
- Only the most recent ranked match gets `lp_after` populated (via `UpdateLpForMostRecentRankedMatchAsync()`)
- Users see incomplete LP progression charts (only 4-5 data points instead of 20+ matches)

### Goal
Calculate estimated LP values for the last 20 ranked matches by working backwards from current LP, enabling complete LP progression charts.

## Solution Architecture

### Two-Part Approach

#### 1. One-Time Backfill Script (Initial Rollout)
- **Purpose**: Populate historical LP data for all existing users
- **When**: Run once after deploying the LP estimation feature
- **Scope**: All riot accounts, last 20 matches per queue (Solo/Duo and Flex)
- **Implementation**: Standalone console application (`Mongoose.LpBackfill`)

#### 2. Lazy Calculation (Ongoing, Edge Cases)
- **Purpose**: Handle new users or missed matches
- **When**: On-demand when viewing LP chart, if matches are missing LP data
- **Scope**: Individual users as needed
- **Implementation**: Integrated into `TrendRepository.GetLpTrendAsync()`

### Why This Approach?
1. **One-time problem**: Historical matches are missing LP because the feature didn't exist before
2. **Going forward**: New matches will have actual LP from `UpdateLpForMostRecentRankedMatchAsync()`
3. **Clean separation**: Backfill handles historical data, normal flow handles new data
4. **No ongoing overhead**: After backfill, everything is just normal database queries
5. **Safety net**: Lazy calculation catches edge cases (new users with old matches)

## Algorithm Overview

### Core Concept: Reverse Calculation
Work backwards from current known LP through match history:

```
Current LP: Gold II 50 LP (known from Riot API)
  ↓
Match 20 (most recent): Win → Reverse: 50 - 20 = 30 LP before match
Match 19: Loss → Reverse: 30 + 17 = 47 LP before match
Match 18: Win → Reverse: 47 - 22 = 25 LP before match
Match 17: Promotion detected (LP > 100) → Was Silver I ~75 LP
  ↓
Continue backwards...
```

### Key Components

#### 1. Remake Detection
- Games < 3.5 minutes (210 seconds) = remake
- LP change = 0 (no gain or loss)

#### 2. LP Gain/Loss Estimation
**Base values (from community data):**
- Win: +20 LP (average)
- Loss: -17 LP (average)

**Streak adjustments:**
- Win streak: +2 LP per consecutive win (max +10 bonus)
- Loss streak: +2 LP per consecutive loss (max +8 penalty)

**Example:**
- Win with 3-game win streak: 20 + (3 × 2) = 26 LP gain
- Loss with 2-game loss streak: 17 + (2 × 2) = 21 LP loss

#### 3. Promotion Handling (Reverse)
When calculated LP > 100:
- Player was promoted from previous division
- Example: Gold IV 110 LP → Was Silver I ~75 LP
- Estimate previous LP at 75 (typical promotion point)

#### 4. Demotion Handling (Reverse)
When calculated LP < 0:
- Player was demoted from higher division
- Example: Silver I -10 LP → Was Gold IV ~25 LP
- Estimate previous LP at 25 (demotion protection buffer)

#### 5. Confidence Tracking
- Most recent match: 95% confidence
- Confidence decreases by 5% per game backwards
- 10 games back: ~45% confidence
- 20 games back: ~20% confidence
- Promotions/demotions reduce confidence further (×0.8)

## Database Schema Changes

### Add Columns to `participants` Table

```sql
ALTER TABLE participants
ADD COLUMN is_lp_estimated BOOLEAN DEFAULT FALSE,
ADD COLUMN lp_estimation_version VARCHAR(10) NULL,
ADD COLUMN lp_estimation_confidence DECIMAL(3,2) NULL,
ADD COLUMN lp_estimated_at TIMESTAMP NULL;

-- Index for finding matches needing estimation
CREATE INDEX idx_participants_lp_estimation 
ON participants(puuid, lp_after, is_lp_estimated);
```

### Column Descriptions
- `is_lp_estimated`: TRUE if LP was calculated (not from Riot API)
- `lp_estimation_version`: Algorithm version used (e.g., "1.0.0")
- `lp_estimation_confidence`: Confidence score 0.0-1.0 (e.g., 0.85 = 85%)
- `lp_estimated_at`: Timestamp when estimation was performed

## Implementation Structure

### Clean Architecture Layers

```
Core/
  ├── Entities/
  │   └── LpEstimate.cs                    # Data model for LP estimate
  ├── Interfaces/
  │   ├── ILpEstimationService.cs          # Service interface
  │   └── ILpBackfillService.cs            # Backfill service interface
  └── ValueObjects/
      └── LpCalculationConfig.cs           # Configuration constants

Application/
  ├── Services/
  │   └── LpEstimationService.cs           # Main estimation logic
  └── DTOs/
      └── LpEstimationResult.cs            # Result DTOs

Infrastructure/
  ├── Services/
  │   ├── StreakCalculator.cs              # Win/loss streak calculation
  │   ├── RankTransitionHandler.cs         # Promotion/demotion logic
  │   └── LpBackfillService.cs             # Backfill orchestration
  └── Database/
      └── Repositories/
          └── ParticipantsRepository.cs    # Add BatchUpdateLpEstimatesAsync()

Mongoose.LpBackfill/                       # Standalone console app
  ├── Program.cs                           # Entry point
  └── Mongoose.LpBackfill.csproj
```

## Configuration Constants

### LpCalculationConfig.cs
All magic numbers are centralized in configuration:

```csharp
public class LpCalculationConfig
{
    // Remake detection
    public int RemakeThresholdSeconds { get; init; } = 210; // 3.5 minutes

    // Base LP changes (from community data)
    public int BaseLpGain { get; init; } = 20;
    public int BaseLpLoss { get; init; } = 17;

    // Streak bonuses
    public int StreakBonusPerWin { get; init; } = 2;
    public int MaxStreakBonus { get; init; } = 10;
    public int StreakPenaltyPerLoss { get; init; } = 2;
    public int MaxStreakPenalty { get; init; } = 8;

    // Rank transition estimates
    public int PromotionEstimateLp { get; init; } = 75;
    public int DemotionEstimateLp { get; init; } = 25;

    // Confidence calculation
    public double ConfidenceDecayRate { get; init; } = 0.05; // 5% per game
    public double BaseConfidence { get; init; } = 0.95; // 95% for most recent

    // Algorithm version for auditability
    public string Version { get; init; } = "1.0.0";
}
```

### appsettings.json
```json
{
  "LpCalculation": {
    "RemakeThresholdSeconds": 210,
    "BaseLpGain": 20,
    "BaseLpLoss": 17,
    "StreakBonusPerWin": 2,
    "MaxStreakBonus": 10,
    "Version": "1.0.0"
  }
}
```

## Backfill Script Implementation

### Purpose
One-time script to populate historical LP data for all existing users.

### How It Works

```
FOR EACH riot_account in database:
    FOR EACH queue (Solo, Flex):
        1. Get current rank from riot_accounts table
        2. Find last 20 ranked matches for this queue
        3. Check: How many matches have lp_after = NULL?
        4. IF any missing:
            - Calculate estimated LP working backwards
            - Batch UPDATE participants table with estimates
            - Mark as estimated (is_lp_estimated = true)
        5. Log progress and stats
```

### Key Features

#### 1. Dry Run Mode
Test the backfill without making database changes:
```bash
dotnet run -- --dry-run
```

#### 2. Batch Updates
Update multiple matches in a single SQL query for performance:
```csharp
// Instead of 20 individual UPDATEs (slow)
// Use single batch UPDATE with CASE statements (fast)
UPDATE participants
SET
    lp_after = CASE
        WHEN match_id = 'match1' THEN 50
        WHEN match_id = 'match2' THEN 30
        ...
    END,
    tier_after = CASE ...
WHERE match_id IN ('match1', 'match2', ...)
  AND lp_after IS NULL
```

#### 3. Safety Checks
- Only update matches where `lp_after IS NULL` (never overwrite actual LP)
- Transaction support (rollback on error)
- Progress reporting every 100 accounts
- Comprehensive error logging

#### 4. Parallel Processing (Optional)
Process multiple accounts concurrently for speed:
```csharp
await Parallel.ForEachAsync(accounts,
    new ParallelOptions { MaxDegreeOfParallelism = 10 },
    async (account, ct) => await BackfillAccountAsync(account.Puuid, ct)
);
```

### Running the Backfill

#### Development
```bash
cd server/Mongoose.LpBackfill
dotnet run
```

#### Production
```bash
# Build
dotnet publish -c Release -o ./publish

# Dry run first (preview changes)
./publish/Mongoose.LpBackfill --dry-run

# Actual run
./publish/Mongoose.LpBackfill

# With specific connection string
./publish/Mongoose.LpBackfill --ConnectionStrings:Database="Server=..."
```

#### With Docker
```bash
docker run --rm \
  -e Database_production="Server=..." \
  mongoose-lp-backfill:latest
```

### Expected Output
```
[INFO] Starting LP backfill for all accounts
[INFO] Progress: 100/1000 accounts, 1,234 matches backfilled
[INFO] Progress: 200/1000 accounts, 2,567 matches backfilled
...
[INFO] LP backfill completed:
  Accounts processed: 1000/1000
  Matches backfilled: 12,345
  Matches skipped: 3,456 (already had LP)
  Failed accounts: 2
  Duration: 00:01:23
```

### Performance Estimates

**Assumptions:**
- 1,000 users
- Average 15 matches per user needing backfill
- 15,000 total matches to backfill

**Timing:**
- Calculate LP for 20 matches: ~50ms
- Batch update 20 matches: ~20ms
- **Per user: ~70ms**
- **Sequential: 1,000 users × 70ms = 70 seconds**
- **Parallel (10 concurrent): ~7 seconds**

## Lazy Calculation (Ongoing)

### Purpose
Handle edge cases where matches are missing LP data after the backfill (e.g., new users with old matches).

### Integration Point
In `TrendRepository.GetLpTrendAsync()`:

```csharp
public async Task<IList<LpTrendPoint>> GetLpTrendAsync(
    string puuid,
    string? queueType = null,
    int limit = 100)
{
    // 1. Fetch matches from participants table
    var matches = await FetchMatchesFromDatabase(puuid, queueType, limit);

    // 2. Check if any matches need LP estimation
    var matchesNeedingEstimation = matches
        .Where(m => m.LpAfter == null && !m.IsLpEstimated)
        .ToList();

    if (matchesNeedingEstimation.Any())
    {
        // This should rarely happen after backfill
        _logger.LogInformation(
            "Found {Count} matches needing LP estimation for {Puuid}",
            matchesNeedingEstimation.Count,
            puuid
        );

        // 3. Get current rank for this queue
        var currentRank = await GetCurrentRank(puuid, queueType);

        if (currentRank != null)
        {
            // 4. Calculate estimated LP
            var estimates = await _lpEstimationService.CalculateHistoricalLpAsync(
                puuid,
                queueType,
                currentRank.Lp,
                currentRank.Tier,
                currentRank.Division,
                maxMatches: 20
            );

            // 5. Persist estimates to database (cache for future requests)
            await _participantsRepo.BatchUpdateLpEstimatesAsync(estimates.Value);

            // 6. Re-fetch matches to get updated data
            matches = await FetchMatchesFromDatabase(puuid, queueType, limit);
        }
    }

    // 7. Convert to LpTrendPoint and return
    return ConvertToLpTrendPoints(matches);
}
```

### When It Runs
- **Rarely**: Only for new users or if backfill missed some accounts
- **On-demand**: When user views LP chart
- **Cached**: Results are persisted to database for future requests

## Algorithm Pseudocode

```
FUNCTION CalculateHistoricalLp(puuid, queueType, currentLp, currentTier, currentDivision, maxMatches):
    // 1. Validate inputs
    IF currentLp < 0 OR currentLp > 100:
        RETURN Error("Invalid LP")
    IF currentTier NOT IN [IRON, BRONZE, SILVER, GOLD, PLATINUM, EMERALD, DIAMOND]:
        RETURN Error("Invalid tier or unsupported tier (Master+)")

    // 2. Fetch last N ranked matches for this queue
    matches = GetRecentRankedMatches(puuid, queueType, maxMatches)
    IF matches.IsEmpty:
        RETURN Empty list

    // 3. Pre-calculate streaks for performance (O(n) instead of O(n²))
    streaks = StreakCalculator.CalculateAllStreaks(matches)

    // 4. Initialize state
    results = []
    lp = currentLp
    tier = currentTier
    division = currentDivision

    // 5. Iterate backwards through matches (newest to oldest)
    FOR i = 0 TO matches.Length - 1:
        match = matches[i]

        // Skip if match already has actual LP data
        IF match.lp_after IS NOT NULL:
            results.Add(CreateActualLpEstimate(match))
            lp = match.lp_after
            tier = match.tier_after
            division = match.rank_after
            CONTINUE

        // Store current state (LP AFTER this match)
        estimate = CreateLpEstimate(
            matchId: match.match_id,
            lpAfter: lp,
            tierAfter: tier,
            divisionAfter: division,
            confidence: CalculateConfidence(i, hadRankChange),
            isEstimated: true
        )
        results.Add(estimate)

        // Calculate LP BEFORE this match (reverse the change)
        IF IsRemake(match):
            lpChange = 0
            estimate.WasRemake = true
        ELSE IF match.win:
            // Reverse a win: subtract LP gain
            lpChange = -EstimateLpGain(tier, division, streaks[i].ConsecutiveWins)
        ELSE:
            // Reverse a loss: add back LP loss
            lpChange = EstimateLpLoss(tier, division, streaks[i].ConsecutiveLosses)

        estimate.LpChange = -lpChange  // Store the actual change (not reversed)

        // Apply LP change
        newLp = lp + lpChange

        // Handle rank transitions
        IF newLp > 100:
            // Reverse promotion
            (tier, division) = GetPreviousDivision(tier, division)
            newLp = config.PromotionEstimateLp
            estimate.WasPromotion = true
            LOG.Warning("Detected promotion boundary", matchId, tier, division)
        ELSE IF newLp < 0:
            // Reverse demotion
            (tier, division) = GetNextDivision(tier, division)
            newLp = config.DemotionEstimateLp
            estimate.WasDemotion = true
            LOG.Warning("Detected demotion boundary", matchId, tier, division)

        lp = Clamp(newLp, 0, 100)

    // 6. Reverse results (we built them backwards)
    RETURN Reverse(results)


FUNCTION CalculateConfidence(gamesBack, hadRankChange):
    baseConfidence = config.BaseConfidence  // 0.95
    confidence = baseConfidence - (gamesBack * config.ConfidenceDecayRate)  // -5% per game

    // Reduce confidence if rank change detected
    IF hadRankChange:
        confidence = confidence * 0.8

    RETURN Max(confidence, 0.1)  // Minimum 10% confidence


FUNCTION EstimateLpGain(tier, division, winStreak):
    baseGain = config.BaseLpGain  // 20
    streakBonus = Min(winStreak * config.StreakBonusPerWin, config.MaxStreakBonus)
    RETURN baseGain + streakBonus


FUNCTION EstimateLpLoss(tier, division, lossStreak):
    baseLoss = config.BaseLpLoss  // 17
    streakPenalty = Min(lossStreak * config.StreakPenaltyPerLoss, config.MaxStreakPenalty)
    RETURN baseLoss + streakPenalty


FUNCTION IsRemake(match):
    RETURN match.game_duration < config.RemakeThresholdSeconds  // 210 seconds
```

## Edge Cases

### 1. Remakes
- **Detection**: Game duration < 210 seconds (3.5 minutes)
- **LP Change**: 0 (no gain or loss)
- **Handling**: Skip LP calculation, mark as remake

### 2. Promotions (Reverse)
- **Detection**: Calculated LP > 100
- **Action**: Move to previous division (e.g., Gold IV → Silver I)
- **Estimate**: Set LP to 75 (typical promotion point)
- **Confidence**: Reduce by 20%

### 3. Demotions (Reverse)
- **Detection**: Calculated LP < 0
- **Action**: Move to next division (e.g., Silver I → Gold IV)
- **Estimate**: Set LP to 25 (demotion protection buffer)
- **Confidence**: Reduce by 20%

### 4. Win/Loss Streaks
- **Detection**: Count consecutive wins/losses before each match
- **Impact**: Adjust LP gain/loss (+2 LP per streak, max +10)
- **Performance**: Pre-calculate all streaks once (O(n) instead of O(n²))

### 5. Master+ Tier
- **Problem**: LP has no upper bound (can be 0-3000+)
- **Solution**: Return error - unsupported tier
- **Reason**: Different LP system, no divisions

### 6. Placement Matches
- **Problem**: Different LP rules during placements
- **Solution**: Return error - cannot estimate
- **Detection**: Check if match is in placement period

### 7. Mixed Actual/Estimated Data
- **Scenario**: Some matches have actual LP, others don't
- **Solution**: Use actual LP where available, estimate only missing
- **Implementation**: Check `lp_after IS NOT NULL` before estimating

### 8. No Matches
- **Scenario**: User has no ranked matches
- **Solution**: Return empty list
- **No error**: Valid state for new players

### 9. All Matches Have LP
- **Scenario**: Backfill already completed or all matches are recent
- **Solution**: Return actual data, skip estimation
- **Performance**: Fast path, no calculation needed

### 10. LP Clamping
- **Scenario**: Calculated LP goes out of bounds (< 0 or > 100)
- **Solution**: Clamp to valid range and log warning
- **Logging**: Track how often this happens for algorithm tuning

## Testing Requirements

### Unit Tests (Minimum 10 Required)

Create `LpEstimationServiceTests.cs` in `Mongoose.Api.Tests/`:

1. **Test_IsRemake_GameUnder210Seconds_ReturnsTrue**
2. **Test_IsRemake_GameOver210Seconds_ReturnsFalse**
3. **Test_EstimateLpGain_NoStreak_ReturnsBaseGain**
4. **Test_EstimateLpGain_WithWinStreak_ReturnsBaseGainPlusBonus**
5. **Test_EstimateLpGain_LongWinStreak_CapsAtMaxBonus**
6. **Test_EstimateLpLoss_NoStreak_ReturnsBaseLoss**
7. **Test_EstimateLpLoss_WithLossStreak_ReturnsBaseLossPlusPenalty**
8. **Test_HandlePromotionBoundary_LpOver100_ReturnsPreviousDivision**
9. **Test_HandleDemotionBoundary_LpUnder0_ReturnsNextDivision**
10. **Test_CalculateHistoricalLp_SimpleCase_ReturnsCorrectProgression**
11. **Test_CalculateHistoricalLp_WithPromotion_DetectsRankChange**
12. **Test_CalculateHistoricalLp_WithRemake_NoLpChange**
13. **Test_CalculateConfidence_RecentMatch_HighConfidence**
14. **Test_CalculateConfidence_OldMatch_LowConfidence**
15. **Test_ValidateInputs_InvalidLp_ReturnsError**
16. **Test_ValidateInputs_MasterTier_ReturnsError**

### Integration Tests (Optional)
1. Test with real match data from database
2. Verify estimated LP matches actual LP (where both exist)
3. Test backfill script on staging database
4. Performance test with 1000+ accounts

## Logging Requirements

### Log Levels and Events

**Info:**
- Calculation started (puuid, queue, num matches)
- Calculation completed (num estimated, avg confidence)
- Backfill started (total accounts)
- Backfill progress (every 100 accounts)
- Backfill completed (statistics)

**Warning:**
- LP clamped (match_id, calculated_lp, clamped_lp, reason)
- Promotion/demotion detected (match_id, old_rank, new_rank)
- Low confidence (<50%) (match_id, confidence, games_back)
- Account skipped (puuid, reason)

**Error:**
- Invalid inputs (puuid, details)
- Calculation failed (puuid, exception)
- Database update failed (match_id, exception)
- Backfill failed for account (puuid, exception)

### Example Log Output
```
[INFO] Starting LP backfill for all accounts
[INFO] Processing account abc123 (1/1000)
[DEBUG] Calculating LP for 15 matches in RANKED_SOLO_5x5
[WARNING] Detected promotion boundary: match xyz789, Silver I → Gold IV
[INFO] Backfilled 15 matches for abc123 (avg confidence: 0.72)
[INFO] Progress: 100/1000 accounts, 1,234 matches backfilled
[ERROR] Failed to backfill account def456: Invalid tier 'MASTER'
[INFO] LP backfill completed: 998/1000 accounts, 12,345 matches, 2 failures
```

## Metrics to Track

### Counters
- `lp_estimation.calculations_total` - Total calculations performed
- `lp_estimation.promotions_detected` - Promotions detected
- `lp_estimation.demotions_detected` - Demotions detected
- `lp_estimation.remakes_detected` - Remakes detected
- `lp_estimation.errors_total` - Calculation errors
- `lp_estimation.backfill_accounts_processed` - Accounts processed in backfill
- `lp_estimation.backfill_accounts_failed` - Accounts failed in backfill

### Histograms
- `lp_estimation.matches_estimated` - Distribution of matches estimated per calculation
- `lp_estimation.calculation_duration_ms` - Time to calculate LP
- `lp_estimation.batch_update_duration_ms` - Time to update database

### Gauges
- `lp_estimation.average_confidence` - Average confidence score
- `lp_estimation.backfill_progress_percent` - Backfill completion percentage

## Scenarios and Expected Behavior

### Scenario 1: New User (First Time Viewing LP Chart)
```
1. User creates account and links Riot account
2. MatchHistorySyncJob syncs last 20 matches
3. Only most recent match has lp_after (from UpdateLpForMostRecentRankedMatchAsync)
4. User visits Solo page
5. TrendRepository detects 19 matches missing lp_after
6. Calculates estimated LP for those 19 matches
7. Persists estimates to database
8. Returns complete LP progression (1 actual + 19 estimated)
9. Future requests use cached estimates (fast)
```

### Scenario 2: Existing User After Backfill
```
1. Backfill script has already run
2. User visits Solo page
3. TrendRepository queries participants table
4. All matches have lp_after (some actual, some estimated)
5. No calculation needed
6. Returns data immediately (fast)
```

### Scenario 3: User Plays New Match
```
1. User plays ranked game
2. MatchHistorySyncJob syncs new match
3. UpdateLpForMostRecentRankedMatchAsync sets lp_after (actual)
4. User visits Solo page
5. TrendRepository sees new match has actual lp_after
6. No estimation needed for that match
7. Returns data with 1 new actual + 19 old estimates
```

### Scenario 4: Algorithm Version Updated
```
1. Developer updates LpCalculationConfig.Version to "1.1.0"
2. Re-run backfill script with --force flag
3. Script checks lp_estimation_version
4. Finds matches with version "1.0.0" (old)
5. Re-calculates with new algorithm
6. Updates database with new estimates and version "1.1.0"
7. Users see improved estimates
```

### Scenario 5: User With Promotion in History
```
1. User's last 20 matches include a promotion (Silver I → Gold IV)
2. Backfill calculates backwards from current Gold II 50 LP
3. Detects promotion when calculated LP > 100
4. Estimates previous LP at Silver I 75 LP
5. Marks promotion in estimate (WasPromotion = true)
6. Reduces confidence for that match (×0.8)
7. Continues calculating backwards from Silver I 75 LP
```

## Going Forward (After Backfill)

### New Matches
- `MatchHistorySyncJob` continues to run normally
- `UpdateLpForMostRecentRankedMatchAsync()` sets actual LP on most recent match
- **No estimation needed** - all new matches have actual LP
- LP progression chart shows mix of estimated (historical) + actual (recent)

### New Users
- First match sync sets actual LP on most recent match
- Older matches (if any) would need estimation
- **Option 1**: Run backfill for new users on first login (small scale)
- **Option 2**: Lazy calculation on first chart view (recommended)

### Algorithm Updates
- If you improve the algorithm, re-run backfill with `--force` flag
- Check `lp_estimation_version` to find old estimates
- Update only estimates, not actual LP values
- Users automatically get improved estimates

### Monitoring
- Track average confidence scores over time
- Monitor how often LP clamping occurs (indicates algorithm issues)
- Watch for errors in specific tiers/divisions
- Validate estimates against actual LP when available

## Success Criteria

- [ ] All unit tests pass (minimum 10 tests)
- [ ] No compiler warnings or errors
- [ ] Code follows Clean Architecture principles
- [ ] All magic numbers in configuration
- [ ] Comprehensive error handling
- [ ] Logging at appropriate levels
- [ ] XML documentation on all public APIs
- [ ] Confidence scores calculated for all estimates
- [ ] Handles all edge cases listed above
- [ ] Backfill script runs successfully on staging
- [ ] Performance meets targets (<100ms per user)
- [ ] Database schema updated with new columns
- [ ] Frontend displays estimated LP with visual distinction

## Out of Scope

The following are explicitly **NOT** part of this implementation:

- ❌ Machine learning or advanced prediction models
- ❌ Support for Master+ tier (different LP system)
- ❌ Support for placement matches
- ❌ LP decay handling (inactive accounts)
- ❌ MMR estimation
- ❌ User feedback mechanism to improve estimates
- ❌ Frontend changes (separate task)
- ❌ Real-time LP tracking during games
- ❌ LP prediction for future matches

## References

### Riot API Documentation
- Remake threshold: 3 minutes (180 seconds)
- Queue IDs: 420 = Ranked Solo/Duo, 440 = Ranked Flex
- No historical LP data available via API

### Community Data Sources
- Average LP gain: 18-25 LP (most common: 20-22)
- Average LP loss: 15-20 LP (most common: 17-18)
- Win streaks: +2-3 LP per consecutive win (caps around +30 total)
- Loss streaks: +2-3 LP per consecutive loss (caps around -25 total)
- Promotion typically occurs around 75-100 LP in Division I
- Demotion protection typically lasts 2-3 games at 0 LP

### Related Files
- `server/Infrastructure/Jobs/MatchHistorySyncJob.cs` - Match sync logic
- `server/Infrastructure/Database/Repositories/TrendRepository.cs` - LP trend queries
- `server/Application/Services/LpCalculationService.cs` - Absolute LP calculation
- `server/schema.sql` - Database schema
- `docs/api-design-guidelines.md` - API design patterns

## Questions for Clarification

Before implementing, clarify these decisions:

1. Should we persist estimated LP to database or calculate on-demand?
   - **Decision**: Persist to database (backfill + lazy calculation)

2. What should the UI show for estimated vs actual LP?
   - **Suggestion**: Dotted line for estimated, solid line for actual, tooltip shows confidence

3. Should we allow users to manually correct estimated LP values?
   - **Suggestion**: Not in v1.0, consider for future

4. What's the maximum number of matches we should estimate?
   - **Decision**: 20 matches (configurable)

5. Should we expose confidence scores in the API response?
   - **Decision**: Yes, include in LpTrendPoint DTO

6. How should we handle algorithm version updates?
   - **Decision**: Re-run backfill with --force flag, check version in database

7. Should backfill run automatically or manually triggered?
   - **Decision**: Manual trigger (console app or admin endpoint)

## Implementation Checklist

- [ ] Create database migration for new columns
- [ ] Implement `LpCalculationConfig` with all constants
- [ ] Implement `LpEstimate` entity
- [ ] Implement `ILpEstimationService` interface
- [ ] Implement `LpEstimationService` with all methods
- [ ] Implement `StreakCalculator` helper service
- [ ] Implement `RankTransitionHandler` helper service
- [ ] Implement `ILpBackfillService` interface
- [ ] Implement `LpBackfillService` with backfill logic
- [ ] Add `BatchUpdateLpEstimatesAsync()` to ParticipantsRepository
- [ ] Create `Mongoose.LpBackfill` console application
- [ ] Integrate lazy calculation into `TrendRepository.GetLpTrendAsync()`
- [ ] Register services in DI container
- [ ] Add configuration to appsettings.json
- [ ] Write unit tests (minimum 10)
- [ ] Add logging throughout
- [ ] Add metrics tracking
- [ ] Test on staging database
- [ ] Run backfill on production
- [ ] Update API documentation
- [ ] Update this guide with lessons learned

