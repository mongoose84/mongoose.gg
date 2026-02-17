# Feature: Danger Zones Death Heatmap — Frontend

## Problem Statement
Death position data from the backend (see [danger-zones-backend.spec.md](danger-zones-backend.spec.md)) needs a spatial visualization that overlays death frequency on a Summoner's Rift minimap. Players need to visually identify "danger zones" — map areas where they die repeatedly — so they can adjust their pathing, warding, and positioning. Text-based or chart-based death data cannot convey spatial patterns.

## Proposed Solution
Build a `DangerZonesMap.vue` component that renders an HTML5 Canvas heatmap over a Summoner's Rift minimap image. Death positions from the API are plotted as heat points using a lightweight heatmap library (`simpleheat`, ~3KB). The component includes phase filter controls (Early/Mid/Late/All) and a complementary phase bar showing death count per time bucket. Integrate into the Solo page Zone 4 alongside the Radar Chart.

## User Stories
### Primary User Story
As a solo player, I want to see a heatmap of where I die on the Summoner's Rift map so that I can identify dangerous areas and change my behavior.

### Additional User Stories
- As a solo player, I want to filter the heatmap by game phase (early/mid/late) so that I can distinguish laning deaths from teamfight deaths
- As a solo player, I want to see a summary bar showing death counts per phase so that I understand my temporal death distribution
- As a solo player, I want the heatmap to respect my queue and time range filters so that I see relevant data
- As a solo player, I want to see how many matches contributed to the heatmap so that I know if the data is meaningful

## Requirements

### Functional Requirements
1. Render a Summoner's Rift minimap as the background (static image asset)
2. Overlay a canvas-based heatmap of death positions using Gaussian kernel density estimation
3. Normalize Riot API coordinates (0–15000 range) to minimap image pixel space
4. Provide phase filter buttons: All, Early (0–10), Mid (10–20), Late (20–30), Very Late (30+)
5. Show a phase summary bar with death counts per phase (already returned by API)
6. Display "matches analyzed" and "total deaths" as context text
7. Show loading skeleton while data fetches
8. Show empty state when no death position data exists (e.g., new account, no matches synced since feature shipped)
9. Respect existing queue and time range filters from the Solo page
10. Handle blue/red side normalization (optional v1 enhancement — not required for initial release, coordinates are absolute)

### Non-Functional Requirements
- **Performance**: Heatmap should render in < 200ms for up to 1000 death points
- **Bundle size**: Use `simpleheat` (~3KB minified) instead of heavier alternatives. Do NOT import a full WebGL heatmap library.
- **Accessibility**: Include `data-testid` attributes; `aria-label` on the map container; phase filter buttons must be keyboard-navigable
- **Responsive**: Map scales down on smaller screens preserving aspect ratio

## Technical Approach

### Frontend Changes
**Framework**: Vue 3 + Composition API

**Components**:
- [ ] `client/src/components/solo/DangerZonesMap.vue` — The heatmap component
- [ ] Update `client/src/views/SoloPage.vue` — Add DangerZonesMap to Zone 4 deep-analysis slot alongside RadarChart
- [ ] Update `client/src/services/authApi.js` — Add `getDeathPositions()` API function
- [ ] Add minimap asset: `client/public/assets/images/summoners-rift-minimap.png`

