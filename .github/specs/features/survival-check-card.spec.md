# Feature: SurvivalCheckCard — Rank-Adaptive Death Insight

## Problem Statement
The Overview page has a placeholder `SurvivalCheckCard.vue` showing "Coming soon". The backend serves `SurvivalStats` with hardcoded death buckets (≤3 / 5+), which are meaningless for low-rank players — a Bronze player averaging 7.5 deaths/game has almost no games in the ≤3 bucket, producing noise rather than insight.

## Proposed Solution
Introduce rank-adaptive death thresholds: the backend resolves the player's current rank tier to rank-appropriate low/high death boundaries, parameterizes the SQL query with those thresholds, and returns them in the `SurvivalStats` response. The frontend replaces the placeholder with a single-insight card answering: *"Does dying less actually win me games?"*

## User Stories
### Primary User Story
As a solo player, I want to see how my deaths correlate with wins — calibrated to my rank — so I can decide whether playing safer is worth prioritizing.

## Requirements

### Functional Requirements
1. Backend computes death thresholds from the player's current rank tier (Iron → Diamond+) using a static lookup table.
2. Repository `GetSurvivalStatsAsync` accepts `lowDeathThreshold` and `highDeathThreshold` parameters instead of hardcoding values in SQL.
3. The API response includes `lowDeathThreshold` and `highDeathThreshold` fields so the frontend can display them without duplicating rank logic.
4. The card renders a single adaptive headline: motivational (low-death WR ≥ 55% and gap ≥ 15pp), warning (high-death WR ≤ 45% and gap ≥ 15pp), neutral (gap < 15pp), or empty (no data).
5. The left border is color-tinted based on the player's avg deaths vs rank thresholds (green/red/purple).
6. `deathsBefore10Pct` is removed from Overview scope; it will live on the Solo page.

### Non-Functional Requirements
- **Performance**: No new database tables or external API calls; changes are purely computational.
- **Security**: Parameterized SQL only — threshold values passed as parameters, never concatenated.
- **Accessibility**: `aria-label="Death insight: win rate correlation with deaths"` on card; text carries meaning independently of border color.

## Technical Approach

### Backend Changes
**Language**: C#

**Components**:
- [x] Domain logic: `server/Mongoose.Api/Core/DeathThresholds.cs` (NEW)
- [x] DTO: `server/Mongoose.Api/Application/DTOs/Overview/OverviewDto.cs` (MODIFY — replace `SurvivalStats` record)
- [x] Query model: `server/Mongoose.Api/Core/QueryModels/OverviewQueryModels.cs` (MODIFY — replace `SurvivalStatsData`)
- [x] Repository interface: `server/Mongoose.Api/Core/Interfaces/Analytics/IOverviewStatsRepository.cs` (MODIFY)
- [x] Repository impl: `server/Mongoose.Api/Infrastructure/Database/Repositories/OverviewStatsRepository.cs` (MODIFY)
- [x] Endpoint: `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs` (MODIFY)

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [x] `client/src/components/overview/SurvivalCheckCard.vue` (REPLACE placeholder)
- [x] `client/test/unit/DeathInsightCard.spec.js` (NEW)

### Database Changes
None — no schema changes. Only SQL query parameterization.

### API Contracts

#### `GET /api/v2/overview/{userId}` — `survivalStats` field

**Before** (current):
```json
{
  "survivalStats": {
    "avgDeathsPerGame": 4.5,
    "deathsBefore10Pct": 0.3,
    "winRateAtOrBelow3Deaths": 0.65,
    "winRateAbove5Deaths": 0.2,
    "gamesAtOrBelow3Deaths": 8,
    "gamesAbove5Deaths": 5,
    "totalGames": 20
  }
}
```

**After** (new):
```json
{
  "survivalStats": {
    "avgDeathsPerGame": 4.5,
    "winRateLowDeaths": 0.65,
    "winRateHighDeaths": 0.2,
    "gamesLowDeaths": 12,
    "gamesHighDeaths": 3,
    "lowDeathThreshold": 4,
    "highDeathThreshold": 6,
    "totalGames": 20
  }
}
```

**Breaking changes** (internal API — frontend is sole consumer, ships atomically):
- Removed: `deathsBefore10Pct`, `winRateAtOrBelow3Deaths`, `winRateAbove5Deaths`, `gamesAtOrBelow3Deaths`, `gamesAbove5Deaths`
- Added: `winRateLowDeaths`, `winRateHighDeaths`, `gamesLowDeaths`, `gamesHighDeaths`, `lowDeathThreshold`, `highDeathThreshold`

## UI/UX Requirements

