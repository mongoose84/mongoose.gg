# Feature: Overview "Today at a Glance" Redesign (Overall Mode)

## Problem Statement

When a Pro user has multiple linked Riot accounts, the Overview page in Overall mode shows redundant rank information:
- **Your Accounts** (header) already displays Solo rank and Flex rank per account card
- **Today at a Glance** shows "Highest Rank (Solo)" — repeating the same rank data one section below

This wastes the most valuable real-estate on the page. The Overview's time budget is 5–15 seconds; redundant data costs attention without adding insight.

Additionally, the **Recent Matches** section contains the `MatchActivityHeatmap`, which is a deep analysis artifact better suited to the Solo page, alongside two CTAs (`ChampionSelectCTA` and `SoloAnalyticsCTA`) that serve as routing elements.

## Proposed Solution

Replace the redundant `RankSnapshot` in Overall mode with two new insight cards:
1. **Today's Session** — session W/L, WR, KDA, best champion today
2. **Survival Check** — deaths-per-game with win rate correlation

Move `MatchActivityHeatmap` to the Solo page (Zone 4, below Performance Profile). Relocate `ChampionSelectCTA` from glance-right to recent-left. Rename the "Recent matches" section to **"Quick actions"**.

## User Stories

### Primary User Story
As a multi-account player, I want to see how my current day/session is going across all accounts so that I can decide whether to keep playing or take a break.

### Additional User Stories
- As a ranked player, I want to see how my death count correlates with my win rate so that I have a concrete, actionable habit to improve.
- As a multi-account player, I want the overview to show me different information in each section so that I don't waste time reading the same rank twice.

## Requirements

### Functional Requirements

1. **Today's Session card** replaces `RankSnapshot` in the `#glance-left` slot when in Overall mode
2. **Survival Check card** replaces `ChampionSelectCTA` in the `#glance-right` slot when in Overall mode
3. `ChampionSelectCTA` moves to the `#recent-left` slot (was occupied by `MatchActivityHeatmap`)
4. `MatchActivityHeatmap` is removed from Overview and added to the Solo page in Zone 4 (`#deep-analysis`), placed in the left column below Performance Profile, adjacent to Danger Zones on the right
5. Section heading "Recent matches" is renamed to **"Quick actions"**
6. **Fallback chain for Today's Session**: if no games today → "This week: X games, Y% WR"; if no games this week → "This season: X games, Y% WR" using `CombinedStats`
7. Survival Check always uses last 20 games across all accounts (never empty if the user has played at all)
8. In **Individual mode** (single account selected), the existing `RankSnapshot` and layout remain unchanged

### Non-Functional Requirements
- **Performance**: New cards must render within the existing overview fetch — no additional API round-trips where possible
- **Security**: No new user input; all data resolved server-side via existing PUUID resolution
- **Accessibility**: Cards must meet WCAG AA contrast, include `aria-label` on visual indicators, keyboard-navigable
- **Glance-ability**: Both cards must be comprehensible within 3 seconds each (per UX spec principle 4)

## Technical Approach

### Backend Changes

#### New endpoint data: Session stats
Add a session/activity stats object to the existing `OverviewResponse` for Overall mode.

**New DTO**:
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
```

**New DTO**:
```csharp
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

Add both as optional fields on `OverviewResponse`:
```csharp
[property: JsonPropertyName("sessionStats")] SessionStats? SessionStats = null,
[property: JsonPropertyName("survivalStats")] SurvivalStats? SurvivalStats = null
```

#### New repository method: `IOverviewStatsRepository`

```csharp
Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lastNGames = 20);
```

**Session stats query** — filter `matches` by `game_start_time` >= start of today (UTC), join `participants` on matched PUUIDs. Group for W/L/KDA. Best champion = highest (wins / total) with KDA tiebreaker.

**Survival stats query** — last 20 games across all PUUIDs, join `participant_metrics` for `deaths_pre_10`. Bucket games by death count (≤3 vs 5+), compute win rate per bucket.

#### Populate existing `gamesToday` / `gamesThisWeek` fields
The existing `AccountSummary` DTO has `gamesToday` and `gamesThisWeek` [currently hardcoded to 0](../../server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs). Populate from the same session query.

### Frontend Changes

**New components**:
- `client/src/components/overview/TodaySessionCard.vue` — Today's Session display
- `client/src/components/overview/SurvivalCheckCard.vue` — Survival Check display

