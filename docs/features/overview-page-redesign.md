# Overview "At a Glance" Redesign — Work Tasks

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
Task 6  (OverviewPage rewiring — depends on 4 & 5, no mode branching)
  ↓
Task 7  (section + slot renames)
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
- `OverviewQueryModels.cs` — add query model definitions

**Query model definitions** (add to `Core/QueryModels/OverviewQueryModels.cs`):
```csharp
/// <summary>
/// Per-PUUID session breakdown. The repository returns one entry per PUUID so the
/// endpoint can populate both the aggregate SessionStats DTO and per-account
/// AccountSummary.GamesToday / GamesThisWeek fields in a single query.
/// </summary>
public record PerAccountSessionData(
    string Puuid,
    int GamesToday,
    int WinsToday,
    int LossesToday,
    double? AvgKdaToday,
    string? BestChampionName,
    int BestChampionWins,
    int BestChampionLosses,
    double BestChampionAvgKda,
    int GamesThisWeek,
    int WinsThisWeek,
    int LossesThisWeek,
    double? AvgKdaThisWeek
);

/// <summary>
/// Aggregate session stats across all requested PUUIDs.
/// Built by the endpoint from the per-account breakdown.
/// </summary>
public record SessionStatsData(
    IReadOnlyList<PerAccountSessionData> PerAccount
);

/// <summary>
/// Survival analysis over the last N games.
/// </summary>
public record SurvivalStatsData(
    double AvgDeathsPerGame,
    double DeathsBefore10Pct,
    double? WinRateAtOrBelow3Deaths,
    double? WinRateAbove5Deaths,
    int GamesAtOrBelow3Deaths,
    int GamesAbove5Deaths,
    int TotalGames
);
```

**Interface additions**:
```csharp
Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lastNGames = 20);
```

**Query notes**:
- **Session stats**: Filter `matches` by `game_start_time >= start of today (UTC)`, join `participants` on matched PUUIDs. **Group by PUUID** to produce per-account breakdown (`PerAccountSessionData`). Aggregate W/L/KDA. Best champion = highest `(wins / total)` with KDA tiebreaker. Also compute "this week" stats (last 7 days). The endpoint aggregates the per-account rows into the `SessionStats` DTO and distributes per-PUUID counts into `AccountSummary.GamesToday` / `GamesThisWeek`.
- **Survival stats**: **Last 20 games across all PUUIDs sorted by `game_start_time` descending** (not 20 per PUUID). Join `participant_metrics` for `deaths_pre_10`. Bucket games by death count (≤3 vs 5+), compute win rate per bucket.
- Both queries must use parameterized SQL only.

**Acceptance**:
- [ ] `GetSessionStatsAsync` returns per-PUUID breakdown for correct aggregation across multiple PUUIDs
- [ ] `GetSurvivalStatsAsync` returns last-20-game death buckets (across all accounts) with win rate correlation
- [ ] Repository integration tests pass for multi-PUUID aggregation

---

## Task 3: Backend — Wire new data into `OverviewEndpoint`

**Scope**: Call the new repository methods in `OverviewEndpoint.cs` for every request. Populate `SessionStats` and `SurvivalStats` on `OverviewResponse`. When `accountId=all`, populate `GamesToday` / `GamesThisWeek` on each `AccountSummary` from the per-PUUID session breakdown. Also remove the now-unused `wlLast20`, `last20Wins`, and `last20Losses` fields from the `RankSnapshot` DTO and the query that computes them.

**Files to modify**:
- `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs`
- `server/Mongoose.Api/Application/DTOs/Overview/OverviewDto.cs` — remove `wlLast20`, `last20Wins`, `last20Losses` from `RankSnapshot`

**Changes**:
1. Call `GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow)` and `GetSurvivalStatsAsync(selectedPuuids)` for every request (regardless of `accountId`)
2. **Parallelize** the new calls with existing independent calls using `Task.WhenAll`:
   ```
   GetPrimaryQueueAsync → GetLast20MatchesAsync → Task.WhenAll(
       GetLastMatchAsync,
       GetMostPlayedChampionAsync,
       GetSessionStatsAsync,      // new
       GetSurvivalStatsAsync       // new
   )
   ```