### External Dependency
- [ ] Install `simpleheat`: `npm install simpleheat` (or vendor the file — it's a single 3KB file)
  - GitHub: https://github.com/mourner/simpleheat
  - No Vue wrapper needed — it works directly with a canvas element
  - Usage: `const heat = simpleheat(canvas); heat.data(points); heat.draw();`

### Component Architecture

#### `DangerZonesMap.vue`
**Location**: `client/src/components/solo/DangerZonesMap.vue`

```vue
<template>
  <div class="danger-zones-map" data-testid="danger-zones-map" aria-label="Death heatmap on Summoner's Rift">
    <!-- Loading state -->
    <div v-if="loading" class="loading-state" data-testid="danger-zones-loading">
      <div class="skeleton skeleton-map" />
    </div>

    <!-- Empty state -->
    <div v-else-if="!hasData" class="empty-state" data-testid="danger-zones-empty">
      <p class="empty-text">No death position data available</p>
      <p class="empty-subtext">Play some games to see your danger zones</p>
    </div>

    <!-- Content -->
    <div v-else class="map-content">
      <!-- Phase filter buttons -->
      <div class="phase-filters" data-testid="phase-filters">
        <button
          v-for="phase in phaseOptions"
          :key="phase.value"
          :class="['phase-btn', { active: selectedPhase === phase.value }]"
          :data-testid="`phase-${phase.value}`"
          @click="selectPhase(phase.value)"
        >
          {{ phase.label }}
          <span class="phase-count">{{ phase.count }}</span>
        </button>
      </div>

      <!-- Map container -->
      <div class="map-container" ref="mapContainer">
        <img
          src="/assets/images/summoners-rift-minimap.png"
          alt="Summoner's Rift map"
          class="minimap-image"
          @load="onMapLoaded"
          ref="mapImage"
        />
        <canvas
          ref="heatCanvas"
          class="heat-overlay"
          data-testid="heat-canvas"
        />
      </div>

      <!-- Phase summary bar -->
      <div class="phase-summary" data-testid="phase-summary">
        <div class="phase-bar">
          <div
            v-for="segment in phaseSegments"
            :key="segment.phase"
            class="phase-segment"
            :style="{ width: segment.widthPct + '%', backgroundColor: segment.color }"
            :title="`${segment.label}: ${segment.count} deaths`"
          />
        </div>
        <div class="phase-labels">
          <span v-for="segment in phaseSegments" :key="segment.phase" class="phase-label">
            {{ segment.label }}: {{ segment.count }}
          </span>
        </div>
      </div>

      <!-- Context -->
      <p class="context-text">
        {{ totalDeaths }} deaths across {{ matchesAnalyzed }} games
      </p>
    </div>
  </div>
</template>
```

**Props**:
| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `deaths` | `Array` | `[]` | Array of `{ x, y, minuteMark, phase, killerChampionId, assistCount }` from API |
| `totalDeaths` | `Number` | `0` | Total death count |
| `matchesAnalyzed` | `Number` | `0` | Number of matches contributing data |
| `phaseSummary` | `Object` | `{ early: 0, mid: 0, late: 0, veryLate: 0 }` | Death counts per phase |
| `loading` | `Boolean` | `false` | Loading state |

**Internal State**:
- `selectedPhase` — `ref('all')` — current phase filter (local to component, not API filter. Filters the `deaths` prop client-side for instant switching)
- `mapLoaded` — `ref(false)` — whether the minimap image has loaded

**Key Computed**:
- `hasData` — `deaths.length > 0`
- `filteredDeaths` — deaths filtered by `selectedPhase` (all, early, mid, late, very_late)
- `phaseOptions` — array of `{ value, label, count }` for phase filter buttons
- `phaseSegments` — computed from `phaseSummary` for the stacked bar (with colors and width percentages)
- `heatPoints` — `filteredDeaths` mapped to canvas pixel coordinates `[canvasX, canvasY, intensity]`

**Methods**:
- `selectPhase(phase)` — sets `selectedPhase`, re-renders heatmap
- `onMapLoaded()` — sets `mapLoaded`, initializes canvas size and draws initial heatmap
- `renderHeatmap()` — uses `simpleheat` to draw heat points on canvas

**Watchers**:
- Watch `filteredDeaths` → call `renderHeatmap()`
- Watch `deaths` (prop) → re-render when parent provides new data (filter/time range change)

### Coordinate Normalization

Riot API coordinates range from (0, 0) at bottom-left to (~15000, ~15000) at top-right. The minimap image has its origin at top-left. The normalization:

```javascript
const MAP_SIZE = 15000 // Approximate Riot coordinate max

function riotToCanvas(x, y, canvasWidth, canvasHeight) {
  return {
    canvasX: (x / MAP_SIZE) * canvasWidth,
    canvasY: (1 - y / MAP_SIZE) * canvasHeight  // Invert Y axis (Riot Y increases upward, canvas Y increases downward)
  }
}
```

### simpleheat Usage

```javascript
import simpleheat from 'simpleheat'

function renderHeatmap() {
  const canvas = heatCanvas.value
  const ctx = canvas.getContext('2d')
  ctx.clearRect(0, 0, canvas.width, canvas.height)

  const heat = simpleheat(canvas)
  heat.radius(15, 20)  // point radius, blur radius
  heat.max(5)           // max data value for color scaling

  // Convert deaths to [x, y, intensity] format
  const points = filteredDeaths.value.map(d => {
    const { canvasX, canvasY } = riotToCanvas(d.x, d.y, canvas.width, canvas.height)
    return [canvasX, canvasY, 1]  // intensity 1 per death
  })

  heat.data(points)
  heat.draw()
}
```

### Phase Colors
| Phase | Color | Label |
|-------|-------|-------|
| Early (0–10) | `#3b82f6` (blue) | Early |
| Mid (10–20) | `#eab308` (yellow) | Mid |
| Late (20–30) | `#f97316` (orange) | Late |
| Very Late (30+) | `#ef4444` (red) | Very Late |

### SoloPage.vue Integration

Extend the Zone 4 `#deep-analysis` slot to include both the Radar Chart and Danger Zones Map side by side:

```vue
<template #deep-analysis>
  <div class="deep-analysis-grid">
    <!-- Radar Chart (from spider-chart-frontend) -->
    <BaseCard data-testid="radar-chart-card">
      <h3 class="section-title">Performance Profile</h3>
      <p class="section-subtitle">Your strengths and weaknesses across 6 dimensions</p>
      <RadarChart
        :axes="radarChartData?.axes ?? []"
        :games-analyzed="radarChartData?.gamesAnalyzed ?? 0"
        :loading="radarChartLoading"
      />
    </BaseCard>

    <!-- Danger Zones Map -->
    <BaseCard data-testid="danger-zones-card">
      <h3 class="section-title">Danger Zones</h3>
      <p class="section-subtitle">Where you die most frequently on the map</p>
      <DangerZonesMap
        :deaths="deathPositionsData?.deaths ?? []"
        :total-deaths="deathPositionsData?.totalDeaths ?? 0"
        :matches-analyzed="deathPositionsData?.matchesAnalyzed ?? 0"
        :phase-summary="deathPositionsData?.phaseSummary ?? { early: 0, mid: 0, late: 0, veryLate: 0 }"
        :loading="deathPositionsLoading"
      />
    </BaseCard>
  </div>
</template>
```

**New state in SoloPage.vue**:
```javascript
import DangerZonesMap from '../components/solo/DangerZonesMap.vue'
import { getDeathPositions } from '../services/authApi'

const deathPositionsData = ref(null)
const deathPositionsLoading = ref(false)

async function fetchDeathPositions() {
  if (!authStore.userId) return
  deathPositionsLoading.value = true
  try {
    deathPositionsData.value = await getDeathPositions(authStore.userId, queueFilter.value, timeRange.value)
  } catch (err) {
    console.error('Failed to fetch death positions:', err)
    deathPositionsData.value = null
  } finally {
    deathPositionsLoading.value = false
  }
}
```

Add `fetchDeathPositions()` to the `fetchAllData()` `Promise.all` array.

**Note**: The phase filter is **client-side only** — all death positions are fetched at once (no phase param to API) and filtered in the component for instant switching. This avoids re-fetching on every phase toggle.

### API Service Function

**File**: `client/src/services/authApi.js`

```javascript
/**
 * Get death position data for danger zone heatmap
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @returns {Promise<Object|null>} Death positions data or null
 */
export async function getDeathPositions(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') params.append('queueType', queueType)
  if (timeRange) params.append('timeRange', timeRange)

  const endpoint = `/solo/death-positions/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })
  if (response.status === 404) return null
  return parseResponse(response, 'Failed to get death positions')
}
```

### Minimap Asset

Place a Summoner's Rift minimap PNG at `client/public/assets/images/summoners-rift-minimap.png`. The image should be:
- Square aspect ratio (e.g., 512x512 or 1024x1024)
- Dark-themed to match the site's dark UI
- Shows terrain, jungle, lanes, river clearly
- No team-specific overlays (neutral map)

Source options:
1. Community Data Dragon extract from Riot's official assets
2. Riot's `minimap.png` from the game client (commonly redistributed by tools like op.gg, u.gg)

**Important**: Verify licensing. Riot's Legal Jibber Jabber allows use of game assets in community tools as long as they're not sold. The Mongoose.gg platform falls under this policy.

### Styling

```css
.danger-zones-map {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.map-container {
  position: relative;
  width: 100%;
  max-width: 400px;
  aspect-ratio: 1 / 1;
  margin: 0 auto;
}

.minimap-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: var(--border-radius-md);
}

