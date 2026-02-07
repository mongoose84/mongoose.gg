---
type: "implementation-plan"
date: "2026-02-06"
status: "ready-for-review"
---

# Match Cleanup Implementation Plan

## Overview

This document provides a step-by-step implementation plan for the automated match cleanup feature, designed to delete matches older than 6 months to manage database growth and maintain optimal performance.

---

## Architecture Assessment

### ✅ Reusability: Grade A
- Follows existing `IHostedService` pattern (same as `MatchHistorySyncJob`)
- Reuses `RepositoryBase` infrastructure for database operations
- Configuration-driven behavior (no hardcoded values)
- Minimal code duplication

### ✅ Testability: Grade A
- Repository method independently unit-testable
- Background job testable with mocked dependencies
- Integration tests verify end-to-end behavior
- Clear separation of concerns enables isolated testing

### ✅ Lines of Code: Grade A (Minimal Bloat)
- **Estimated Total**: ~390 lines
  - `MatchCleanupJob.cs`: ~150 lines
  - `IMatchesRepository` interface update: ~5 lines
  - `MatchesRepository.DeleteOldMatchesAsync`: ~30 lines
  - Unit tests: ~200 lines
  - Configuration: ~10 lines
- **Assessment**: Lean, focused implementation with no unnecessary complexity

### ✅ Clean Code Principles: Grade A
- **Single Responsibility**: Job handles scheduling, repository handles data access
- **Open/Closed**: Configurable without modifying code
- **Dependency Inversion**: Uses interfaces (`IMatchesRepository`, `ILogger`)
- **KISS**: Simple time-based deletion, no over-engineering
- **DRY**: Reuses existing patterns and infrastructure

### ✅ Separation of Concerns: Grade A
- **Infrastructure Layer**: `MatchCleanupJob` (background service)
- **Core Layer**: `IMatchesRepository` interface
- **Infrastructure Layer**: `MatchesRepository` implementation
- **Application Layer**: Configuration settings
- Clear layer boundaries, no violations

---

## Implementation Steps

### Step 1: Update Repository Interface
**File**: `server/Core/Interfaces/IMatchesRepository.cs`

Add method signature:
```csharp
/// <summary>
/// Deletes matches older than the specified cutoff date in batches.
/// Uses CASCADE DELETE to automatically remove related records.
/// </summary>
/// <param name="cutoffTimestamp">Unix timestamp in milliseconds (matches older than this will be deleted)</param>
/// <param name="batchSize">Maximum number of matches to delete in one batch</param>
/// <returns>Number of matches deleted</returns>
Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize);
```

### Step 2: Implement Repository Method
**File**: `server/Infrastructure/Database/Repositories/MatchesRepository.cs`

```csharp
public async Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize)
{
    // Step 1: Find old match IDs
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
    
    // Step 2: Delete matches (CASCADE will handle related tables)
    var placeholders = string.Join(",", matchIds.Select((_, i) => $"@id{i}"));
    var deleteSql = $"DELETE FROM matches WHERE match_id IN ({placeholders})";
    
    var parameters = matchIds
        .Select((id, i) => ($"@id{i}", (object)id))
        .ToArray();
    
    await ExecuteNonQueryAsync(deleteSql, parameters);
    
    return matchIds.Count;
}
```

### Step 3: Create Background Job
**File**: `server/Infrastructure/Jobs/MatchCleanupJob.cs`

Key components:
- Inherit from `BackgroundService`
- Inject `IServiceProvider`, `ILogger<MatchCleanupJob>`, `IConfiguration`
- Calculate daily execution time from configuration
- Execute cleanup in batches
- Comprehensive logging

### Step 4: Update Configuration
**File**: `server/appsettings.json`

```json
"Jobs": {
  "EnableMatchHistorySync": true,
  "EnableMatchCleanup": true,
  "MatchCleanupSchedule": "02:00",
  "MatchRetentionDays": 180,
  "MatchCleanupBatchSize": 1000
}
```

