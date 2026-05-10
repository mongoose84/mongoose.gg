# Feature: Damage Per Minute (DPM) Trend Chart

> **Purpose**: Add a Damage Per Minute trend chart to the Solo Dashboard to help players visualize combat output efficiency across game phases and correlate it with win rates.

## Problem Statement

Players lack visibility into their **damage output consistency** and **scaling efficiency** across game phases. Currently, the Solo Dashboard shows:
- Win rate (outcome)
- Deaths (deficit prevention)
- Vision score (preparation)
- CS/min (resource gathering)

But **combat output** — the ultimate conversion metric — is missing. Players cannot easily answer:
- "Am I outputting enough damage for my role?"
- "Do I scale well mid-to-late game?"
- "Is my high damage correlated with wins?"

This gap forces players to manually inspect match details (external tool friction), missing actionable trend insights.

## Proposed Solution

Add a **Damage Per Minute (DPM) Trend Chart** to the Solo Dashboard (Zone 3: Trend Charts, after Vision Score) that:
1. Displays rolling 20-game DPM trends **segmented by phase** (Early: 0–10min, Mid: 10–20min, Late: 20+min)
2. Shows **win vs. loss correlation** via optional split view
3. Provides **phase breakdown cards** showing scaling health, consistency, and role-based benchmarks
4. Supports multi-account and queue filtering like other trend metrics

## User Stories

### Primary User Story
As a **solo ladder climber**, I want to **track my damage output trends by game phase** so that **I can identify if weak damage output (not macro play) is holding back my climb**.

### Additional User Stories
- As a **role specialist** (ADC/Mid/Support), I want to **compare my DPM against role-based benchmarks** so that **I know if I'm outputting what's expected for my position**.
- As a **player reviewing my recent games**, I want to **see if my damage correlates with wins** so that **I can determine if damage is my primary win condition or if I need to focus on macro**.
- As a **multi-account player**, I want to **overlay DPM trends across accounts** so that **I can see if one account has different damage patterns**.

## Requirements

### Functional Requirements

1. **Fetch DPM trend data** from the backend grouped by rolling 20-game windows
2. **Calculate DPM by phase** (Early/Mid/Late) from match timeline checkpoints and total damage
3. **Compute role-based benchmarks** (e.g., ADC avg 55 DPM mid, Support avg 12 DPM mid)
4. **Support queue and time range filters** (Solo, Flex; 30, 90, 365, all games)
5. **Display three visualizations**:
   - Default: Tri-phase stacked area trend (rolling average)
   - Phase Split: Three separate trend lines (Early/Mid/Late)
   - Win vs. Loss: Red line (losses) vs. Green line (wins)
6. **Show efficiency cards** below chart: Consistency, Scaling Ratio, Win Correlation
7. **Provide hover tooltips** with: Game number, Win/Loss, per-phase DPM, total damage, game duration
8. **Support multi-account overlay** (show multiple lines when user toggles accounts)

### Non-Functional Requirements

- **Performance**: API endpoint returns within 200ms (aggregated data over 50 games)
- **Security**: Endpoint requires authentication; returns only user's own PUUID data
- **Compatibility**: Chart renders on desktop and tablet (responsive for 768px+ width)
- **Accessibility**: 
  - Chart has `role="img"` with descriptive `aria-label`
  - Tooltip accessible via keyboard (Tab to chart, Arrow keys navigate points)
  - Color-blind friendly: Use patterns/icons in addition to colors (phase names in legend)
- **Data Accuracy**: DPM calculated as `totalDamageDealt / (gameDurationSeconds / 60)`

## Technical Approach

### Backend Changes

**Language**: C#  
**Architecture**: Clean Architecture (Core → Application → Infrastructure)

**Components**:

- [ ] **Data Models** (`Core/`):
  - `DpmTrendPoint` — single game's DPM (early, mid, late phases)
  - `DpmSummary` — rolling averages, role benchmarks, scaling metrics

