# Overview Redesign — Frontend Tasks

Parent feature: [overview-glance-redesign.md](overview-glance-redesign.md)

## Execution Order

```
Task 1 ─┬─ Task 2  (new components, can be parallel)
  ↓     ↓
Task 3  (OverviewPage rewiring — depends on 1 & 2)
  ↓
Task 4  (section rename)
  ↓
Task 5  (Solo page heatmap move)
```

Tasks 1 and 2 are independent. Task 3 depends on both new components existing. Tasks 4 and 5 are independent of each other but logically follow Task 3.

**Backend dependency**: All frontend tasks require the backend tasks to be complete (API returns `sessionStats` and `survivalStats`). Tasks 1–2 can be built with mock data in parallel with backend work.

---

## Task 1: `TodaySessionCard.vue` component

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

## Task 2: `SurvivalCheckCard.vue` component

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

## Task 3: Rewire `OverviewPage.vue` slots (Overall vs Individual)

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

## Task 4: Rename "Recent matches" to "Quick actions"

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

## Task 5: Add `MatchActivityHeatmap` to Solo page Zone 4

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