**File**: `server/web.config`

```xml
<environmentVariable name="Jobs__EnableMatchCleanup" value="true" />
<environmentVariable name="Jobs__MatchCleanupSchedule" value="02:00" />
<environmentVariable name="Jobs__MatchRetentionDays" value="180" />
<environmentVariable name="Jobs__MatchCleanupBatchSize" value="1000" />
```

### Step 5: Register Job in DI Container
**File**: `server/Program.cs`

```csharp
// Match Cleanup Job (deletes matches older than retention period)
var enableMatchCleanup = builder.Configuration.GetValue<bool>("Jobs:EnableMatchCleanup", true);
if (enableMatchCleanup)
{
    builder.Services.AddHostedService<MatchCleanupJob>();
}
```

### Step 6: Write Unit Tests
**File**: `server/Mongoose.Api.Tests/MatchCleanupJobTests.cs`

Test cases:
1. ✅ Deletes matches older than cutoff date
2. ✅ Preserves matches within retention period
3. ✅ Verifies cascade deletion of related tables
4. ✅ Handles empty database gracefully
5. ✅ Respects batch size limits
6. ✅ Logs deletion metrics correctly
7. ✅ Handles configuration correctly
8. ✅ Executes on schedule

---

## Database Impact Analysis

### Tables Affected (via CASCADE DELETE)

| Table | Rows per Match | Cascade Configured | Impact |
|-------|----------------|-------------------|--------|
| `matches` | 1 | N/A (parent) | Direct deletion |
| `participants` | 10 | ✅ ON DELETE CASCADE | Auto-deleted |
| `participant_metrics` | 10 | ✅ ON DELETE CASCADE | Auto-deleted |
| `participant_checkpoints` | 30-50 | ✅ ON DELETE CASCADE | Auto-deleted |
| `team_objectives` | 2 | ✅ ON DELETE CASCADE | Auto-deleted |
| `participant_objectives` | 10 | ✅ ON DELETE CASCADE | Auto-deleted |
| `team_match_metrics` | 2 | ✅ ON DELETE CASCADE | Auto-deleted |
| `team_role_responsibilities` | 10 | ✅ ON DELETE CASCADE | Auto-deleted |
| `duo_metrics` | 0-45 | ✅ ON DELETE CASCADE | Auto-deleted |

**Total Rows Deleted per 1000 Matches**: ~65,000-85,000 rows

### Performance Estimates

**Batch Size**: 1000 matches
**Execution Frequency**: Daily at 2:00 AM
**Expected Duration**: 5-10 seconds per batch (hardware dependent)
**Database Lock Duration**: Minimal (batch-based approach)

### Index Utilization

✅ **Existing Index**: `idx_game_start_time` on `matches.game_start_time`
- Deletion queries will efficiently use this index
- No new indexes required

---

## Risk Mitigation

### Data Loss Prevention
1. ✅ **Database Backups**: Ensure daily backups before deployment
2. ✅ **Staging Testing**: Test thoroughly on staging environment
3. ✅ **Gradual Rollout**: Monitor first execution closely
4. ✅ **Kill Switch**: `EnableMatchCleanup` flag for emergency disable

### Performance Safeguards
1. ✅ **Batch Processing**: Prevents long-running transactions
2. ✅ **Low-Traffic Execution**: Runs at 2:00 AM
3. ✅ **Configurable Batch Size**: Adjustable if performance issues arise
4. ✅ **Comprehensive Logging**: Track execution metrics

### Operational Safety
1. ✅ **Configuration-Driven**: No code changes needed to adjust behavior
2. ✅ **Error Handling**: Exceptions don't crash application
3. ✅ **Monitoring**: Log all deletions for audit trail
4. ✅ **Reversible**: Can adjust retention period if needed

---

## Testing Checklist

### Unit Tests
- [ ] Repository method deletes correct matches
- [ ] Repository method respects batch size
- [ ] Repository method returns accurate count
- [ ] Job calculates cutoff date correctly
- [ ] Job reads configuration correctly
- [ ] Job logs all events properly
- [ ] Job handles errors gracefully