- [ ] **Repository** (`Infrastructure/Repositories/`):
  - `DpmTrendRepository.cs` — queries `participants`, `matches`, `participant_checkpoints`, `participant_metrics` tables

- [ ] **Query Service** (`Application/Services/`):
  - `DpmTrendService.cs` — calculates phase-based DPM, applies filters, computes benchmarks

- [ ] **API Endpoint** (`Application/Endpoints/Trends/`):
  - `DpmTrendEndpoint.cs` — HTTP GET `/api/trends/dpm`

**Database**:
- No new tables (uses existing: `participants`, `matches`, `participant_checkpoints`, `participant_metrics`)
- Existing `damage_dealt` and `game_duration_sec` columns are sufficient

### Frontend Changes

**Framework**: Vue 3 + Pinia + Tailwind

**Components**:

- [ ] **UI Component** (`client/src/components/Charts/`):
  - `DpmChart.vue` — Renders tri-phase stacked area chart (uses Chart.js or similar)
  - `DpmEfficiencyCards.vue` — Three metric cards (Consistency, Scaling Ratio, Win Correlation)

- [ ] **Composable** (`client/src/composables/`):
  - `useSoloDpmData.js` — Fetches trend data, manages loading state, handles filters

- [ ] **API Service** (`client/src/services/`):
  - Add method to existing analytics service: `fetchDpmTrend(queueId, timeRange, accounts)`

- [ ] **Page Integration** (`client/src/views/`):
  - Update `SoloStatsPage.vue` — Insert `DpmTrendCard` after Vision Score chart