3. Map repository results to `SessionStats` and `SurvivalStats` DTOs. Aggregate the per-PUUID `PerAccountSessionData` rows into the top-level `SessionStats` (sum games/wins/losses, weighted-average KDA, pick best champion across all accounts).
4. Pass them into the `OverviewResponse` constructor
5. In the `isAllMode` block, replace the hardcoded `GamesToday: 0, GamesThisWeek: 0` on each `AccountSummary` with real values looked up from the per-PUUID breakdown by matching PUUID
6. **Remove `wlLast20`, `last20Wins`, `last20Losses`** from the `RankSnapshot` DTO — these fields are no longer rendered by any frontend component after this redesign. Remove the corresponding backend logic that computes them (the `GetLast20MatchesAsync` call can be removed if it served no other purpose, or kept if still needed for the LP snapshot).

**Tests** (`OverviewEndpointTests.cs`):
- [ ] Response includes `sessionStats` and `survivalStats` (non-null) for both `accountId=all` and single-account requests
- [ ] `accountSummaries[].gamesToday` and `gamesThisWeek` are populated (not 0) when `accountId=all`
- [ ] `rankSnapshot` no longer includes `wlLast20`, `last20Wins`, or `last20Losses`

**Acceptance**:
- [ ] No additional API round-trips — new data piggybacks on the existing overview fetch
- [ ] New DB calls are parallelized via `Task.WhenAll` alongside existing independent calls
- [ ] All existing `OverviewEndpointTests` still pass (update assertions for removed `RankSnapshot` fields)
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
- Same card styling as existing `RankSnapshot` (surface background, border, radius-lg)
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

> **User validation note**: SurvivalCheckCard is analytical for an orientation page. Ship it, then gather user feedback on whether the death-bucket breakdown is useful at this level or should be simplified to a single-sentence insight (e.g. "You win 72% when you die ≤3 times"). If users find it too dense, simplify to headline metric only and move the full breakdown to the Solo page.

---

## Task 6: Frontend — Rewire `OverviewPage.vue` slots

**Scope**: Modify `OverviewPage.vue` to use the new cards unconditionally. The redesigned layout is universal — no mode branching. Rank display moves from `RankSnapshot` into the header components.

**Files to modify**:
- `client/src/views/OverviewPage.vue`
- `client/src/components/overview/OverviewPlayerHeader.vue`
- `client/src/components/overview/OverviewAccountCards.vue` (already shows rank — no changes needed)

**`OverviewPlayerHeader` changes** (individual account mode):
- Add props: `rank` (`String`, nullable), `lp` (`Number`, nullable), `primaryQueueLabel` (`String`, nullable)
- Display a compact rank badge inline next to the summoner name / region row:
  - Rank emblem (24px, same local asset path as `RankSnapshot`), formatted rank text (e.g. "Silver IV"), LP
  - If unranked: show "Unranked" text, no emblem
  - Styling: `text-sm text-text-secondary`, emblem + text in a horizontal flex row
- **Verify** that rank emblem assets exist at the path used by `RankSnapshot` and cover all tiers (Iron → Challenger + Unranked). If `RankSnapshot` uses Data Dragon CDN URLs instead of local assets, use the same CDN approach here.
- `OverviewAccountCards` already displays Solo/Flex rank per account card — no changes needed

**`OverviewPage.vue` changes**:
1. **`#header`**: Pass `rankSnapshot` data as new props to `OverviewPlayerHeader` (rank, lp, primaryQueueLabel)
2. **`#glance-left`**: Replace `RankSnapshot` with `TodaySessionCard`
3. **`#glance-right`**: Replace `ChampionSelectCTA` with `SurvivalCheckCard`
4. **`#actions-left`** (renamed from `#recent-left`): Replace `MatchActivityHeatmap` with `ChampionSelectCTA`
5. **Remove** the `MatchActivityHeatmap` import and its `getMatchActivity()` call from this page
6. **Remove** `matchActivityData` ref and related logic
7. **Remove** `RankSnapshot` import and related props/computed properties (e.g. `rankSnapshotLabel`)
8. Pass `sessionStats`, `survivalStats`, and `combinedStats` from `overviewData` as props to the new cards