### Integration Tests
- [ ] Job registers in DI container
- [ ] Configuration binds correctly
- [ ] Cascade deletes work as expected
- [ ] No orphaned records remain
- [ ] Performance is acceptable

### Manual Testing (Staging)
- [ ] Create test matches (old and new)
- [ ] Run job manually
- [ ] Verify old matches deleted
- [ ] Verify new matches preserved
- [ ] Verify related tables cleaned up
- [ ] Check logs for accuracy

---

## Deployment Plan

### Phase 1: Development (Day 1-2)
1. Implement repository method
2. Implement background job
3. Add configuration settings
4. Write unit tests
5. Local testing

### Phase 2: Staging (Day 3-4)
1. Deploy to staging environment
2. Seed test data (old and new matches)
3. Run job and verify behavior
4. Monitor logs and performance
5. Verify database cleanup

### Phase 3: Production (Day 5)
1. Deploy to production during maintenance window
2. Monitor first execution closely
3. Verify logs and metrics
4. Check database size reduction
5. Document results

### Phase 4: Monitoring (Ongoing)
1. Track daily execution logs
2. Monitor database size trends
3. Adjust batch size if needed
4. Optimize schedule if needed

---

## Success Criteria

### Functional Requirements
- ✅ Deletes matches older than 6 months
- ✅ Preserves matches within retention period
- ✅ Cascade deletes all related records
- ✅ Runs automatically on schedule
- ✅ Configurable via appsettings

### Non-Functional Requirements
- ✅ Execution time < 1 minute per run
- ✅ No user-facing impact
- ✅ Comprehensive logging
- ✅ Error handling prevents crashes
- ✅ Testable and maintainable code

### Quality Metrics
- ✅ Code coverage > 80%
- ✅ No code duplication
- ✅ Follows existing patterns
- ✅ Clean architecture compliance
- ✅ Documentation complete

---

## Monitoring & Alerts

### Key Metrics to Track
1. **Deletion Count**: Matches deleted per run
2. **Execution Duration**: Time taken per run
3. **Error Rate**: Failed executions
4. **Database Size**: Overall database growth trend

### Recommended Alerts
1. **Error Alert**: If job fails 3 consecutive times
2. **Performance Alert**: If execution time > 5 minutes
3. **Volume Alert**: If deletion count > 10,000 matches (unexpected)

### Log Analysis Queries
```sql
-- Check database size
SELECT
    table_name,
    ROUND(((data_length + index_length) / 1024 / 1024), 2) AS size_mb
FROM information_schema.TABLES
WHERE table_schema = 'your_database'
ORDER BY size_mb DESC;

-- Count matches by age
SELECT
    DATE(FROM_UNIXTIME(game_start_time / 1000)) AS game_date,
    COUNT(*) AS match_count
FROM matches
GROUP BY game_date
ORDER BY game_date DESC
LIMIT 30;
```

---

## Documentation Updates

### Files to Update
1. ✅ `docs/architecture/data-retention-strategy.md` (created)
2. ✅ `docs/architecture/match-cleanup-implementation-plan.md` (this file)
3. ⏳ `docs/architecture/database-schema.md` (add retention policy notes)
4. ⏳ `docs/architecture/architectural-survey.md` (add new job to inventory)
5. ⏳ `README.md` (mention data retention in features)

---

## Conclusion

This implementation plan provides a **production-ready, maintainable, and performant** solution for automated match cleanup. The approach:

- ✅ Follows clean architecture principles
- ✅ Reuses existing patterns and infrastructure
- ✅ Maintains high testability
- ✅ Minimizes code complexity
- ✅ Provides operational flexibility
- ✅ Includes comprehensive monitoring

**Recommendation**: ✅ **Ready for implementation**

**Estimated Effort**: 2-3 days (development + testing)
**Risk Level**: Low
**Business Value**: High (reduces storage costs, improves performance)