**Modified components**:
- `client/src/views/OverviewPage.vue` — conditional slot rendering for Overall vs Individual mode; remove `MatchActivityHeatmap` and its `getMatchActivity()` data fetch
- `client/src/views/SoloStatsPage.vue` — add `MatchActivityHeatmap` to `#deep-analysis` slot, in the left column below Performance Profile; refactor `deep-analysis-grid` to a 2-column layout with stacked left column; add `getMatchActivity()` data fetch
- `client/src/components/overview/OverviewLayout.vue` — rename "Recent matches" heading to "Quick actions"

**Moved components**:
- `MatchActivityHeatmap` — removed from `OverviewPage.vue` `#recent-left` slot, added to `SoloStatsPage.vue` `#deep-analysis` left column, below Performance Profile and beside Danger Zones

### Database Changes
No schema changes. All data is derived from existing tables:
- `matches` (game_start_time, queue_id)
- `participants` (puuid, win, kills, deaths, assists, champion_name)
- `participant_metrics` (deaths_pre_10)

### API Contracts

The existing `GET /api/v2/overview/{userId}?accountId=all` response gains two new optional fields:

**Response additions** (only present when `accountId=all`):
```json
{
  "sessionStats": {
    "gamesToday": 5,
    "winsToday": 3,
    "lossesToday": 2,
    "avgKdaToday": 3.4,
    "bestChampionToday": {
      "championName": "Jinx",
      "wins": 2,
      "losses": 0,
      "avgKda": 4.8
    },
    "gamesThisWeek": 12,
    "winsThisWeek": 7,
    "lossesThisWeek": 5,
    "avgKdaThisWeek": 3.1
  },
  "survivalStats": {
    "avgDeathsPerGame": 4.8,
    "deathsBefore10Pct": 0.35,
    "winRateAtOrBelow3Deaths": 0.72,
    "winRateAbove5Deaths": 0.38,
    "gamesAtOrBelow3Deaths": 7,
    "gamesAbove5Deaths": 8,
    "totalGames": 20
  }
}
```

## UI/UX Requirements