- [ ] **Styles** (`client/src/style.css` or component-scoped):
  - Phase layer colors: Early (#3B82F6), Mid (#FBBF24), Late (#EF4444)
  - Reference line style: Dashed, `stroke-dasharray="5,5"`, opacity 0.5

### API Contracts

#### Endpoint: Get DPM Trend Data

```
GET /api/trends/dpm?queueId=420&timeRange=90&accounts=puuid1,puuid2
```

**Query Parameters**:
- `queueId` (int): Queue identifier (420 = Solo/Duo, 440 = Flex) — optional, defaults to primary
- `timeRange` (int): Days (30, 90, 365, or 0 for all) — optional, defaults to 90
- `accounts` (string): Comma-separated PUUIDs — optional, defaults to primary account

**Response** (200 OK):
```json
{
  "trendData": [
    {
      "gameNumber": 1,
      "win": true,
      "dpmEarly": 15.2,
      "dpmMid": 42.1,
      "dpmLate": 78.5,
      "totalDamage": 8247,
      "gameDuration": 1950,
      "championName": "Ahri",
      "role": "middle",
      "timestamp": "2026-05-09T14:30:00Z"
    }
  ],
  "summary": {
    "avgDpmOverall": 45.3,
    "avgDpmEarly": 15.1,
    "avgDpmMid": 41.8,
    "avgDpmLate": 76.2,
    "dpmStdDev": 8.4,
    "scalingRatio": 5.04,
    "winRate": 0.68,
    "winCorrelation": 0.67,
    "roleMedianEarly": 14.2,
    "roleMedianMid": 38.9,
    "roleMedianLate": 72.1,
    "phaseDefinitions": {
      "early": { "min": 0, "max": 600 },
      "mid": { "min": 600, "max": 1200 },
      "late": { "min": 1200, "max": 999999 }
    }
  },
  "gamesAnalyzed": 47
}
```

**Error Responses**:
- 401 Unauthorized → Invalid or missing auth token
- 403 Forbidden → Requested PUUID not linked to user
- 400 Bad Request → Invalid `queueId` or `timeRange`
- 500 Server Error → Database or calculation failure

---

## UI/UX Requirements

All components follow [UI/UX Spec](./ui-ux.spec.md) design system.

### DPM Trend Card

**Layout**: Inserted into Solo Dashboard Zone 3 (Trend Charts), after Vision Score, before Gold at 15.

**Structure**:
```
┌─────────────────────────────────────────────────┐
│ Damage Per Minute                               │
│ Combat output by phase: early, mid, late        │ [Expand ⬈]
├─────────────────────────────────────────────────┤
│                                                 │
│  [Tri-phase stacked area chart]                 │
│  X: Games (1–47)                               │
│  Y: DPM (0–100+)                                │
│  ┌─────────────────────────────────────────┐   │
│  │ ░░░░░ Late (20+min)         [red]        │   │
│  │ ▒▒▒▒▒ Mid (10–20min)       [gold]       │   │
│  │ ▓▓▓▓▓ Early (0–10min)      [blue]       │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
├─────────────────────────────────────────────────┤
│ [View: Overall] [View: Phases] [View: Win/Loss] │
├─────────────────────────────────────────────────┤
│ Consistency: 8.4 DPM StdDev  │ Scaling: 5.0x   │ Win Corr: 67% │
└─────────────────────────────────────────────────┘
```

**Components**:
- **Title**: "Damage Per Minute" (heading-sm, `text-gray-900`)
- **Subtitle**: "Combat output by phase: early, mid, late" (text-xs, `text-gray-500`)
- **Expand Button**: Top-right, uses existing `@toggle-expand` handler
- **Chart Container**: `<DpmChart>` — receives props:
  - `:data="dpmTrendData"` — array of `DpmTrendPoint`
  - `:phase-breakdown="dpmPhaseBreakdown"` — { early, mid, late, ratio }
  - `:win-rate-correlation="dpmWinRateCorrelation"` — 0–1
  - `:chart-mode="chartMode"` — "overall" | "phases" | "winloss"
  - `:accounts="chartAccounts"` — for multi-account overlay
  - `:loading="dpmLoading"` — boolean
- **View Toggle Buttons**: Mutually exclusive buttons, style with `btn-secondary` variant
  - "Overall" (default)
  - "Phases"
  - "Win vs. Loss"
- **Efficiency Cards Row**: Below toggle
  - Card 1: `text-center`
    - Label: "Consistency" (text-xs, `text-gray-600`)
    - Value: "8.4 DPM" (text-lg, `font-semibold`, `text-gray-900`)
    - Help icon (hover: "Lower is better — shows steady output")
  - Card 2: Same pattern
    - Label: "Scaling Ratio"
    - Value: "5.0x" (Late DPM ÷ Early DPM)
    - Help icon (hover: "How much you scale from early to late game")
  - Card 3: Same pattern
    - Label: "Win Correlation"
    - Value: "67%" (games with DPM > median win % ÷ total wins)
    - Help icon (hover: "% of wins when DPM exceeded your median")

**Behavior**:
- **Loading**: Show skeleton of chart with spinner overlay; efficiency cards show "–"
- **Data fetched**: Chart animates in (fade-in 300ms); cards populate
- **View toggle**: Chart re-renders instantly (no refetch)
- **Multi-account overlay**: If user selects multiple accounts, chart shows multiple lines (one per account, different colors from palette)
- **Hover tooltip**: Sticky on desktop, tap-to-show on mobile
  - Content: Game #N, W/L badge, DPM by phase, Total Dmg, Game Duration

**Accessibility**:
- Chart `role="img"` with `aria-label="Damage Per Minute trend over last 47 games, showing combat output by phase"`
- Buttons labeled: `aria-label="View overall DPM trend"`, etc.
- Efficiency cards wrapped in `<dl>` with `<dt>` (label) + `<dd>` (value)
- Help icons trigger tooltip on Enter/Space
- Keyboard nav: Tab through view buttons, use Arrow Left/Right to navigate chart points

**Responsive**:
- Min-width: 768px (does not display on mobile; show "View on desktop" message at 767px)
- Chart scales to container width, maintains 16:9 aspect ratio

---

## Testing Strategy

### Unit Tests

**Backend** (xUnit, `server/Mongoose.Api.Tests/`):

- [ ] `DpmTrendServiceTests.cs`
  - ✓ Calculates early/mid/late DPM correctly
  - ✓ Applies queue filter (Solo only, Flex only)
  - ✓ Applies time range filter (last 30, 90, 365 days)
  - ✓ Returns empty array for user with no matches
  - ✓ Handles multi-account PUUID list

- [ ] `DpmTrendRepositoryTests.cs`
  - ✓ Queries participants + matches correctly
  - ✓ Calculates DPM from damage_dealt and game_duration_sec
  - ✓ Filters by queue_id
  - ✓ Filters by date range
  - ✓ Handles missing damage_dealt (treats as 0)

**Frontend** (Vitest, `client/test/unit/`):

- [ ] `DpmChart.component.spec.js`
  - ✓ Renders tri-phase stacked area with correct layer order
  - ✓ Displays efficiency cards with correct values
  - ✓ Toggles between Overall/Phases/Win-Loss views
  - ✓ Shows loading skeleton on `loading=true`
  - ✓ Handles empty data gracefully

- [ ] `useSoloDpmData.spec.js`
  - ✓ Fetches data from API
  - ✓ Applies queue and time range filters
  - ✓ Handles API errors (shows error message)
  - ✓ Handles network timeout

### Integration Tests

**Backend** (xUnit, `DpmTrendEndpointTests.cs`):

- [ ] GET `/api/trends/dpm` without auth → 401 Unauthorized
- [ ] GET `/api/trends/dpm?accounts=unlinked-puuid` → 403 Forbidden
- [ ] GET `/api/trends/dpm?queueId=999` → 400 Bad Request
- [ ] GET `/api/trends/dpm?timeRange=invalid` → 400 Bad Request
- [ ] GET `/api/trends/dpm` with valid auth → 200 OK + correct data shape
- [ ] GET `/api/trends/dpm?timeRange=30` → only last 30 days
- [ ] GET `/api/trends/dpm?accounts=puuid1,puuid2` → data for both accounts

**Frontend** (Playwright E2E, `client/e2e/solo-dashboard.spec.js`):

- [ ] Navigate to Solo Dashboard → DPM chart loads within 3 seconds
- [ ] Click "Phases" view button → chart updates
- [ ] Hover over chart point → tooltip shows DPM breakdown
- [ ] Resize viewport to 767px → "View on desktop" message appears
- [ ] Change queue filter → DPM chart updates
- [ ] Change time range → DPM chart updates

### Manual Testing Scenarios

1. **Happy Path**: User with 50+ ranked games loads Solo Dashboard
   - ✓ DPM chart displays with populated data
   - ✓ Efficiency cards show realistic values
   - ✓ View toggles work smoothly

2. **New Player**: User with 3 ranked games
   - ✓ Chart displays (but with sparse data)
   - ✓ No crash or error message

3. **Role Variance**: ADC plays 30 games, then switches to Support
   - ✓ DPM drops significantly (expected)
   - ✓ Role-based benchmarks update correctly

4. **Multi-Account**: User with 2 linked accounts switches between them
   - ✓ DPM data different per account
   - ✓ Overlay mode shows both lines

5. **Edge Case**: User with 10 games, all losses, all low DPM
   - ✓ Chart renders (red area stays low)
   - ✓ Win Correlation shows 0%
   - ✓ No division-by-zero errors

## Validation Criteria

Feature is considered complete when:

- [x] All functional requirements are implemented
- [ ] Unit tests pass with >85% code coverage (backend: DpmTrendService, DpmTrendRepository; frontend: DpmChart, useSoloDpmData)
- [ ] Integration tests pass (7 backend scenarios, 5 E2E scenarios)
- [ ] API documentation updated in [architecture.spec.md](./architecture.spec.md)
- [ ] Solo Dashboard loads within 2.5s with DPM data included
- [ ] Tooltip accessible via keyboard (Tab + Arrow keys)
- [ ] Chart colors pass WCAG AA contrast ratio
- [ ] Code review by tech lead approved
- [ ] No regressions in existing dashboard tests
- [ ] Response time <200ms under load (50 games × 3 phases × 4 accounts)

## Dependencies

### Internal Dependencies
- **Existing**: `TrendChartCard.vue` (parent component, reused pattern)
- **Existing**: `useChartDisplayMode.js` (chart view mode logic)
- **Existing**: `useSoloDashboardData.js` (auth, filter state)
- **Existing**: `MatchesRepository` (match + participant queries)

### External Dependencies
- **Chart.js** v4+ (already in `package.json` for other charts)
- **.NET 10** (for backend, already in use)
- **xUnit** (test framework, already in use)
- **Vitest** (test framework, already in use)

### Risk & Mitigation
| Risk | Mitigation |
|------|-----------|
| DPM calculation off by 1–2% due to rounding | Use fixed-point arithmetic (2 decimal places); add unit tests |
| Slow API on large date ranges (365 days) | Pre-aggregate rolling averages server-side; add caching |
| Chart flickers on filter change | Implement loading skeleton; debounce filter updates by 200ms |
| Role benchmarks outdated mid-patch | Update benchmarks monthly from role-filtered aggregate stats |

---

## Files Affected Summary

### New Files
- `server/Mongoose.Api/Core/Models/DpmTrendPoint.cs`
- `server/Mongoose.Api/Core/Models/DpmSummary.cs`
- `server/Mongoose.Api/Application/Services/DpmTrendService.cs`
- `server/Mongoose.Api/Infrastructure/Repositories/DpmTrendRepository.cs`
- `server/Mongoose.Api/Application/Endpoints/Trends/DpmTrendEndpoint.cs`
- `server/Mongoose.Api.Tests/DpmTrendServiceTests.cs`
- `server/Mongoose.Api.Tests/DpmTrendRepositoryTests.cs`
- `server/Mongoose.Api.Tests/DpmTrendEndpointTests.cs`
- `client/src/components/Charts/DpmChart.vue`
- `client/src/components/Charts/DpmEfficiencyCards.vue`
- `client/src/composables/useSoloDpmData.js`
- `client/test/unit/DpmChart.spec.js`
- `client/test/unit/useSoloDpmData.spec.js`
- `client/e2e/solo-dashboard.spec.js` (update with DPM scenarios)

### Modified Files
- `client/src/views/SoloStatsPage.vue` (insert DPM card after Vision Score)
- `client/src/services/analyticsService.js` (add `fetchDpmTrend` method)
- `specs/architecture.spec.md` (add DPM endpoint contract)
- `specs/ui-ux.spec.md` (add DPM color tokens to phase layer definition)

---

## Implementation Checklist

### Phase 1: Backend
- [ ] Create data models (DpmTrendPoint, DpmSummary)
- [ ] Implement DpmTrendRepository (queries)
- [ ] Implement DpmTrendService (logic + benchmarks)
- [ ] Create DpmTrendEndpoint
- [ ] Write unit + integration tests
- [ ] Update architecture.spec.md

### Phase 2: Frontend
- [ ] Create DpmChart component
- [ ] Create DpmEfficiencyCards component
- [ ] Create useSoloDpmData composable
- [ ] Integrate into SoloStatsPage
- [ ] Write component + composable unit tests
- [ ] Add E2E test scenarios

### Phase 3: QA & Review
- [ ] Cross-browser testing (Chrome, Firefox, Safari)
- [ ] Mobile responsiveness (768px breakpoint)
- [ ] Accessibility audit (keyboard, screen reader)
- [ ] Performance testing (API response time, chart render time)
- [ ] Code review
- [ ] Merge to main
