# Overview "Today at a Glance" Redesign — Work Tasks

Parent feature: [overview-glance-redesign.md](overview-glance-redesign.md)

## Execution Order

```
Task 1  (DTOs)
  ↓
Task 2  (Repository)
  ↓
Task 3  (Endpoint wiring + backend tests)
  ↓
Task 4 ─┬─ Task 5  (new components, can be parallel)
  ↓     ↓
Task 6  (OverviewPage rewiring — depends on 4 & 5)
  ↓
Task 7  (section rename)
  ↓
Task 8  (Solo page heatmap move)
  ↓
Task 9  (E2E updates)
```

Tasks 1–3 are purely backend and can be merged as one PR if preferred. Tasks 4 and 5 are independent of each other. Task 6 depends on both new components existing. Tasks 7 and 8 are independent of each other but logically follow task 6. Task 9 is the final validation pass.

---

## Task 1: Backend — Add `SessionStats` and `SurvivalStats` DTOs

**Scope**: Define the new record types (`SessionStats`, `SessionChampion`, `SurvivalStats`) in the DTOs area. Add `SessionStats?` and `SurvivalStats?` as optional fields on `OverviewResponse`.

**Files to create/modify**:
- New DTO records (location per project convention — likely alongside existing Overview DTOs)
- `OverviewResponse` record — add two new optional parameters

**DTO definitions** (from feature spec):
```csharp
public record SessionStats(
    [property: JsonPropertyName("gamesToday")] int GamesToday,
    [property: JsonPropertyName("winsToday")] int WinsToday,
    [property: JsonPropertyName("lossesToday")] int LossesToday,
    [property: JsonPropertyName("avgKdaToday")] double? AvgKdaToday,
    [property: JsonPropertyName("bestChampionToday")] SessionChampion? BestChampionToday,
    [property: JsonPropertyName("gamesThisWeek")] int GamesThisWeek,
    [property: JsonPropertyName("winsThisWeek")] int WinsThisWeek,
    [property: JsonPropertyName("lossesThisWeek")] int LossesThisWeek,
    [property: JsonPropertyName("avgKdaThisWeek")] double? AvgKdaThisWeek
);

public record SessionChampion(
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("wins")] int Wins,
    [property: JsonPropertyName("losses")] int Losses,
    [property: JsonPropertyName("avgKda")] double AvgKda
);

public record SurvivalStats(
    [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame,
    [property: JsonPropertyName("deathsBefore10Pct")] double DeathsBefore10Pct,
    [property: JsonPropertyName("winRateAtOrBelow3Deaths")] double? WinRateAtOrBelow3Deaths,
    [property: JsonPropertyName("winRateAbove5Deaths")] double? WinRateAbove5Deaths,
    [property: JsonPropertyName("gamesAtOrBelow3Deaths")] int GamesAtOrBelow3Deaths,
    [property: JsonPropertyName("gamesAbove5Deaths")] int GamesAbove5Deaths,
    [property: JsonPropertyName("totalGames")] int TotalGames
);
```

**`OverviewResponse` additions**:
```csharp
[property: JsonPropertyName("sessionStats")] SessionStats? SessionStats = null,
[property: JsonPropertyName("survivalStats")] SurvivalStats? SurvivalStats = null
```

**Acceptance**:
- [ ] New DTOs compile and follow existing `[JsonPropertyName("camelCase")]` convention
- [ ] `OverviewResponse` accepts the new optional fields without breaking existing consumers (default `null`)

---

## Task 2: Backend — Repository methods for session and survival data

**Scope**: Add `GetSessionStatsAsync` and `GetSurvivalStatsAsync` to `IOverviewStatsRepository` and implement in the MySQL repository. No schema changes — queries use existing `matches`, `participants`, and `participant_metrics` tables.

**Files to modify**:
- `IOverviewStatsRepository.cs` — add interface methods
- MySQL implementation (e.g. `OverviewStatsRepository.cs`) — add query implementations

**Interface additions**:
```csharp
Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lastNGames = 20);
```

**Query notes**:
- **Session stats**: Filter `matches` by `game_start_time >= start of today (UTC)`, join `participants` on matched PUUIDs. Group for W/L/KDA. Best champion = highest `(wins / total)` with KDA tiebreaker. Also compute "this week" stats (last 7 days).
- **Survival stats**: Last 20 games across all PUUIDs, join `participant_metrics` for `deaths_pre_10`. Bucket games by death count (≤3 vs 5+), compute win rate per bucket.
- Both queries must use parameterized SQL only.

**Acceptance**:
- [ ] `GetSessionStatsAsync` returns correct aggregation across multiple PUUIDs
- [ ] `GetSurvivalStatsAsync` returns last-20-game death buckets with win rate correlation
- [ ] Repository integration tests pass for multi-PUUID aggregation

---

## Task 3: Backend — Wire new data into `OverviewEndpoint`

