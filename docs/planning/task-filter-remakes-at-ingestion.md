# Task: Filter Remake/Abandoned Games at Ingestion

**Priority:** P0 - Critical  
**Type:** Bug Fix  
**Estimate:** 2 points  
**Labels:** `data-quality`, `ingestion`, `backend`

---

## Problem

The match ingestion pipeline stores every game the Riot API returns, including remakes and abandoned games (typically under 5 minutes). These records contaminate all analytics — winrates, KDA averages, trend charts, role baselines, etc. — because downstream queries have no consistent way to exclude them.

---

## Solution

Reject remake and abandoned games in `MatchDataPersistenceService.PersistMatchDataAsync` before any data is written to the database. This is the cleanest fix: bad data never enters the system.

### Detection criteria (use both):
1. `info.gameDuration < 300` — games under 5 minutes cannot be legitimate competitive games.
2. Any participant has `gameEndedInEarlySurrender: true` — Riot's explicit remake flag (available on the `participants[]` array in match-v5 info).

If either condition is true, log at `Debug` level and return early without persisting anything.

---

## Acceptance Criteria

- [ ] `PersistMatchDataAsync` checks `gameDuration < 300` from the match info JSON before calling `_matchesRepo.UpsertAsync`.
- [ ] `PersistMatchDataAsync` also checks that no participant has `gameEndedInEarlySurrender: true`.
- [ ] If either condition is true, the method returns early and nothing is written to any table.
- [ ] A `_logger.LogDebug` call records the skipped match ID and the reason (`short_duration` or `early_surrender`).
- [ ] Existing matches already in the database are **not** automatically cleaned up (that is a separate task).
- [ ] A unit test covers the early-return path for each condition (short duration, early surrender flag).
- [ ] A unit test confirms a normal game (≥ 300 s, no early surrender) is still persisted.

---

## Files to Change

- `server/Mongoose.Api/Infrastructure/Services/MatchDataPersistenceService.cs` — add guard at top of `PersistMatchDataAsync`
- `server/Mongoose.Api.Tests/` — add or extend `MatchDataPersistenceServiceTests.cs`

---

## Notes

- The check must use the `info.gameDuration` field (seconds integer), not `gameStartTimestamp`.
- `gameEndedInEarlySurrender` is per-participant; checking `any participant == true` is sufficient.
- Do not filter based on `gameDuration` alone at values higher than 300 — short but real games (e.g., fast ARAM wins ~15 min) must not be excluded.
- This fix prevents future contamination. The companion task handles existing dirty data in queries.