All views follow [UI/UX Spec](../ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

### SurvivalCheckCard (DeathInsightCard)

**Layout**: Right column of the "At a glance" row on OverviewPage, alongside TodaySessionCard. Both cards must use `height: 100%` so they fill the row height set by whichever card is taller. The `section-row` uses `align-items: stretch` on desktop.

**Structure** (motivational state example for a Bronze player — thresholds ≤5 / 8+ from backend):
```
┌─ [3px left border: success/error/default] ────────────────────┐
│  DEATH INSIGHT                                                 │
│                                                                │
│  72%  win rate when you die ≤5 times                           │
│  (40px, green)  (14px, text-secondary)                         │
│                                                                │
│  vs  41%  when you die 8+ times                                │
│  (muted)  (18px, red)  (14px, text-secondary)                  │
│                                                                │
│  ──────────────────────────────────────                        │
│  Your avg: 6.8 deaths/game  ·  15 games                        │
│  (12px, text-secondary)                                        │
└────────────────────────────────────────────────────────────────┘
```

**Adaptive headline logic**:

| Condition | Headline | Tone |
|-----------|----------|------|
| `winRateLowDeaths ≥ 0.55` AND gap ≥ 0.15 | "{WR}% win rate when you die ≤{low} times" | Motivational (success border) |
| `winRateHighDeaths ≤ 0.45` AND gap ≥ 0.15 | "You lose {WR}% when dying {high}+ times" | Warning (error border) |
| Gap < 0.15 or insufficient data | "Avg {N} deaths/game across {total} games" | Neutral (default border) |
| `survivalStats` is null | "Play a few games to unlock death insights" | Empty state |

Where `gap = winRateLowDeaths - winRateHighDeaths`.

**Performance-tinted left border**:

| Condition | Border | Token |
|-----------|--------|-------|
| Avg deaths ≤ `lowDeathThreshold` | Green | `var(--color-success-border)` |
| Avg deaths ≥ `highDeathThreshold` | Red | `var(--color-error-border)` |
| Between | Purple | `var(--color-border)` |

**Styling tokens**:
- Card base: `background: var(--color-surface)`, `border-radius: var(--radius-lg)`, `backdrop-filter: blur(10px)`
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Hero WR: `font-size-2xl` (40px), `font-weight-bold`, color from `useWinRateColor()`
- Contrast WR: `font-size-lg` (18px), color from `useWinRateColor()`
- Footer: `font-size-xs`, `text-text-secondary`
- Hover: `transform: translateY(-2px)` + `box-shadow: var(--shadow-md)` + `transition: all 0.2s ease`

## Testing Strategy

### Backend Integration Tests (`server/Mongoose.Api.Tests/OverviewEndpointTests.cs`)

Modify existing tests:
- Update deserialization record to match new `SurvivalStats` field names
- Update `FakeOverviewStatsRepository.GetSurvivalStatsAsync` signature to accept threshold params
- Update `SurvivalStatsData` construction in fake

New tests:
1. Response `survivalStats` includes `lowDeathThreshold` and `highDeathThreshold`
2. Gold-ranked player gets thresholds `(4, 6)`
3. Unranked player gets default thresholds `(4, 7)`
4. Response JSON does NOT contain `deathsBefore10Pct`
5. Response contains `winRateLowDeaths` / `winRateHighDeaths` (not old names)

### Frontend Unit Tests (`client/test/unit/DeathInsightCard.spec.js`)

1. Renders motivational headline when `winRateLowDeaths ≥ 0.55` and gap ≥ 0.15
2. Renders warning headline when `winRateHighDeaths ≤ 0.45` and gap ≥ 0.15
3. Falls back to neutral when gap < 0.15
4. Shows empty state when `survivalStats` is null
5. Shows loading skeleton when `loading` is true
6. Footer shows avg deaths and total games
7. Displays rank-adaptive thresholds in headline text

## DDD Alignment

- `DeathThresholds` lives in Core — pure domain knowledge about LoL rank tiers, no infrastructure dependencies
- Ubiquitous language: "low deaths" / "high deaths" are rank-relative, matching how players think
- `deathsBefore10Pct` is scoped out to Solo bounded context (no duplication across pages)
- No new infrastructure, no new aggregate roots — change fits within existing Overview context

## Rank-Adaptive Threshold Table

| Rank Tier | Low (≤) | High (≥) |
|-----------|---------|---------|
| Iron | 6 | 9 |
| Bronze | 5 | 8 |
| Silver | 5 | 7 |
| Gold | 4 | 6 |
| Platinum | 4 | 6 |
| Emerald | 3 | 5 |
| Diamond+ | 3 | 5 |
| Unranked/null | 4 | 7 |