**Scope**: Call the new repository methods in `OverviewEndpoint.cs` when `accountId=all` (Overall mode). Populate `SessionStats` and `SurvivalStats` on `OverviewResponse`. Populate the currently-hardcoded `GamesToday` / `GamesThisWeek` fields on each `AccountSummary`.

**Files to modify**:
- `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs`

**Changes**:
1. In the `isAllMode` block, call `GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow)` and `GetSurvivalStatsAsync(selectedPuuids)`
2. Map repository results to `SessionStats` and `SurvivalStats` DTOs
3. Pass them into the `OverviewResponse` constructor
4. Replace the hardcoded `GamesToday: 0, GamesThisWeek: 0` on each `AccountSummary` with real values from the session query (per-account breakdown or aggregate — TBD based on query design in Task 2)
5. Individual mode: both fields remain `null` on `OverviewResponse`

**Tests** (`OverviewEndpointTests.cs`):
- [ ] Overall mode response includes `sessionStats` and `survivalStats` (non-null)
- [ ] Individual mode response does NOT include `sessionStats` / `survivalStats` (null)
- [ ] `accountSummaries[].gamesToday` and `gamesThisWeek` are populated (not 0)

**Acceptance**:
- [ ] No additional API round-trips — new data piggybacks on the existing overview fetch
- [ ] All existing `OverviewEndpointTests` still pass
- [ ] New integration tests pass

---

## Task 4: Frontend — `TodaySessionCard.vue` component

**Scope**: Build `TodaySessionCard.vue` in `client/src/components/overview/`. Self-contained card that receives session data via props and implements the fallback chain.

**File to create**:
- `client/src/components/overview/TodaySessionCard.vue`

**Props**:
- `sessionStats` — the `sessionStats` object from the API (nullable)
- `combinedStats` — the `combinedStats` object for season fallback (nullable)
- `loading` — boolean for skeleton state

**Behavior**:
- **Has games today** (`sessionStats.gamesToday > 0`): Label "TODAY'S SESSION", show today's W/L, WR (color-coded via `useWinRateColor()`), KDA, best champion with icon (`getChampionIconUrl()`)
- **No games today, has games this week** (`sessionStats.gamesThisWeek > 0`): Label "THIS WEEK", show weekly stats, hide best champion
- **No games at all**: Label "THIS SEASON", use `combinedStats` (totalGames, winRate, avgKda)
- **Loading**: Skeleton placeholder matching card dimensions

**Visual details**:
- Same card styling as existing `RankSnapshot`
- W/L strip: reuse `wl-indicator` pattern from `RankSnapshot`
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Game count + W/L: `text-xl font-bold text-text`
- Champion icon: 24px via `getChampionIconUrl()`

**Accessibility**:
- `aria-label="Today's session summary"` on card section
- `aria-label="Win"` / `aria-label="Loss"` on each W/L dot
- Win rate: numeric label always present (not color-only)

**Tests** (`test/unit/TodaySessionCard.spec.js`):
- [ ] Renders today's stats when `gamesToday > 0`
- [ ] Falls back to "THIS WEEK" when `gamesToday === 0` and `gamesThisWeek > 0`
- [ ] Falls back to "THIS SEASON" using `combinedStats` when no session data
- [ ] Shows loading skeleton when `loading` is true

---

## Task 5: Frontend — `SurvivalCheckCard.vue` component

**Scope**: Build `SurvivalCheckCard.vue` in `client/src/components/overview/`. Self-contained card showing death/win-rate correlation.

**File to create**:
- `client/src/components/overview/SurvivalCheckCard.vue`

**Props**:
- `survivalStats` — the `survivalStats` object from the API (nullable)
- `loading` — boolean for skeleton state

**Visual structure**:
```
SURVIVAL CHECK
Avg 4.2 deaths/game
████████░░  32% before 10 min
≤3 deaths → 72% WR   (7 games)
5+ deaths → 38% WR   (8 games)
```

**Components**:
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Avg deaths: `text-xl font-bold text-text`
- Progress bar: `div` with `bg-error` fill, `bg-elevated` track, `border-radius: var(--radius-sm)`
- Win rate rows: color-coded via `useWinRateColor()` — ≤3 deaths in success tones, 5+ in error tones
- Game counts: `text-xs text-text-secondary`
- If fewer than 5 games in a bucket: dim the row, show "limited data" tooltip

**Accessibility**:
- `aria-label="Survival check: death analysis"` on card section
- Progress bar: `role="meter"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-label="Percentage of deaths before 10 minutes"`
- Win rate values: numeric label always present (not color-only)

**Tests** (`test/unit/SurvivalCheckCard.spec.js`):
- [ ] Renders death stats and win rate rows
- [ ] Progress bar width matches `deathsBefore10Pct`
- [ ] Shows "limited data" tooltip when a bucket has < 5 games
- [ ] Shows loading skeleton when `loading` is true

---

## Task 6: Frontend — Rewire `OverviewPage.vue` slots (Overall vs Individual)