**Tests** (update `test/unit/OverviewPage.spec.js`):
- [ ] Renders `TodaySessionCard` in glance-left and `SurvivalCheckCard` in glance-right
- [ ] Renders `ChampionSelectCTA` in actions-left
- [ ] `RankSnapshot` component is no longer rendered
- [ ] `MatchActivityHeatmap` is no longer rendered on Overview
- [ ] `OverviewPlayerHeader` shows rank emblem and LP when rank data is present
- [ ] `OverviewPlayerHeader` shows "Unranked" when rank data is null

---

## Task 7: Frontend — Rename sections and slots in `OverviewLayout.vue`

**Scope**: Rename the "Today at a glance" heading to "At a glance", the "Recent matches" heading to "Quick actions", and rename the `#recent-left` / `#recent-right` slots to `#actions-left` / `#actions-right` to match the new heading.

**File to modify**:
- `client/src/components/overview/OverviewLayout.vue`
- `client/src/views/OverviewPage.vue` (update slot usage to match new names)

**Changes**:
```diff
- <h2 class="section-title">Today at a glance</h2>
+ <h2 class="section-title">At a glance</h2>
```
```diff
- <h2 class="section-title">Recent matches</h2>
+ <h2 class="section-title">Quick actions</h2>
```
```diff
- <section v-if="$slots['recent-left'] || $slots['recent-right']" class="overview-section">
+ <section v-if="$slots['actions-left'] || $slots['actions-right']" class="overview-section">
```
```diff
- <slot name="recent-left"></slot>
+ <slot name="actions-left"></slot>
- <slot name="recent-right"></slot>
+ <slot name="actions-right"></slot>
```

**`OverviewPage.vue`** slot usage update (must align with Task 6):
```diff
- <template #recent-left>
+ <template #actions-left>
- <template #recent-right>
+ <template #actions-right>
```

**Tests** (update `test/unit/OverviewLayout.spec.js`):
- [ ] Section heading reads "At a glance" (not "Today at a glance")
- [ ] Section heading reads "Quick actions" (not "Recent matches")
- [ ] Slots `#actions-left` and `#actions-right` render content correctly

**Note**: Affects E2E selectors — keep as a discrete commit for clean review.

---

## Task 8: Frontend — Add `MatchActivityHeatmap` to Solo page Zone 4

**Scope**: Move the heatmap from Overview to the Solo page, filling the vertical gap below Performance Profile in the left column of the deep analysis grid.

**File to modify**:
- `client/src/views/SoloStatsPage.vue`
- `client/src/composables/useSoloDashboardData.js` — add `getMatchActivity()` call here (not directly in the page)

**Changes**:
1. **Add `getMatchActivity()` to `useSoloDashboardData` composable** — this is the correct integration point because the composable already manages queue/time-range reactivity and loading states for all Solo page data. Adding the fetch directly in `SoloStatsPage.vue` would break the existing pattern.
2. Import `MatchActivityHeatmap` in `SoloStatsPage.vue`
3. Add `MatchActivityHeatmap` wrapped in `BaseCard` (title "Match Activity") as the **third child** inside `.deep-analysis-grid`, after Danger Zones
4. Refactor `.deep-analysis-grid` CSS:

```css
.deep-analysis-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  grid-template-rows: auto auto;
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

/* Match Activity: bottom-left, sizes to content (does not stretch to fill Danger Zones height) */
.deep-analysis-grid > :nth-child(3) {
  grid-column: 1;
  grid-row: 2;
  align-self: start;
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
- [ ] "Today's Session" and "Survival Check" cards are visible
- [ ] Section heading reads "At a glance" (not "Today at a glance")
- [ ] Section heading reads "Quick actions" (not "Recent matches")
- [ ] `ChampionSelectCTA` is visible in the quick actions section
- [ ] `RankSnapshot` component is NOT present on the overview page
- [ ] Rank emblem and LP are visible in the player header (individual mode)
- [ ] `MatchActivityHeatmap` is NOT present on the overview page

**Solo E2E tests**:
- [ ] `MatchActivityHeatmap` is visible in Zone 4 below Performance Profile

**Selector updates**: Existing tests may reference "Today at a glance", "Recent matches" heading, `#recent-left`/`#recent-right` slots, and `RankSnapshot` — update to match the new headings ("At a glance", "Quick actions"), slot names (`#actions-left`/`#actions-right`), and `data-testid` attributes.
