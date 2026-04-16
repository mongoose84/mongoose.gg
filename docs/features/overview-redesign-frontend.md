# Overview Redesign — Frontend Tasks

> Source: [overview-page-redesign.md](overview-page-redesign.md) — Tasks 4–8

## Execution Order

```
Task 4 ─┬─ Task 5  (new components, can be parallel)
        ↓
Task 6  (OverviewPage rewiring — depends on 4 & 5)
  ↓
Task 7  (section + slot renames)
  ↓
Task 8  (Solo page heatmap move)
```

Tasks 4 and 5 are independent of each other. Task 6 depends on both new components existing. Tasks 7 and 8 are independent of each other but logically follow task 6.

---

## Task 4: `TodaySessionCard.vue` component

→ [overview-redesign-task4-today-session-card.md](overview-redesign-task4-today-session-card.md)

---

## Task 5: `SurvivalCheckCard.vue` component

→ [overview-redesign-task5-survival-check-card.md](overview-redesign-task5-survival-check-card.md)

---

## Task 6: Rewire `OverviewPage.vue` slots

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

## Task 7: Rename sections and slots in `OverviewLayout.vue`

**Scope**: Rename the "Today at a glance" heading to "At a glance", the "Recent matches" heading to "Quick actions", and rename the `#recent-left` / `#recent-right` slots to `#actions-left` / `#actions-right` to match the new heading.

**Files to modify**:
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

## Task 8: Add `MatchActivityHeatmap` to Solo page Zone 4

**Scope**: Move the heatmap from Overview to the Solo page, filling the vertical gap below Performance Profile in the left column of the deep analysis grid.

**Files to modify**:
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

/* Match Activity: bottom-left, sizes to content */
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