**Scope**: Modify `OverviewPage.vue` to conditionally render the new cards in Overall mode while preserving the existing layout in Individual mode.

**File to modify**:
- `client/src/views/OverviewPage.vue`

**Changes**:
1. **`#glance-left`**: Render `TodaySessionCard` when `authStore.isOverallMode`, otherwise keep `RankSnapshot`
2. **`#glance-right`**: Render `SurvivalCheckCard` when `authStore.isOverallMode`, otherwise keep `ChampionSelectCTA`
3. **`#recent-left`**: Render `ChampionSelectCTA` when `authStore.isOverallMode` (moved from glance-right), otherwise remove `MatchActivityHeatmap`
4. **Remove** the `MatchActivityHeatmap` import and its `getMatchActivity()` call from this page
5. **Remove** `matchActivityData` ref and related logic
6. Pass `sessionStats`, `survivalStats`, and `combinedStats` from `overviewData` as props to the new cards

**Tests** (update `test/unit/OverviewPage.spec.js`):
- [ ] Overall mode renders `TodaySessionCard` in glance-left, `SurvivalCheckCard` in glance-right
- [ ] Overall mode renders `ChampionSelectCTA` in recent-left
- [ ] Individual mode renders `RankSnapshot` in glance-left, `ChampionSelectCTA` in glance-right
- [ ] `MatchActivityHeatmap` is no longer rendered on Overview

---

## Task 7: Frontend — Rename "Recent matches" to "Quick actions"

**Scope**: Change the section heading in `OverviewLayout.vue`. Applies to both Overall and Individual modes.

**File to modify**:
- `client/src/components/overview/OverviewLayout.vue`

**Change**:
```diff
- <h2 class="section-title">Recent matches</h2>
+ <h2 class="section-title">Quick actions</h2>
```

**Tests** (update `test/unit/OverviewLayout.spec.js`):
- [ ] Section heading reads "Quick actions"

**Note**: Small change but it affects E2E selectors — keep as a discrete commit for clean review.

---

## Task 8: Frontend — Add `MatchActivityHeatmap` to Solo page Zone 4

**Scope**: Move the heatmap from Overview to the Solo page, filling the vertical gap below Performance Profile in the left column of the deep analysis grid.

**File to modify**:
- `client/src/views/SoloStatsPage.vue`

**Changes**:
1. Import `MatchActivityHeatmap` and `getMatchActivity` from existing locations
2. Add `getMatchActivity()` call to `SoloStatsPage`'s data fetching (pass through queue filter and time range)
3. Add `MatchActivityHeatmap` wrapped in `BaseCard` (title "Match Activity") as the **third child** inside `.deep-analysis-grid`, after Danger Zones
4. Refactor `.deep-analysis-grid` CSS:

```css
.deep-analysis-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  grid-template-rows: auto 1fr;
  gap: var(--spacing-lg);
}

/* Performance Profile: top-left */
.deep-analysis-grid > :nth-child(1) {
  grid-column: 1;
  grid-row: 1;
}

/* Danger Zones: spans full right column */
.deep-analysis-grid > :nth-child(2) {
  grid-column: 2;
  grid-row: 1 / -1;
}

/* Match Activity: bottom-left, fills remaining height */
.deep-analysis-grid > :nth-child(3) {
  grid-column: 1;
  grid-row: 2;
}
```

5. Mobile (`max-width: 768px`): collapse to single column, all three stack vertically (Performance Profile → Match Activity → Danger Zones)

**Result layout**:
```
┌─────────────────────────┐  ┌────────────────────────────────┐
│  Performance Profile    │  │                                │
│  (RadarChart)           │  │  Danger Zones                  │
├─────────────────────────┤  │  (DangerZonesMap)              │
│  Match Activity         │  │                                │
│  (MatchActivityHeatmap) │  │                                │
└─────────────────────────┘  └────────────────────────────────┘
```

**Tests** (update `test/unit/SoloStatsPage.spec.js`):
- [ ] `MatchActivityHeatmap` renders in Zone 4 below Performance Profile
- [ ] Heatmap updates when queue filter changes

---

## Task 9: E2E test updates

**Scope**: Update Playwright tests to validate the new layout in both Overview and Solo dashboards.

**Files to modify**:
- `client/e2e/overview-dashboard.spec.js`
- `client/e2e/solo-dashboard.spec.js`

**Overview E2E tests**:
- [ ] Overall mode shows "Today's Session" and "Survival Check" cards
- [ ] Section heading reads "Quick actions"
- [ ] `ChampionSelectCTA` is visible in the quick actions section
- [ ] `MatchActivityHeatmap` is NOT present on the overview page

**Solo E2E tests**:
- [ ] `MatchActivityHeatmap` is visible in Zone 4 below Performance Profile

**Selector updates**: Existing tests may reference "Today at a glance" heading and `RankSnapshot` in Overall mode — update to match the new component `data-testid` attributes.
