---
type: "manual"
date: "2026-02-06"
status: "proposal"
---

# Data Retention Strategy - Match Cleanup

## Executive Summary

This document outlines the architectural approach for implementing automated deletion of match data older than 6 months. The strategy balances database performance, storage costs, and user experience while maintaining clean architecture principles.

## Current State Analysis

### Data Volume Characteristics
- **Match Sync Window**: Currently syncs 6 months of history on initial backfill (`BackfillLookbackPeriod = 180 days`)
- **Database Tables Affected**:
  - `matches` (core match metadata)
  - `participants` (10 rows per match)
  - `participant_checkpoints` (~30-50 rows per match for timeline data)
  - `participant_metrics` (10 rows per match)
  - `team_objectives` (2 rows per match)
  - `participant_objectives` (10 rows per match)
  - `team_match_metrics` (2 rows per match)
  - `team_role_responsibilities` (10 rows per match)
  - `duo_metrics` (variable, 0-45 rows per match)

### Cascade Delete Configuration
✅ **All child tables already use `ON DELETE CASCADE`** (verified in schema.sql)
- Deleting from `matches` table automatically cascades to all related tables
- No orphaned records will remain

### Current Retention Behavior
- ❌ **No automated cleanup** - data accumulates indefinitely
- ⚠️ **Potential Issues**:
  - Database bloat over time
  - Slower query performance on large datasets
  - Increased storage costs
  - Stale data that users no longer care about

---

## Proposed Solution

### Architecture: Background Cleanup Job

**Pattern**: Follow existing `MatchHistorySyncJob` pattern using `IHostedService`

**Key Design Decisions**:
1. ✅ **Separate Background Service** - Independent, testable, configurable
2. ✅ **Configurable Retention Period** - Default 6 months, adjustable via appsettings
3. ✅ **Batch Processing** - Delete in chunks to avoid long-running transactions
4. ✅ **Scheduled Execution** - Run daily during low-traffic hours
5. ✅ **Comprehensive Logging** - Track deletions for audit and monitoring
6. ✅ **Graceful Degradation** - Errors don't crash the application

### Implementation Components

#### 1. New Background Job: `MatchCleanupJob`
**Location**: `server/Infrastructure/Jobs/MatchCleanupJob.cs`

**Responsibilities**:
- Run on a daily schedule (configurable interval)
- Identify matches older than retention period
- Delete in batches to avoid locking issues
- Log deletion metrics (count, duration, errors)

**Configuration**:
```json
"Jobs": {
  "EnableMatchHistorySync": true,
  "EnableMatchCleanup": true,
  "MatchCleanupSchedule": "02:00",  // 2 AM daily
  "MatchRetentionDays": 180,         // 6 months
  "MatchCleanupBatchSize": 1000      // Delete 1000 matches per batch
}
```

#### 2. Repository Method: `IMatchesRepository.DeleteOldMatchesAsync`
**Location**: `server/Infrastructure/Database/Repositories/MatchesRepository.cs`

**Signature**:
```csharp
Task<int> DeleteOldMatchesAsync(DateTime cutoffDate, int batchSize);
```

**Implementation Strategy**:
- Use `game_start_time` (BIGINT milliseconds) for cutoff calculation
- Delete in batches to avoid long transactions
- Return count of deleted matches for logging
- Leverage CASCADE DELETE for automatic cleanup of related tables

#### 3. Configuration Interface
**Location**: `server/appsettings.json` and `server/web.config`

Add new configuration section for cleanup job settings.

---

## Data Retention Policy

### Retention Period: 6 Months (180 Days)
**Rationale**:
- Aligns with initial sync window (consistency)
- Covers 2-3 competitive seasons (sufficient for trend analysis)
- Balances storage costs with user value
- Matches industry standards for gaming analytics

### Cutoff Calculation
```csharp
var cutoffTimestamp = DateTimeOffset.UtcNow
    .AddDays(-retentionDays)
    .ToUnixTimeMilliseconds();
```

### Exclusions (Future Consideration)
- **Option**: Preserve matches for active users (last login < 30 days)
- **Option**: Preserve "milestone" matches (first game, highest KDA, etc.)
- **Current Proposal**: Simple time-based deletion (KISS principle)

---

## Performance Considerations

### Batch Size Optimization
- **Recommended**: 1000 matches per batch
- **Reasoning**: 
  - ~10,000 related rows per batch (participants, metrics, etc.)
  - Keeps transaction size manageable
  - Allows progress tracking
  - Prevents lock escalation

### Execution Schedule
- **Recommended**: Daily at 2:00 AM (low-traffic period)
- **Alternative**: Weekly on Sunday nights
- **Configurable**: Via `MatchCleanupSchedule` setting

### Index Impact
- ✅ **Existing Index**: `idx_game_start_time` on `matches.game_start_time`
- Deletion queries will be efficient using this index

### Database Load
- **Estimated Duration**: 5-10 seconds per 1000 matches (depends on hardware)
- **Lock Duration**: Minimal (batch-based approach)
- **Impact**: Negligible during low-traffic hours

---

## Testing Strategy

### Unit Tests
**Location**: `server/Mongoose.Api.Tests/MatchCleanupJobTests.cs`

**Test Cases**:
1. ✅ Delete matches older than cutoff date
2. ✅ Preserve matches within retention period
3. ✅ Verify cascade deletion of related tables
4. ✅ Handle empty database gracefully
5. ✅ Respect batch size limits
6. ✅ Log deletion metrics correctly

