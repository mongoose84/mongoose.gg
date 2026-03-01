# Feature: MA-08 — Solo Page: Overall Mode

## Problem Statement
The Solo Dashboard currently shows trend charts and summary stats for a single account. When a user is in "Overall" mode, they want to see their combined performance across all accounts — aggregate win rate, KDA, and trend charts that include games from all linked accounts.

## Proposed Solution
When the active account is "Overall", the Solo Dashboard fetches data with `?account=all`. Summary stats show aggregated values. Trend charts display data from all accounts following the user's chart display preference (merged single line or per-account colored lines), configurable in Settings (MA-09).

## User Stories
### Primary User Story
As a player with multiple accounts, I want to see my overall improvement trends across all my accounts so I get the full picture of my progression.

### Additional User Stories
- As a player, I want to toggle between seeing a single merged trend line and per-account colored lines
- As a player, I want the summary stats to clearly indicate they represent all accounts
- As a player, I want queue and time range filters to apply across all accounts

## Requirements

### Functional Requirements
1. When `activeAccountPuuid === 'overall'`, Solo API calls use `?account=all`
2. `SummaryStatsCard` shows aggregate stats with a label "Across N accounts"
3. `SummaryStatsCard` rank display shows all ranks stacked (not averaged)
4. Trend charts render data following the chart display mode setting:
   - **Merged** (default): Single trend line, games interleaved chronologically
   - **Per-Account Lines**: Separate colored lines per account on same chart
5. Chart tooltips in merged mode show which account the data point came from
6. Chart legend in per-account mode shows account name + color
7. Queue filter and time range filter apply across all accounts
8. When viewing a specific account, Solo page works exactly as today

### Non-Functional Requirements
- **Performance**: Single API call per chart (not one per account). Server returns interleaved or separated data based on needs.
- **Data integrity**: KDA and win rates are computed server-side as weighted aggregates (not averages of averages)
- **Charts**: Chart.js datasets support multiple datasets natively — per-account lines are N datasets on one chart

## Technical Approach

### Backend Changes
Already handled by MA-02. Solo endpoints with `?account=all` return aggregated stats and interleaved trend data.

Additional consideration for per-account chart mode:
- [ ] Trend endpoints should include `puuid` field on each data point so the frontend can split into per-account datasets when needed
- [ ] Alternatively, add `?chartMode=merged|per-account` parameter to return data pre-grouped (prefer: include puuid on each point, let frontend group)

### Frontend Changes
**Framework**: Vue

#### Modified View: `SoloStatsPage.vue`
- [ ] API calls include `?account=` parameter (from MA-03 state)
- [ ] Watch `activeAccountPuuid` to re-fetch all data on account switch
- [ ] Pass chart display mode preference to chart components

#### Modified Component: `SummaryStatsCard.vue`
- [ ] Add `accountCount` prop — when > 1, show "Across N accounts" label
- [ ] Rank display: when multiple ranks provided, show all in a stacked list instead of single rank
- [ ] Add `ranks` prop (array of `{ gameName, rank, lp }`) for Overall mode

#### Modified Chart Components (all trend charts)
- [ ] Accept `chartMode` prop: `'merged'` | `'per-account'`
- [ ] Accept `accounts` prop: array of `{ puuid, gameName, color }` for per-account legend
- [ ] In merged mode: single dataset, tooltip shows account name from data point's `accountGameName`
- [ ] In per-account mode: split data points by PUUID, create one Chart.js dataset per account with assigned color
- [ ] Read chart mode from a composable or injected preference (from MA-09)

#### New Composable: `useChartDisplayMode()`
Location: `client/src/composables/useChartDisplayMode.js`
- Reads chart display preference from `localStorage` (key: `mongoose_chart_mode`)
- Returns reactive `chartMode` ref
- Default: `'merged'`
- Used by MA-09 Settings to set, used by chart components to read

### Database Changes
None.

## UI/UX Requirements

### Summary Stats in Overall Mode

**Structure**:
```
┌───────────────────────────────────────────────────────────────┐
│  Games Played        Win Rate           KDA                   │
│  152                 54.6%              3.2                   │
│  Across 3 accounts   ↑ vs overall       4.8 / 3.1 / 5.2     │
│                                                               │
│  Ranks: Diamond II (FakerMain) · Plat I (FakerSmurf) · G2   │
└───────────────────────────────────────────────────────────────┘
```

**Styling**:
- "Across N accounts" label: `text-3xs text-text-secondary mt-xs`
- Stacked ranks: `flex gap-sm flex-wrap`, each rank as `text-xs px-2 py-0.5 rounded-sm bg-background-elevated text-text-secondary`
- Highest rank highlighted: `border border-primary-soft`

### Merged Chart Mode
- Single line, same color as today
- Tooltip includes: value, game date, account name (e.g., "54.2% — FakerMain · Mar 1")
- No legend needed (one line)

### Per-Account Chart Mode
- N colored lines, one per account
- Color assignment: use a predefined palette (purple, blue, green, amber, red, etc.)
- Legend: `flex gap-md mb-sm`, each entry: colored dot + account name
- Tooltip: standard per-dataset tooltip (Chart.js handles this)
- Lines have different dash patterns if > 3 accounts (colorblind friendliness)

### Chart Mode Indicator (on chart card)
Small toggle or label indicating current mode, or defer to Settings (MA-09).
Recommend: no inline toggle — mode set globally in Settings, displayed consistently across all charts.

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `SummaryStatsCard` shows "Across N accounts" when `accountCount > 1`
- [ ] `SummaryStatsCard` shows stacked ranks when `ranks` array provided
- [ ] Chart components render single dataset in merged mode
- [ ] Chart components render N datasets in per-account mode
- [ ] `useChartDisplayMode` returns correct default and responds to changes
- [ ] Tooltip in merged mode includes account name

### Integration Tests
- [ ] Solo endpoints with `?account=all` return aggregate stats
- [ ] Trend data points include `accountGameName` for tooltip display

### Manual Testing
1. Overall mode — summary shows aggregate stats from all accounts
2. Overall mode — trend charts show merged line with per-point account tooltips
3. Switch chart mode in Settings — charts update to per-account lines
4. Switch to specific account — solo page shows that account only
5. Queue/time filters work in Overall mode

## Dependencies
### Internal Dependencies
- [ ] MA-02 (backend returns aggregated stats and tagged trend data)
- [ ] MA-03 (active account state drives `?account=` param)
- [ ] MA-09 (chart display mode preference — can ship with hardcoded "merged" default before MA-09)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Merged chart is noisy with many accounts | Medium | Medium | Data is chronological regardless; merged line smooths naturally with rolling averages |
| Per-account lines are hard to read with > 3 accounts | Medium | Low | Use dash patterns + distinct colors; most users have 2-3 accounts |
| Aggregated KDA is misleading (different rank difficulties) | Low | Medium | Label clearly "Across accounts"; individual account view is one click away |

## Open Questions
- [ ] Should "Across N accounts" show a breakdown tooltip on hover (e.g., per-account win rates)? Recommend: nice-to-have for v2, not required for initial launch.
