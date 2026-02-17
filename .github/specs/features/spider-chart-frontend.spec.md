# Feature: Performance Radar Chart — Frontend

## Problem Statement
The Solo Dashboard occupies Zones 1–3 of the `AnalysisLayout` but Zone 4 ("deep-analysis") is empty. Players need a holistic performance overview that synthesizes the 6 trend charts into a single glanceable visualization showing strengths and weaknesses across multiple dimensions. The backend endpoint `GET /api/v2/solo/radar-chart/{userId}` (see [spider-chart-backend.spec.md](spider-chart-backend.spec.md)) provides the data.

## Proposed Solution
Build a `RadarChart.vue` component (Chart.js radar type via `vue-chartjs`) and integrate it into the Solo Dashboard's Zone 4 slot. The chart renders 6 axes representing performance dimensions (Laning, Farming, Combat, Vision, Objectives, Survivability) on a 0–100 scale with tooltips showing raw values.

## User Stories
### Primary User Story
As a solo player, I want to see a spider/radar chart of my performance profile so that I can instantly identify my strongest and weakest areas.

### Additional User Stories
- As a solo player, I want the radar chart to respect my queue and time range filters so that I see the profile for the context I care about
- As a solo player, I want to hover over each axis to see my actual raw stat value so that I understand what the score means
- As a solo player, I want to see a loading skeleton while the chart data loads so that the page doesn't jump around

## Requirements

### Functional Requirements
1. Render a 6-axis radar chart using `vue-chartjs` Radar component (Chart.js is already in dependencies)
2. Display normalized 0–100 values on each axis with labels (Laning, Farming, Combat, Vision, Objectives, Survivability)
3. Show tooltip on hover for each axis displaying: axis label, normalized score, raw value with unit (e.g., "Laning: 62.5 — Gold diff @15: +500")
4. Respect the existing `queueFilter` and `timeRange` filters from the Solo page — fetch new data when filters change
5. Show loading skeleton state while data is being fetched
6. Show empty state with message when no data is available (0 games analyzed)
7. Show the games analyzed count as context text below the chart

### Non-Functional Requirements
- **Performance**: Chart should render in < 100ms after data arrives
- **Accessibility**: Include `data-testid` attributes; chart should have `aria-label` describing the data
- **Compatibility**: Works on all modern browsers; responsive down to 768px

## Technical Approach

### Frontend Changes
**Framework**: Vue 3 + Composition API

**Components**:
- [ ] `client/src/components/solo/RadarChart.vue` — The radar chart component
- [ ] Update `client/src/views/SoloPage.vue` — Add Zone 4 deep-analysis slot with RadarChart
- [ ] Update `client/src/services/authApi.js` — Add `getRadarChart()` API function
- [ ] Update `client/src/utils/chartConfigs.js` — Add `radarChartConfig()` (optional, depends on complexity)

### Component Architecture

#### `RadarChart.vue`
**Location**: `client/src/components/solo/RadarChart.vue`

This is a standalone component (NOT a thin wrapper around `TrendLineChart.vue` — the radar is a different chart type). It uses `vue-chartjs`'s `Radar` component directly.

```vue
<template>
  <div class="radar-chart" data-testid="radar-chart">
    <!-- Loading state -->
    <div v-if="loading" class="loading-state" data-testid="radar-loading">
      <div class="skeleton skeleton-radar" />
    </div>

    <!-- Empty state -->
    <div v-else-if="!hasData" class="empty-state" data-testid="radar-empty">
      <p class="empty-text">No performance data available</p>
      <p class="empty-subtext">Play some games to see your performance profile</p>
    </div>

    <!-- Chart -->
    <div v-else class="chart-container">
      <Radar :data="chartData" :options="chartOptions" />
      <p class="games-context">Based on {{ gamesAnalyzed }} games</p>
    </div>
  </div>
</template>
```

**Props**:
| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `axes` | `Array` | `[]` | Array of `{ key, label, value, rawValue, rawUnit }` objects from API |
| `gamesAnalyzed` | `Number` | `0` | Total games used for this profile |
| `loading` | `Boolean` | `false` | Loading state |

**Computed**:
- `hasData` — `axes.length > 0 && gamesAnalyzed > 0`
- `chartData` — Chart.js data object with labels from `axes[].label` and dataset from `axes[].value`
- `chartOptions` — Chart.js radar options with scale 0–100, tooltip callbacks showing raw values

**Chart.js Configuration**:
```javascript
// chartData
{
  labels: ['Laning', 'Farming', 'Combat', 'Vision', 'Objectives', 'Survivability'],
  datasets: [{
    label: 'Performance',
    data: [62.5, 58.0, 64.2, 44.0, 55.3, 56.0],
    backgroundColor: 'rgba(109, 40, 217, 0.2)',  // Purple fill
    borderColor: '#6d28d9',                        // Purple border
    borderWidth: 2,
    pointBackgroundColor: '#6d28d9',
    pointBorderColor: '#ffffff',
    pointBorderWidth: 1,
    pointRadius: 4,
    pointHoverRadius: 6
  }]
}

// chartOptions
{
  responsive: true,
  maintainAspectRatio: true,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => {
          // Show: "Score: 62.5 — Gold diff @15: +500"
          const axis = axes[context.dataIndex]
          return `Score: ${axis.value} — ${axis.rawUnit}: ${axis.rawValue}`
        }
      }
    }
  },
  scales: {
    r: {
      min: 0,
      max: 100,
      ticks: { stepSize: 20, color: '#888888', backdropColor: 'transparent' },
      grid: { color: 'rgba(255, 255, 255, 0.1)' },
      angleLines: { color: 'rgba(255, 255, 255, 0.1)' },
      pointLabels: { color: '#ffffff', font: { size: 12 } }
    }
  }
}
```