All views follow the design system in [UI/UX Spec](../../.github/specs/ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

### Overview Page Layout (Overall Mode)

**Layout**: Existing `OverviewLayout.vue` slot structure, no new pages.

**Structure (after redesign)**:
```
┌─ HEADER (full width) ─────────────────────────────────────────────┐
│  OverviewAccountCards — rank per account (unchanged)              │
└───────────────────────────────────────────────────────────────────┘

┌─ TODAY AT A GLANCE ──────────────────────────────────────────────┐
│                                                                   │
│  ┌─ #glance-left ──────────┐  ┌─ #glance-right ──────────────┐  │
│  │  TodaySessionCard       │  │  SurvivalCheckCard            │  │
│  │                         │  │                                │  │
│  │  5 games · 3W – 2L     │  │  Avg 4.2 deaths/game          │  │
│  │  ■ ■ ■ □ □   60% WR    │  │  ████████░░ (32% before 10m)  │  │
│  │                         │  │                                │  │
│  │  3.4 KDA               │  │  ≤3 deaths → 72% WR           │  │
│  │  Best: Jinx (2-0, 4.8) │  │  5+ deaths → 38% WR           │  │
│  └─────────────────────────┘  └────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

┌─ QUICK ACTIONS ──────────────────────────────────────────────────┐
│                                                                   │
│  ┌─ #recent-left ──────────┐  ┌─ #recent-right ──────────────┐  │
│  │  ChampionSelectCTA      │  │  AnalysisStatusCard           │  │
│  │                         │  │  SoloAnalyticsCTA             │  │
│  └─────────────────────────┘  └────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

┌─ LATEST MATCH (full width) ──────────────────────────────────────┐
│  LastMatchCard (unchanged)                                        │
└───────────────────────────────────────────────────────────────────┘
```

### TodaySessionCard

**Layout**: Fits in `#glance-left` slot. Same card styling as existing `RankSnapshot`.

**Structure**:
```
┌────────────────────────────────────┐
│  TODAY'S SESSION          (or fallback label)
│
│  5 games · 3W – 2L
│  ■ ■ ■ □ □            60% WR (color-coded)
│
│  3.4 KDA
│  Best: [icon] Jinx (2-0, 4.8 KDA)
└────────────────────────────────────┘
```

**Components**:
- Section label: `span` — `text-xs uppercase tracking-wide text-text-secondary`
- Game count + W/L: `span` — `text-xl font-bold text-text`
- W/L strip: reuse same `wl-indicator` pattern from existing `RankSnapshot`
- WR display: color-coded via `useWinRateColor()` composable
- Best champion: champion icon (24px via `getChampionIconUrl()`) + name + record + KDA

**Behavior**:
- **Has games today**: Show today's session stats
- **No games today, has games this week**: Label changes to "THIS WEEK", shows weekly stats, best champion hidden
- **No games at all**: Label shows "THIS SEASON", uses `CombinedStats` (totalGames, winRate, avgKda)
- Loading state: skeleton placeholder matching card dimensions

**Accessibility**:
- `aria-label="Today's session summary"` on the card section
- W/L indicators: `aria-label="Win"` / `aria-label="Loss"` on each dot
- Win rate color: text label always present (not color-only)

### SurvivalCheckCard

**Layout**: Fits in `#glance-right` slot. Same card styling.

**Structure**:
```
┌────────────────────────────────────┐
│  SURVIVAL CHECK
│
│  Avg 4.2 deaths/game
│  ████████░░  32% before 10 min
│
│  ≤3 deaths → 72% WR   (7 games)
│  5+ deaths → 38% WR   (8 games)
└────────────────────────────────────┘
```

**Components**:
- Section label: `span` — `text-xs uppercase tracking-wide text-text-secondary`
- Avg deaths: `span` — `text-xl font-bold text-text`
- Progress bar: `div` with `bg-error` fill, `bg-elevated` track, `border-radius: var(--radius-sm)`
- Win rate rows: color-coded via `useWinRateColor()` — ≤3 deaths row in success tones, 5+ deaths row in error tones
- Game counts: `span` — `text-xs text-text-secondary`

**Behavior**:
- Always shows data from last 20 games (never empty if user has played)
- If fewer than 5 games in a bucket, dim the row and show "limited data" tooltip
- Loading state: skeleton placeholder

**Accessibility**:
- `aria-label="Survival check: death analysis"` on the card section
- Progress bar: `role="meter"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-label="Percentage of deaths before 10 minutes"`
- Win rate values: not color-only — always includes numeric label

### OverviewLayout Section Rename

Change in `OverviewLayout.vue`:
```
- <h2 class="section-title">Recent matches</h2>
+ <h2 class="section-title">Quick actions</h2>
```

### Individual Mode (single account)

No changes. The existing `RankSnapshot` + `ChampionSelectCTA` layout remains in the glance slots. The "Quick actions" rename applies to both modes.

### Solo Page — MatchActivityHeatmap Placement

**Location**: `SoloStatsPage.vue`, inside the `#deep-analysis` slot. The heatmap sits in the left column below Performance Profile, beside the Danger Zones card on the right. The bottom of the heatmap aligns with the bottom of Danger Zones, filling the vertical gap that currently exists below the shorter RadarChart.

**Current Zone 4 structure**:
```
┌─ ZONE 4: DEEP ANALYSIS ─────────────────────────────────────────┐
│  ┌─────────────────────────┐  ┌────────────────────────────────┐ │
│  │  Performance Profile    │  │                                │ │
│  │  (RadarChart)           │  │  Danger Zones                  │ │
│  │  ~400px tall            │  │  (DangerZonesMap)              │ │
│  └─────────────────────────┘  │  ~700px+ tall                  │ │
│                               │  (512px map + controls +       │ │
│  ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐  │   phase bar + header)          │ │
│  │   GAP (unused space)    │  │                                │ │
│  └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘  └────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

**After (heatmap fills the gap)**:
```
┌─ ZONE 4: DEEP ANALYSIS ─────────────────────────────────────────┐
│  ┌─────────────────────────┐  ┌────────────────────────────────┐ │
│  │  Performance Profile    │  │                                │ │
│  │  (RadarChart)           │  │  Danger Zones                  │ │
│  │                         │  │  (DangerZonesMap)              │ │
│  ├─────────────────────────┤  │                                │ │
│  │  Match Activity         │  │                                │ │
│  │  (MatchActivityHeatmap) │  │                                │ │
│  │                         │  │                                │ │
│  └─────────────────────────┘  └────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

**CSS implementation**: Refactor `deep-analysis-grid` from `grid-template-columns: repeat(2, 1fr)` to a 2-column layout with a stacked left column:

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

On mobile (`max-width: 768px`), collapse to single column: all three cards stack vertically in order (Performance Profile → Match Activity → Danger Zones).

**Implementation in `SoloStatsPage.vue`**: Add the `MatchActivityHeatmap` (wrapped in a `BaseCard` with title "Match Activity") as the third child inside `deep-analysis-grid`, after the Danger Zones card.

**Data source**: Call `getMatchActivity()` from `SoloStatsPage.vue` (moved from `OverviewPage.vue`). The heatmap respects the active queue filter and time range from Zone 1 (pass through props).

**Rationale**: The Danger Zones card (512px map + controls) is significantly taller than the Performance Profile (~400px), leaving a visible gap in the left column. The heatmap fills this gap naturally, creating a tightly packed layout where both columns bottom-align. The reading order flows: "How am I performing?" (radar, top-left) → "When am I playing?" (heatmap, bottom-left) → "Where am I dying?" (danger zones, right).

## Testing Strategy

### Unit Tests (Vitest)

- [ ] `TodaySessionCard.spec.js` — renders today's stats, fallback to week, fallback to season, loading state, empty state
- [ ] `SurvivalCheckCard.spec.js` — renders death stats, win rate correlation rows, progress bar, limited data tooltip, loading state
- [ ] `OverviewPage.spec.js` — update existing tests: Overall mode renders new cards instead of `RankSnapshot`; Individual mode still renders `RankSnapshot`; `MatchActivityHeatmap` no longer rendered
- [ ] `OverviewLayout.spec.js` — section heading reads "Quick actions"
- [ ] `SoloStatsPage.spec.js` — update existing tests: `MatchActivityHeatmap` renders in Zone 4 below the deep analysis grid

### Integration Tests (xUnit)

- [ ] `OverviewEndpointTests.cs` — Overall mode response includes `sessionStats` and `survivalStats`
- [ ] `OverviewEndpointTests.cs` — Individual mode response does NOT include `sessionStats` / `survivalStats`
- [ ] `OverviewEndpointTests.cs` — `accountSummaries` have populated `gamesToday` / `gamesThisWeek`
- [ ] Repository tests for `GetSessionStatsAsync` and `GetSurvivalStatsAsync` — correct aggregation across multiple PUUIDs

### E2E Tests (Playwright)

- [ ] `overview-dashboard.spec.js` — Overall mode shows "Today's Session" and "Survival Check" cards
- [ ] `overview-dashboard.spec.js` — section heading reads "Quick actions"
- [ ] `overview-dashboard.spec.js` — ChampionSelectCTA is visible in the quick actions section
- [ ] `overview-dashboard.spec.js` — `MatchActivityHeatmap` is NOT present on the overview page
- [ ] `solo-dashboard.spec.js` — `MatchActivityHeatmap` is visible in Zone 4 below Performance Profile

### Manual Testing Scenarios
1. Pro user with 2+ accounts, games played today → verify session card shows today's data
2. Pro user with 2+ accounts, no games today but games this week → verify fallback
3. Pro user with 2+ accounts, new account with < 5 games → verify survival card shows limited data state
4. Free user (single account) → verify layout is unchanged (RankSnapshot + ChampionSelectCTA)
5. Overall mode → Individual mode switch → verify cards swap correctly
6. Solo page → verify heatmap renders below Performance Profile / Danger Zones grid
7. Solo page → change queue filter → verify heatmap updates with filtered data

## Validation Criteria

Feature is considered complete when:
- [ ] Today's Session card renders with correct fallback chain (today → week → season)
- [ ] Survival Check card renders with death/WR correlation from last 20 games
- [ ] `RankSnapshot` no longer appears in Overall mode glance section
- [ ] `MatchActivityHeatmap` is removed from Overview and renders on Solo page in Zone 4, left column below Performance Profile, aligned with Danger Zones
- [ ] `ChampionSelectCTA` is in the Quick Actions section (recent-left)
- [ ] Section heading reads "Quick actions" (both modes)
- [ ] Individual mode layout is unchanged (except section rename)
- [ ] `gamesToday` and `gamesThisWeek` are populated in `AccountSummary`
- [ ] All unit, integration, and E2E tests pass
- [ ] WCAG AA contrast and keyboard navigation verified
- [ ] No additional API round-trips — new data piggybacks on existing overview fetch

## Dependencies

### Internal Dependencies
- [ ] Existing `OverviewEndpoint` and `IOverviewStatsRepository` (extended, not replaced)
- [ ] Existing `participant_metrics` table (must be populated by match sync)
- [ ] `useWinRateColor()` composable (reused)
- [ ] `getChampionIconUrl()` utility (reused)
- [ ] `CombinedStats` in `OverviewResponse` (already exists, used as season fallback)
- [ ] `AnalysisLayout.vue` `#deep-analysis` slot (already supports arbitrary content)
- [ ] `getMatchActivity()` API call (moved from OverviewPage to SoloStatsPage)

### Migration Notes
- `MatchActivityHeatmap` removal from Overview is a breaking layout change — coordinate with Solo page addition in the same PR
- Existing E2E tests reference "Today at a glance" heading and `RankSnapshot` — update selectors
- `getMatchActivity()` API call moves from `OverviewPage.vue` to `SoloStatsPage.vue`
- Existing `SoloStatsPage.spec.js` tests must be updated to expect the heatmap in Zone 4
