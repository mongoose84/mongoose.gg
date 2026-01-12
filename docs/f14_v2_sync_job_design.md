# F14: V2 Match History Sync Job Design

## Overview

This document describes the design for the V2 Match History Sync Job, which handles concurrent match synchronization for linked Riot accounts with proper locking and rate limiting.

## Concurrency Analysis

### 1. Riot API Rate Limiting ✅ Already Handled

The `RiotApiClient` uses a token bucket rate limiter (`TokenBucket` with `SemaphoreSlim`). Since it's registered as a **singleton**, all sync operations share the same rate limiter - multiple concurrent syncs automatically queue through it.

### 2. Database Concurrency ✅ Safe by Design

- Each repository operation creates a new connection (no long-lived transactions)
- MySQL/MariaDB handles concurrent writes via row-level locking (InnoDB)
- `UPSERT` operations (`INSERT ... ON DUPLICATE KEY UPDATE`) are atomic
- If two syncs try to insert the same match, one succeeds and one updates (idempotent)

### 3. Job-Level Coordination - Per-Account Locking

| Concern | Solution |
|---------|----------|
| Multiple users triggering sync | Run concurrently - rate limiter handles API, DB upserts are idempotent |
| Same user re-triggering sync | Check `sync_status != 'syncing'` before claiming; reject with 409 if running |
| Atomic claim | Use `UPDATE ... WHERE sync_status = 'pending'` and check affected rows |
| Crash recovery | On startup, reset any `syncing` status older than 10 min back to `pending` |
| Progress tracking | Update `sync_progress` and `sync_total` columns after each match |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                TRIGGERS                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  Account Linked ──┐                                                          │
│  Login (stale) ───┼──► Set sync_status = 'pending' ──► Background Job Loop  │
│  Manual /sync ────┘                                                          │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                          BACKGROUND JOB LOOP                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  1. Poll for pending accounts                                                │
│  2. If none found → Sleep 10s → Go to 1                                      │
│  3. Claim account atomically: pending → syncing                              │
│  4. Sync matches for account                                                 │
│     - Fetch match IDs from Riot (rate limited)                               │
│     - For each new match:                                                    │
│       - Fetch match + timeline (rate limited)                                │
│       - Upsert to v2 tables (idempotent)                                     │
│       - Broadcast progress via WebSocket                                     │
│  5. On success: syncing → completed                                          │
│  6. On failure: syncing → failed                                             │
│  7. Go to 1                                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Class Structure

### V2MatchHistorySyncJob (BackgroundService)

**Location:** `server/Infrastructure/External/V2MatchHistorySyncJob.cs`

**Dependencies:**
- `IServiceProvider` - for creating scopes
- `ILogger<V2MatchHistorySyncJob>` - for logging

**Key Methods:**
- `ExecuteAsync()` - Main loop: recover stuck jobs, then poll for pending accounts
- `RecoverStuckJobsAsync()` - Reset 'syncing' accounts older than 10 min to 'pending'
- `TryProcessNextPendingAccountAsync()` - Claim and process one pending account
- `SyncAccountMatchesAsync()` - Fetch and persist matches for an account
- `FetchNewMatchIdsAsync()` - Paginate through Riot API for new match IDs
- `PersistMatchDataAsync()` - Reuse mapping logic from existing MatchHistorySyncJob

## Required Repository Methods

Add to `V2RiotAccountsRepository`:

### ClaimNextPendingForSyncAsync

Atomically claims the next pending account using a transaction:
1. SELECT one pending account (ORDER BY updated_at ASC LIMIT 1)
2. UPDATE to 'syncing' WHERE sync_status = 'pending' (prevents races)
3. Return the claimed account or null if race lost

### ResetStuckSyncingAccountsAsync

Reset accounts stuck in 'syncing' state for crash recovery:
```sql
UPDATE riot_accounts 
SET sync_status = 'pending', updated_at = NOW() 
WHERE sync_status = 'syncing' AND updated_at < @cutoff
```

## Optional: Progress Columns

Add to `riot_accounts` table for fine-grained progress tracking:

```sql
ALTER TABLE riot_accounts 
    ADD COLUMN sync_progress INT DEFAULT 0 AFTER sync_status,
    ADD COLUMN sync_total INT DEFAULT 0 AFTER sync_progress;
```

## Registration

Add to `Program.cs`:

```csharp
// Register V2 sync job (after existing MatchHistorySyncJob)
if (builder.Configuration.GetValue<bool>("Jobs:EnableMatchHistorySync", true))
{
    builder.Services.AddHostedService<V2MatchHistorySyncJob>();
}
```

## Summary

| Component | Responsibility |
|-----------|----------------|
| `V2MatchHistorySyncJob` | BackgroundService that polls for pending accounts and processes them |
| `ClaimNextPendingForSyncAsync` | Atomic claim with transaction to prevent races |
| `IRiotApiClient` | Singleton with rate limiter - serializes all API calls automatically |
| `Upsert` methods | Idempotent DB operations - safe for concurrent writes |
| `IWebSocketBroadcaster` | Optional progress notifications to connected clients |

**This design allows:**
- ✅ Multiple accounts syncing concurrently (different users)
- ✅ Same account can't be synced twice simultaneously
- ✅ Crash recovery on restart
- ✅ Partial progress saved on failure
- ✅ Real-time progress via WebSocket

