# Feature: Rank snapshots (LP for every ranked match)

> Follow-up to design-migration Phase 4b. Keeps the 4b contract unchanged (`participants.lp_after` / `tier_after` / `rank_after`, the API's `lpChange`, `lpAfter`, `tierAfter`, `rankAfter`, `LpChangeCalculator`, the SQL `LAG` in `MatchesRepository`). This feature only changes *how often and how safely* `lp_after` gets written.

## Problem Statement

Riot's match data (match-v5) has no LP. League-v4 only returns the player's **current** tier, division, LP, wins and losses per queue. Today the current rank is read in three places:

- at the end of every sync (`MatchHistorySyncJob.UpdateLpForMostRecentRankedMatchAsync`), which writes it onto the player's newest ranked match;
- at login (`LoginSyncService`) and when an account is linked (`RiotAccountsEndpoint`), which only update `riot_accounts.solo_*` / `flex_*`.

Sync runs only at login, on "Sync matches" and when an account is linked. Every match played between two syncs therefore shares one reading: only the newest gets `lp_after`, the others get nothing, and the match after them can't be compared. Real data (2026-09-28): 2 of the user's last 20 Flex matches have an LP change.

The current write is also unsafe: if League-v4 has already counted a new match that match-v5 doesn't list yet, the reading lands on the **previous** match, which then shows the wrong change (the guards in `LpChangeCalculator` catch only some of these).

## Proposed Solution

1. A new background job, **`RankSnapshotJob`**, reads League-v4 for every linked account of an active user every 20 minutes and stores each reading as a **rank snapshot** (tier, division, LP, wins, losses, time) per ranked queue.
2. When a queue's `wins + losses` rose by **exactly one** since the previous snapshot, exactly one ranked match in that queue ended between the two readings. The snapshot waits to be **attributed** to that match: it queues a normal sync for the account, and once the match is stored, its `lp_after` / `tier_after` / `rank_after` are written from the snapshot.
3. Attribution picks the match by **end time inside the snapshot window**, never "the newest match". With no match in the window yet (match-v5 lags League-v4), the snapshot stays pending and is retried; it is never written onto another match.
4. `UpdateLpForMostRecentRankedMatchAsync` is replaced: the end of a sync, a login and an account link each **take a snapshot** through the same service and run the same attribution. There is one code path that writes `lp_after`.

With a snapshot every 20 minutes, nearly every ranked match falls alone between two readings, so the consecutive matches 4b compares both have LP.

## User Stories

### Primary User Story
As a ranked player, I want every ranked match on the Matches page to show how much LP it won or lost, so I can see which matches cost me and how my rank moved.

### Additional User Stories
- As a player, I want my new ranked matches to appear without pressing "Sync matches", because the rank job notices that I played.
- As a player, I never want to see an LP change on the wrong match; no number is better than a wrong one.

## Requirements

### Functional Requirements

**Polling**
1. `RankSnapshotJob` (a `BackgroundService`, like `MatchHistorySyncJob`) runs when `Jobs:EnableRankSnapshots` is true.
2. It polls **active accounts**: Riot accounts linked (`user_riot_accounts`) to a user with `is_active = TRUE` and `last_login_at` within `Jobs:RankSnapshotActiveDays` (default 7). Every linked account of such a user is polled, on the free tier too. Accounts of inactive users are not polled; their next login takes a snapshot anyway (FR 13).
3. Each active account is read at most every `Jobs:RankSnapshotIntervalMinutes` (default 20). The job picks accounts in order of their oldest latest snapshot (round-robin), so no account starves when the budget is short.
4. One League-v4 call per account per read (`GetLeagueEntriesByPuuidAsync`), stored as one snapshot row per ranked queue present in the answer (`RANKED_SOLO_5x5` → 420, `RANKED_FLEX_SR` → 440). A queue missing from the answer (unranked) stores nothing.
5. The job spends at most `Jobs:RankSnapshotMaxCallsPerMinute` Riot calls (default 10). It goes through the shared `RiotLimitHandler`, so it never exceeds the key's limits, and the cap keeps room for user-triggered syncs.
6. After each read the account's `riot_accounts.solo_tier/solo_rank/solo_lp` and `flex_*` are updated too, so the Overview player line stays current.

**Detecting a match**
7. For each queue, the new snapshot is compared with the previous snapshot of the same account and queue:
   - `games = wins + losses` unchanged → **no match**: the row is kept only if tier, division or LP changed (decay, a dodge), otherwise the previous row's `captured_at` is not touched and no row is added (keeps the table small).
   - `games` rose by exactly 1 → **pending**: exactly one ranked match in that queue ended between the two `captured_at` times.
   - `games` rose by more than 1, or fell (season reset) → **baseline**: no match can be attributed; the snapshot is only a new starting point.
   - No previous snapshot → **baseline**.
8. A new pending snapshot always (whether or not the user has the app open) sets the account's `sync_status` to `pending` and wakes `MatchHistorySyncJob` through the existing queue signal (skip when it is already `pending` or `syncing`).

**Attribution** (a domain rule in Core, orchestration in Application)
9. A pending snapshot for queue Q with window `(previous.captured_at, snapshot.captured_at]` is attributed to the account's stored match in queue Q whose **end time** (`game_start_time + game_duration_sec`) falls in the window, widened by `Jobs:RankSnapshotClockSkewSeconds` (default 120) on both sides for clock differences.
   - Exactly one candidate → write the snapshot's tier, division and LP onto that participant row (`lp_after`, `tier_after`, `rank_after`) and mark the snapshot `attributed` with the `match_id`.
   - No candidate → stay `pending` (the match isn't synced yet, or match-v5 doesn't list it yet).
   - More than one candidate → mark `skipped` (can't tell which; never guess).
   - A participant row that already has `lp_after` from an earlier attributed snapshot is not overwritten.
10. Attribution runs at the end of every sync of the account and on every job tick for pending snapshots whose match may have arrived.
11. A snapshot pending for longer than `Jobs:RankSnapshotPendingTimeoutMinutes` (default 60) becomes `skipped` (a remake, or a match that will never be stored).
12. Remakes: match-v5 matches under 5 minutes are not stored (`GameConstants.MinValidGameDurationSec`) and don't change wins or losses, so they neither trigger nor receive attribution.

**Other readings**
13. Login (`LoginSyncService`) and the end of a sync already call League-v4; they store their reading as a snapshot through the same service (`source` = `login` / `sync`). `UpdateLpForMostRecentRankedMatchAsync` is removed. Linking needs no hook: the sync that follows a link stores the first snapshot.

**Retention**
14. Snapshots older than `Jobs:RankSnapshotRetentionDays` (default 30) are deleted once a day by `RankSnapshotJob` itself, except the newest snapshot per account and queue (the next comparison needs it). Attribution results live on `participants`, so nothing on the Matches page depends on old snapshots.

### Non-Functional Requirements
- **Performance**: the per-tick query for due accounts uses an index on `(puuid, queue_id, captured_at)`; attribution is one indexed query per pending snapshot. No change to the Matches endpoints.
- **Rate limits**: with the development key (the app's buckets: 10 per second, 50 per 2 minutes) and the default cap of 10 calls per minute, about 200 active accounts get a 20-minute interval; with more, intervals stretch by round-robin (FR 3). A production key (500 per 10 seconds, 30,000 per 10 minutes) supports thousands; raise `RankSnapshotMaxCallsPerMinute` in production config only. A 429 from Riot pauses the job for the `Retry-After` time.
- **Security**: no new endpoints; PUUIDs never come from clients. Log PUUIDs only through `LogSanitizer.HashForLog`, other values through `LogSanitizer.Sanitize`.
- **Correctness over coverage**: every uncertain case leaves `lp_after` empty rather than guessing (FR 7, 9, 11).
- **Time**: `captured_at` is UTC (`DATETIME`, written with `DateTime.UtcNow`); comparisons with `game_start_time` (epoch ms) convert explicitly.

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Core rule: `server/Mongoose.Api/Core/Services/RankSnapshotRules.cs`: `Classify(previous, current)` → `NoMatch | Pending | Baseline` (FR 7) and `ChooseMatch(window, candidates)` → the single match or none / ambiguous (FR 9). Pure, unit tested.
- [ ] Core entity + interface: `Core/Entities/RankSnapshot.cs` (the table row; the 4b value object `RankSnapshot` in `Core/ValueObjects` is renamed only if the names clash, e.g. to `RankReading`), `Core/Interfaces/IRankSnapshotsRepository.cs`.
- [ ] Application service: `Application/Services/RankSnapshotService.cs`: `CaptureAsync(account, source, ct)` (one League-v4 call → snapshots → riot_accounts rank → queue a sync when pending) and `AttributePendingAsync(puuid, ct)`. Used by the job, the sync job, `LoginSyncService` and `RiotAccountsEndpoint`.
- [ ] Infrastructure: `Infrastructure/Database/Repositories/RankSnapshotsRepository.cs` (parameterized SQL), `Infrastructure/Jobs/RankSnapshotJob.cs`, registration in `Program.cs` behind `Jobs:EnableRankSnapshots`.
- [ ] `MatchHistorySyncJob`: replace `UpdateLpForMostRecentRankedMatchAsync` with `RankSnapshotService.CaptureAsync(..., source: sync)` + `AttributePendingAsync`.
- [ ] `ParticipantsRepository.UpdateLpDataAsync`: add `AND lp_after IS NULL` so an attributed reading is never overwritten.
- [ ] `MatchCleanupJob`: snapshot retention (FR 14).
- [ ] `IRiotApiClient`: unchanged (League-v4 by PUUID already exists).

### Frontend Changes
None. The Matches page (4b) shows the extra LP changes as they appear. Out of scope: the header total's threshold (revisit once coverage is measured).

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] New table `rank_snapshots`:

```sql
CREATE TABLE rank_snapshots (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    puuid VARCHAR(78) NOT NULL,
    queue_id INT NOT NULL,                       -- 420 or 440
    tier VARCHAR(20) NOT NULL,
    division VARCHAR(10) NULL,                   -- NULL for Master and above
    lp INT NOT NULL,
    wins INT NOT NULL,
    losses INT NOT NULL,
    captured_at DATETIME(3) NOT NULL,            -- UTC
    window_start_at DATETIME(3) NULL,            -- pending only: the previous reading's time
    source ENUM('poll', 'sync', 'login') NOT NULL,
    status ENUM('baseline', 'pending', 'attributed', 'skipped', 'no_match') NOT NULL,
    match_id VARCHAR(50) NULL,                   -- set when attributed
    KEY idx_rank_snapshots_account_queue_time (puuid, queue_id, captured_at),
    KEY idx_rank_snapshots_status (status, captured_at),
    CONSTRAINT fk_rank_snapshots_account FOREIGN KEY (puuid) REFERENCES riot_accounts(puuid) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

- [ ] Modified tables: `riot_accounts.rank_checked_at DATETIME(3) NULL` (last rank read; the job polls by it, so accounts whose rank didn't change, or unranked ones, aren't re-read every tick). `participants.lp_after` / `tier_after` / `rank_after` already exist.
- [ ] Migration: `server/Mongoose.Api/Infrastructure/Database/Migrations/003_AddRankSnapshots.sql`, `schema.sql`, and `database-schema.spec.md` (new table + the note on `lp_after` from 4b updated: written by attribution, not "newest ranked match").
- [ ] Data migration: none. Existing `lp_after` values stay; each account's first snapshot is a baseline.

### Configuration (`Jobs:*`)

| Key | Default | Meaning |
|---|---|---|
| `EnableRankSnapshots` | `true` (`false` in E2E) | Run `RankSnapshotJob` |
| `RankSnapshotIntervalMinutes` | 20 | Minimum time between reads of one account |
| `RankSnapshotActiveDays` | 7 | Poll accounts of users who logged in within this many days |
| `RankSnapshotMaxCallsPerMinute` | 10 | League-v4 calls the job may spend per minute |
| `RankSnapshotClockSkewSeconds` | 120 | Widening of the attribution window on both sides |
| `RankSnapshotPendingTimeoutMinutes` | 60 | Pending snapshots older than this become `skipped` |
| `RankSnapshotRetentionDays` | 30 | Snapshot retention (newest per account and queue always kept) |

### API Contracts
No new or changed endpoints. `GET /api/v2/matches/{userId}` and `GET /api/v2/matches/{matchId}/details` keep the 4b fields; more of them are non-null.

## UI/UX Requirements

No UI changes. One behaviour users will notice: new ranked matches appear on the Matches page and Overview without pressing "Sync matches", within about one interval plus sync time after the match ends. SyncProgress already shows syncs started from anywhere.

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend)
- [ ] `RankSnapshotRulesTests`: games +0 (with and without an LP change), +1, +2, −n (season reset), no previous snapshot; `ChooseMatch` with zero, one and two candidates, end time exactly on the window edges, clock-skew widening.
- [ ] `RankSnapshotServiceTests` (fake repos + fake Riot client): a read stores one row per ranked queue and none for unranked; updates `riot_accounts` ranks; queues a sync only for a pending snapshot and only when the account isn't already pending or syncing; attribution writes onto the one candidate, leaves pending with none, skips with two, never overwrites an existing `lp_after`, times out after the configured minutes.
- [ ] `RankSnapshotJobTests`: picks due accounts oldest first, respects the per-minute cap, skips inactive users and users outside `RankSnapshotActiveDays`, backs off on 429, does nothing when disabled.
- [ ] `MatchHistorySyncJobTests`: the end of a sync captures a snapshot and runs attribution; `UpdateLpForMostRecentRankedMatchAsync` is gone.
- [ ] `MatchCleanupJobTests`: deletes old snapshots but keeps the newest per account and queue.

### Integration Tests
- [ ] `RankSnapshotsRepositoryIntegrationTests` (opt-in `RUN_DB_INTEGRATION_TESTS` + `Database_test`, like `MatchesRepositoryIntegrationTests`): insert and read snapshots, the due-accounts query, the candidate query by end time and queue, retention.
- [ ] End to end on the 4b query: two matches attributed from consecutive pending snapshots → `GetMatchListSummaryAsync` returns their `lpChange`.
- [ ] `LoginSyncService` and `RiotAccountsEndpoint` tests: they store a snapshot through the service instead of writing ranks themselves.

### Manual Testing Scenarios
1. With the job running, play (or wait for) one ranked match: within one interval a pending snapshot appears, a sync runs, and the match shows its LP change on the Matches page without pressing "Sync matches".
2. Two matches played back to back inside one interval: the snapshot is a baseline, both matches show no change, the next single match shows its change again.
3. Force the race: take a snapshot right after the match ends (before match-v5 lists it): the snapshot stays pending, then attributes once the match is stored; the previous match's `lp_after` is untouched.
4. Measure coverage after a week on the dev database: share of ranked matches with a non-null `lpChange` (target: 80% or more for players who play one match at a time).

## Validation Criteria
Feature is considered complete when:
- [x] All functional requirements are implemented behind `Jobs:EnableRankSnapshots`.
- [x] Unit and integration tests pass; E2E unaffected (job disabled in E2E mode).
- [x] `database-schema.spec.md`, `architecture.spec.md` (jobs section) and `design-migration.plan.md` are updated.
- [ ] Code review and a security review of the new job are done.
- [ ] Riot call volume stays under the configured cap (log the calls per minute at Debug).
- [ ] No match gets an `lp_after` from a reading that belongs to a different match (manual scenario 3).

## Dependencies
### Internal Dependencies
- [x] Phase 4b (`LpChangeCalculator`, the `LAG` query, the API fields): done, commit `b4d72f2`.
- [x] `MatchHistorySyncJob` queue signal (`_queueSignal`) and `sync_status` handling.
- [x] `RiotLimitHandler` shared buckets.

### External Dependencies
- [x] Riot League-v4 `entries/by-puuid` (tier, rank, leaguePoints, wins, losses per queue).
- [ ] A production Riot API key before the number of active accounts grows past what the development key allows.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| League-v4 counts a match before match-v5 lists it | High (wrong LP on a match) | High | Attribute by end-time window, never "newest"; stay pending until the match exists; time out to skipped |
| Two matches inside one interval | Low (no LP for them) | Medium | Baseline, no guess; a 20-minute interval makes it rare: a ranked match plus queue and champion select takes 20+ minutes, so two matches only end in one window after a very early surrender |
| A dodge or apex decay between two matches | Low (change includes the dodge penalty or decay) | Low | 4b compares consecutive `lp_after`; the ±100 and sign guards in `LpChangeCalculator` catch large cases. Documented as a known limit |
| Riot rate limits with many active users | Medium (slower snapshots, slower user syncs) | Medium with the dev key | Per-minute cap, round-robin, 429 back-off, production key |
| Auto-syncs increase match-v5 calls | Medium | Medium | Only on a pending snapshot (one per match played); same cost as the user pressing "Sync matches" once per match |
| Clock differences between Riot and the server | Medium (a match falls outside its window) | Low | `RankSnapshotClockSkewSeconds` widening; ambiguity leads to `skipped`, not a guess |
| Table growth | Low | Medium | Rows only on a change (FR 7), 30-day retention |

## Timeline and Milestones
- [x] **Phase 1**: Spec review, open questions answered
- [x] **Phase 2**: Schema, repository, Core rules and service with tests
- [x] **Phase 3**: `RankSnapshotJob`, sync-job and login/link wiring, retention
- [ ] **Phase 4**: Run on the dev database for a week; measure coverage (manual scenario 4). Started 2026-09-29 (migration applied), ends 2026-10-06.
- [ ] **Phase 5**: Docs, production key and config

## Decisions (2026-09-28)
- Every linked account of an active user is polled, free tier included.
- Active means a login within the last 7 days; the interval is 20 minutes.
- A detected match always queues a sync, whether or not the user has the app open.

## Open Questions
- [ ] Should the Matches page header total wait for this feature to be measured before its threshold is revisited (4b note)?

## Handoff Checklist
Before implementation begins:
- [x] Open questions answered (see Decisions)
- [x] Schema and migration reviewed
- [ ] Rate-limit budget agreed for dev and production keys
- [ ] Security considerations addressed (no new endpoints; logging rules)
- [x] Implementation ready for assignment (backend only)

## References
- `server/Mongoose.Api/Infrastructure/Jobs/MatchHistorySyncJob.cs` (`UpdateLpForMostRecentRankedMatchAsync`, queue signal)
- `server/Mongoose.Api/Core/Services/LpChangeCalculator.cs`, `MatchesRepository.PreviousRankSql` (Phase 4b)
- `server/Mongoose.Api/Infrastructure/Riot/LimitHandler/RiotLimitHandler.cs`
- `.github/specs/design-migration.plan.md` Phase 4b outcome notes
- Riot API: League-v4 `/lol/league/v4/entries/by-puuid/{puuid}`, Match-v5 `/lol/match/v5/matches/by-puuid/{puuid}/ids`