### Integration Tests
1. ✅ Verify job registration in DI container
2. ✅ Verify configuration binding
3. ✅ Verify scheduled execution (mocked timer)

---

## Monitoring & Observability

### Logging Events
- **Startup**: "MatchCleanupJob starting..."
- **Execution Start**: "Starting match cleanup (cutoff: {date}, batch size: {size})"
- **Batch Completion**: "Deleted {count} matches in batch {batchNum}"
- **Completion**: "Match cleanup completed: {totalDeleted} matches deleted in {duration}ms"
- **Errors**: "Error during match cleanup: {exception}"

### Metrics to Track
- Total matches deleted per run
- Execution duration
- Batch count
- Error count
- Database size reduction (optional)

---

## Migration Path

### Phase 1: Implementation (Week 1)
1. ✅ Create `MatchCleanupJob` background service
2. ✅ Add `DeleteOldMatchesAsync` to `IMatchesRepository`
3. ✅ Implement repository method with batch logic
4. ✅ Add configuration settings
5. ✅ Register job in `Program.cs`

### Phase 2: Testing (Week 1)
1. ✅ Write unit tests for repository method
2. ✅ Write unit tests for cleanup job
3. ✅ Test on development database with sample data
4. ✅ Verify cascade deletions work correctly

### Phase 3: Deployment (Week 2)
1. ✅ Deploy to staging environment
2. ✅ Monitor first execution
3. ✅ Verify database size reduction
4. ✅ Deploy to production with monitoring

### Phase 4: Optimization (Ongoing)
1. ⚠️ Monitor execution times
2. ⚠️ Adjust batch size if needed
3. ⚠️ Consider partitioning strategy for very large datasets

---

## Risk Assessment

### Low Risk ✅
- **Cascade Deletes**: Already configured in schema
- **Batch Processing**: Prevents long-running transactions
- **Configurable**: Can disable via appsettings
- **Reversible**: Can adjust retention period to recover data (if backups exist)

### Medium Risk ⚠️
- **Data Loss**: Permanent deletion (mitigated by 6-month retention)
- **Performance Impact**: Minimal during low-traffic hours
- **User Confusion**: Users may wonder where old matches went (mitigated by documentation)

### Mitigation Strategies
1. ✅ **Database Backups**: Ensure daily backups before first deployment
2. ✅ **Gradual Rollout**: Test on staging first
3. ✅ **Monitoring**: Track deletion metrics closely
4. ✅ **Kill Switch**: `EnableMatchCleanup` flag for emergency disable

---

## Alternative Approaches Considered

### ❌ Soft Delete (Rejected)
**Approach**: Add `deleted_at` column, filter in queries
**Rejection Reason**:
- Adds complexity to all queries
- Doesn't reduce database size
- Violates YAGNI principle

### ❌ Archive Table (Rejected)
**Approach**: Move old matches to `matches_archive` table
**Rejection Reason**:
- Adds schema complexity
- Users don't need historical data beyond 6 months
- Increases maintenance burden

### ❌ Manual Cleanup Script (Rejected)
**Approach**: Run SQL script manually when needed
**Rejection Reason**:
- Requires manual intervention
- Error-prone
- Not scalable

### ✅ Background Job (Selected)
**Rationale**:
- Automated and reliable
- Follows existing patterns (`MatchHistorySyncJob`)
- Configurable and testable
- Minimal complexity

---

## Code Quality Assessment

### Reusability: A
- Follows existing `IHostedService` pattern
- Reuses `RepositoryBase` infrastructure
- Configuration-driven behavior

### Testability: A
- Repository method independently testable
- Job logic testable with mocked dependencies
- Integration tests verify end-to-end flow

### Lines of Code: A
- **Estimated LOC**:
  - `MatchCleanupJob.cs`: ~150 lines
  - Repository method: ~30 lines
  - Tests: ~200 lines
  - Configuration: ~10 lines
- **Total**: ~390 lines (minimal, focused)

### Clean Code Principles: A
- Single Responsibility: Job handles scheduling, repository handles deletion
- Open/Closed: Configurable without code changes
- Dependency Inversion: Uses interfaces (`IMatchesRepository`)
- KISS: Simple time-based deletion, no complex logic

### Separation of Concerns: A
- **Infrastructure Layer**: Background job implementation
- **Core Layer**: Repository interface
- **Application Layer**: Configuration
- Clear boundaries, no cross-layer violations

---

## Next Steps

### Immediate Actions
1. ✅ Review this proposal with team
2. ✅ Approve retention period (6 months)
3. ✅ Approve execution schedule (daily 2 AM)
4. ✅ Begin implementation

### Implementation Order
1. Create repository method with tests
2. Create background job with tests
3. Add configuration settings
4. Register job in DI container
5. Deploy to staging
6. Monitor and deploy to production

### Documentation Updates
- ✅ This document (data-retention-strategy.md)
- ⏳ Update database-schema.md with retention policy notes
- ⏳ Update architectural-survey.md with new job

---

## Conclusion

The proposed match cleanup strategy provides a **clean, maintainable, and performant** solution for managing database growth. By following existing architectural patterns and leveraging cascade deletes, we achieve data retention with minimal code complexity.

**Recommendation**: ✅ **Proceed with implementation**

The solution aligns with clean architecture principles, maintains testability, and provides the operational flexibility needed for a production system.