### SoloPage.vue Integration

Add the radar chart to Zone 4 of the `AnalysisLayout`:

```vue
<!-- Zone 4: Deep Analysis -->
<template #deep-analysis>
  <div class="deep-analysis-grid">
    <BaseCard data-testid="radar-chart-card">
      <h3 class="section-title">Performance Profile</h3>
      <p class="section-subtitle">Your strengths and weaknesses across 6 dimensions</p>
      <RadarChart
        :axes="radarChartData?.axes ?? []"
        :games-analyzed="radarChartData?.gamesAnalyzed ?? 0"
        :loading="radarChartLoading"
      />
    </BaseCard>
  </div>
</template>
```

**New state in SoloPage.vue**:
```javascript
// Radar chart data
const radarChartData = ref(null)
const radarChartLoading = ref(false)

async function fetchRadarChart() {
  if (!authStore.userId) return
  radarChartLoading.value = true
  try {
    radarChartData.value = await getRadarChart(authStore.userId, queueFilter.value, timeRange.value)
  } catch (err) {
    console.error('Failed to fetch radar chart:', err)
    radarChartData.value = null
  } finally {
    radarChartLoading.value = false
  }
}
```

Add `fetchRadarChart()` to `fetchAllData()` `Promise.all` array. Add import for `RadarChart` and `getRadarChart`.

### API Service Function

**File**: `client/src/services/authApi.js`

```javascript
/**
 * Get radar chart performance profile for a user
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @returns {Promise<Object|null>} Radar chart data or null
 */
export async function getRadarChart(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') params.append('queueType', queueType)
  if (timeRange) params.append('timeRange', timeRange)

  const endpoint = `/solo/radar-chart/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })
  if (response.status === 404) return null
  return parseResponse(response, 'Failed to get radar chart')
}
```

### Styling

Follow the dark theme pattern used by existing solo components. Key CSS variables from the design system:
- Background: `var(--background-card)` / `var(--background-elevated)`
- Text: `var(--text)` / `var(--text-secondary)`
- Primary color: `#6d28d9` (purple)
- Green (good): `#22c55e`
- Red (bad): `#ef4444`

The Zone 4 layout should use a grid that can later accommodate the Danger Zones heatmap alongside the radar chart:
```css
.deep-analysis-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-lg);
}

@media (max-width: 768px) {
  .deep-analysis-grid {
    grid-template-columns: 1fr;
  }
}
```

## Testing Strategy

### Unit Tests (Vitest)
**File**: `client/test/unit/components/solo/RadarChart.spec.js`

Mock the Chart.js Radar component:
```javascript
vi.mock('vue-chartjs', () => ({
  Radar: {
    name: 'Radar',
    props: ['data', 'options'],
    template: '<div data-testid="mock-radar-chart"></div>'
  }
}))
```

Test cases:
- [ ] `renders radar chart when data is provided` — mount with axes prop, verify chart renders
- [ ] `shows loading state` — mount with `loading: true`, verify skeleton visible
- [ ] `shows empty state when no data` — mount with empty axes, verify empty state message
- [ ] `shows games analyzed count` — mount with data, verify "Based on X games" text
- [ ] `computes chart data correctly from axes prop` — verify labels and dataset values map correctly
- [ ] `all normalized values are within 0-100` — verify chartOptions scale.r.min/max

**File**: `client/test/unit/views/SoloPage.spec.js` (extend existing if present)
- [ ] `renders Zone 4 deep-analysis slot with radar chart` — verify radar chart card renders in the layout

### Integration Tests (Playwright)
**File**: Extend `client/e2e/solo-dashboard.spec.js`
- [ ] `radar chart loads on solo page` — navigate to solo page, verify `[data-testid="radar-chart"]` becomes visible
- [ ] `radar chart responds to filter changes` — change queue filter, verify chart re-renders

## Validation Criteria
Feature is considered complete when:
- [ ] Radar chart renders in Zone 4 with 6 labeled axes
- [ ] Values are plotted correctly on 0–100 scale
- [ ] Tooltips show raw values on hover
- [ ] Loading state displays skeleton
- [ ] Empty state shows message when no data
- [ ] Games analyzed count displays below chart
- [ ] Chart re-fetches when queue/time range filters change
- [ ] Unit tests pass
- [ ] Accessible: `data-testid` attributes present, `aria-label` on chart container

## Dependencies
### Internal Dependencies
- [ ] **Spider Chart Backend** ([spider-chart-backend.spec.md](spider-chart-backend.spec.md)) — must be implemented first to provide the API endpoint
- [ ] `AnalysisLayout.vue` — Zone 4 `#deep-analysis` slot (already exists, just unused)
- [ ] `BaseCard` component — card wrapper (already exists)
- [ ] `vue-chartjs` — Chart.js Vue wrapper (already in `package.json`)
- [ ] `chart.js` — Chart library (already in `package.json`)

### External Dependencies
- None (Chart.js Radar type is included in the core Chart.js library already used by the trend charts)

## References
- [Solo Page Graph Alternatives](../../../docs/solo-page-graph-alternatives.md) — Spider Chart section (UX analysis, precedent from Riot client)
- [Solo Page Feature Research](../../../docs/solo-page-feature-research.md) — Performance Radar Chart (academic evidence, radar chart theory)
- [UI/UX Spec](../ui-ux.spec.md) — Zone 4 layout, design system tokens
- [Component Spec Template](../component.spec.md) — Component structure patterns