.heat-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  border-radius: var(--border-radius-md);
}

.phase-filters {
  display: flex;
  gap: var(--spacing-xs);
  justify-content: center;
}

.phase-btn {
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--background-elevated);
  border: 1px solid var(--border);
  border-radius: var(--border-radius-sm);
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.phase-btn.active {
  background: var(--primary);
  border-color: var(--primary);
  color: white;
}

.phase-count {
  margin-left: var(--spacing-xs);
  opacity: 0.7;
}

.phase-summary {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.phase-bar {
  display: flex;
  height: 8px;
  border-radius: var(--border-radius-sm);
  overflow: hidden;
}

.phase-segment {
  transition: width var(--transition-fast);
}

.phase-labels {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
}

.context-text {
  text-align: center;
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
  margin: 0;
}
```

## Testing Strategy

### Unit Tests (Vitest)
**File**: `client/test/unit/components/solo/DangerZonesMap.spec.js`

Mock `simpleheat`:
```javascript
vi.mock('simpleheat', () => ({
  default: () => ({
    radius: vi.fn().mockReturnThis(),
    max: vi.fn().mockReturnThis(),
    data: vi.fn().mockReturnThis(),
    draw: vi.fn()
  })
}))
```

Test cases:
- [ ] `renders map when data is provided` — mount with deaths prop, verify map image and canvas exist
- [ ] `shows loading state` — mount with `loading: true`, verify skeleton visible
- [ ] `shows empty state when no data` — mount with empty deaths, verify empty message
- [ ] `renders phase filter buttons` — verify 5 buttons (All, Early, Mid, Late, Very Late)
- [ ] `filters deaths client-side when phase button clicked` — click "Early", verify only early-phase deaths are in filteredDeaths
- [ ] `shows phase summary bar` — verify phase segments render with correct colors
- [ ] `displays context text with death count and match count` — verify text content
- [ ] `normalizes coordinates correctly` — test `riotToCanvas` function with known values (center of map ~7500,7500 should map to ~center of canvas)

**File**: `client/test/unit/views/SoloPage.spec.js` (extend)
- [ ] `renders Zone 4 with both radar chart and danger zones map` — verify both cards present

### Integration Tests (Playwright)
**File**: Extend `client/e2e/solo-dashboard.spec.js`
- [ ] `danger zones map loads on solo page` — verify `[data-testid="danger-zones-map"]` visible
- [ ] `phase filter buttons work` — click phase button, verify canvas re-renders (or check button active state)

## Validation Criteria
Feature is considered complete when:
- [ ] Minimap image renders with heatmap overlay
- [ ] Death positions plot at correct map locations (verified visually with known coordinates)
- [ ] Phase filter buttons toggle instantly (client-side filtering)
- [ ] Phase summary bar shows correct death distribution with colored segments
- [ ] Loading skeleton displays while data fetches
- [ ] Empty state shows when no data available
- [ ] Context text shows death count and match count
- [ ] Component re-fetches when queue/time range filters change on Solo page
- [ ] Heatmap renders in < 200ms for 1000 death points
- [ ] Unit tests pass
- [ ] Accessible: `data-testid` on all interactive elements, `aria-label` on map

## Dependencies
### Internal Dependencies
- [ ] **Danger Zones Backend** ([danger-zones-backend.spec.md](danger-zones-backend.spec.md)) — must be implemented first for the API endpoint
- [ ] **Spider Chart Frontend** ([spider-chart-frontend.spec.md](spider-chart-frontend.spec.md)) — Zone 4 layout grid is defined there; both components share the Zone 4 slot
- [ ] `AnalysisLayout.vue` — Zone 4 `#deep-analysis` slot (already exists)
- [ ] `BaseCard` component — card wrapper (already exists)

### External Dependencies
- [ ] **`simpleheat`** — lightweight Canvas heatmap library (~3KB). Install via npm or vendor. MIT licensed.
- [ ] **Summoner's Rift minimap image** — must be sourced and placed at `client/public/assets/images/summoners-rift-minimap.png`

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Sparse heatmap with few games | Medium | Medium | Show minimum games warning: "Play 20+ games for accurate danger zones". Even sparse data provides value. |
| Minimap image sizing/DPI issues | Low | Medium | Use CSS `object-fit: contain` and match canvas size to rendered image size via `onMapLoaded` callback. |
| Canvas rendering performance | Low | Low | `simpleheat` handles 10K+ points efficiently. Cap at 1000 most recent deaths if needed. |
| Blue/red side normalization | Low | Low | Defer to v2. Most deaths happen in neutral zones (river, jungle). Side-specific analysis is a future enhancement. |

## References
- [Solo Page Graph Alternatives](../../../docs/solo-page-graph-alternatives.md) — Danger Zones section (UX analysis, implementation options)
- [Solo Page Feature Research](../../../docs/solo-page-feature-research.md) — Death Spatial Heatmap (Feature 2, academic evidence)
- [Solo Page Additional Metrics Research](../../../docs/solo-page-additional-metrics-research.md) — Time Spent Dead research
- [UI/UX Spec](../ui-ux.spec.md) — Zone layout, design tokens, color system
- [simpleheat GitHub](https://github.com/mourner/simpleheat) — Canvas heatmap library documentation
