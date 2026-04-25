# Task: Exclude Short Games from All Analytics Queries

**Priority:** P0 - Critical  
**Type:** Bug Fix  
**Estimate:** 3 points  
**Labels:** `data-quality`, `analytics`, `backend`  
**Depends on:** `task-filter-remakes-at-ingestion.md` (but can ship independently)

---

## Problem

Analytics queries do not consistently exclude short/remake games from aggregations. The ingestion-layer fix prevents future bad data, but games already stored (and any edge cases that slip through) will still contaminate:

- Winrate trend (rolling average pulled down by `win: false` remakes)
- Deaths trend (0-death remakes make trend look artificially improving)
- Dragon participation trend (0 dragons skews participation rate down)
- Solo performance overall stats (KDA, win rate, avg duration)
- Solo performance champion pool and role breakdown
- Solo performance "performance by phase" (2-min remakes land in the ≤ 15 min bucket)
- Radar chart (all 6 axes averaged over polluted set)
- Role baselines used for match list trend badges
- Overview primary queue detection, last-20 W/L strip, last match
- Matchup repository stats

One query (`CsPerMinuteTrend`) already filters `game_duration_sec >= 900` — but 900 s is too aggressive (excludes short real games) and the threshold is inconsistent with everywhere else.

---

## Solution

Add a shared minimum-duration constant to `RepositoryBase` (or a dedicated `QueryConstants` static class) and apply it to every analytics `WHERE` clause as a standard guard — the same way `queueFilter` and `timeFilter` are already injected.

```csharp
// Proposed location: RepositoryBase or QueryConstants
public const int MinValidGameDurationSec = 300; // 5 minutes — excludes all remakes
```

Every repository query that joins `matches` for analytics purposes must add:
```sql
AND m.game_duration_sec >= 300
```

The existing `CsPerMinuteTrend` threshold of 900 s must be **lowered to 300 s** to align with the standard.

---

## Acceptance Criteria

- [ ] A constant `MinValidGameDurationSec = 300` is defined in one shared location (e.g., `RepositoryBase` or a new `QueryConstants.cs` in Infrastructure).
- [ ] `TrendRepository.GetWinrateTrendAsync` — filter applied.
- [ ] `TrendRepository.GetDeathsTrendAsync` — filter applied.
- [ ] `TrendRepository.GetDragonParticipationTrendAsync` — filter applied.
- [ ] `TrendRepository.GetCsPerMinuteTrendAsync` — existing `>= 900` threshold replaced with the shared constant (`>= 300`).
- [ ] `TrendRepository.GetGoldAt15TrendAsync` — already implicitly safe via `INNER JOIN participant_checkpoints … minute_mark = 15`; add explicit filter anyway for clarity.
- [ ] `SoloPerformanceRepository` — all sub-queries (`GetOverallStatsAsync`, `GetSideStatsAsync`, `GetChampionStatsAsync`, `GetMainChampionsByRoleAsync`, `GetRoleBreakdownAsync`, `GetDeathEfficiencyAsync`, `GetMatchDurationsAsync`, `GetRecentTrendAsync`) — filter applied.
- [ ] `OverviewStatsRepository.GetPrimaryQueueAsync` — filter applied.
- [ ] `OverviewStatsRepository.GetLast20MatchesAsync` — filter applied.
- [ ] `OverviewStatsRepository.GetLastMatchAsync` — filter applied.
- [ ] `RadarChartRepository.GetRadarChartAsync` — filter applied.
- [ ] `MatchesRepository.GetRoleBaselinesAsync` — filter applied to the inner `RankedMatches` CTE.
- [ ] `MatchesRepository.GetMatchListAsync` — filter applied (so remakes don't appear in match history lists).
- [ ] `MatchesRepository.GetMatchListSummaryAsync` — filter applied.
- [ ] `MatchupRepository` — filter applied.
- [ ] `ChampionSelectRepository` — filter applied.
- [ ] Integration tests exist (or are updated) confirming a match with `game_duration_sec = 180` is excluded from analytics results.
- [ ] Integration tests confirm a match with `game_duration_sec = 300` is included.

---

## Files to Change

- `server/Mongoose.Api/Infrastructure/Database/Repositories/RepositoryBase.cs` — add constant (or create `QueryConstants.cs`)
- `server/Mongoose.Api/Infrastructure/Database/Repositories/TrendRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/SoloPerformanceRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/OverviewStatsRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/RadarChartRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/MatchesRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/MatchupRepository.cs`
- `server/Mongoose.Api/Infrastructure/Database/Repositories/ChampionSelectRepository.cs`
- Relevant test files in `server/Mongoose.Api.Tests/`

---

## Notes

- `GetRecentMatchHeadersAsync` (used internally for LP update in `MatchHistorySyncJob`) does **not** need the filter — it's for operational purposes, not analytics.
- `GetMatchParticipantsAsync` (used by `MatchNarrativeEndpoint`) does **not** need the filter — if someone navigates directly to a match detail, it should still display.
- `DeleteOldMatchesAsync` does **not** need the filter — retention cleanup operates on all matches.
- This task is valuable even after the ingestion fix ships, because it protects against any existing dirty data already in production databases.
