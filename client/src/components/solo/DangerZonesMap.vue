<template>
  <div class="danger-zones-map" data-testid="danger-zones-map" aria-label="Death heatmap on Summoner's Rift">
    <!-- Side Filters -->
    <div class="side-filters" role="group" aria-label="Filter deaths by side">
      <button
        v-for="option in sideOptions"
        :key="option.value"
        type="button"
        :class="['side-btn', { active: selectedSide === option.value }]"
        :aria-pressed="selectedSide === option.value"
        :data-testid="`side-filter-${option.value}`"
        @click="selectSide(option.value)"
      >
        {{ option.label }}
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-state" data-testid="loading-state">
      <div class="skeleton-map"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-state" data-testid="error-state">
      <p>{{ error }}</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="!hasData" class="empty-state" data-testid="empty-state">
      <p>No death position data available</p>
      <p class="empty-hint">Play more matches to see your danger zones</p>
    </div>

    <!-- Content -->
    <div v-else class="map-content">
      <!-- Map Container -->
      <div class="map-container" data-testid="map-container">
        <img
          ref="mapImage"
          :src="mapImageSrc"
          alt="Summoner's Rift minimap"
          class="minimap-image"
          @load="onMapLoaded"
        />
        <canvas
          ref="heatCanvas"
          class="heat-overlay"
          data-testid="heat-overlay"
        ></canvas>
      </div>

      <!-- Phase Filters -->
      <div class="phase-filters" role="group" aria-label="Filter deaths by game phase">
        <button
          v-for="option in phaseOptions"
          :key="option.value"
          type="button"
          :class="['phase-btn', { active: selectedPhase === option.value }]"
          :aria-pressed="selectedPhase === option.value"
          :data-testid="`phase-filter-${option.value}`"
          @click="selectPhase(option.value)"
        >
          {{ option.label }}
          <span v-if="option.count !== undefined" class="phase-count">({{ option.count }})</span>
        </button>
      </div>

      <!-- Phase Summary Bar -->
      <div class="phase-summary">
        <div class="phase-bar">
          <div
            v-for="segment in phaseSegments"
            :key="segment.phase"
            :style="{
              width: segment.widthPercent + '%',
              backgroundColor: segment.color
            }"
            class="phase-segment"
            :title="`${segment.label}: ${segment.count} deaths`"
          ></div>
        </div>
        <div class="phase-labels">
          <span
            v-for="segment in phaseSegments"
            :key="segment.phase + '-label'"
            :style="{ color: segment.color }"
          >
            {{ segment.label }}: {{ segment.count }}
          </span>
        </div>
      </div>

      <!-- Context Text -->
      <p class="context-text">
        {{ totalDeaths }} deaths across {{ matchesAnalyzed }} matches
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import simpleheat from 'simpleheat'

const props = defineProps({
  /** Array of death position objects from API */
  deaths: {
    type: Array,
    default: () => []
  },
  /** Total death count */
  totalDeaths: {
    type: Number,
    default: 0
  },
  /** Number of matches contributing data */
  matchesAnalyzed: {
    type: Number,
    default: 0
  },
  /** Death counts per phase */
  phaseSummary: {
    type: Object,
    default: () => ({ early: 0, mid: 0, late: 0, veryLate: 0 })
  },
  /** Loading state */
  loading: {
    type: Boolean,
    default: false
  },
  /** Error message */
  error: {
    type: String,
    default: null
  },
  /** Current queue filter (for server-side re-fetch) */
  queueType: {
    type: String,
    default: 'all'
  },
  /** Current time range filter (for server-side re-fetch) */
  timeRange: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['update:side'])

// Refs
const mapImage = ref(null)
const heatCanvas = ref(null)
const selectedPhase = ref('all')
const selectedSide = ref('all')
const mapLoaded = ref(false)
let heatInstance = null

// Constants
const MAP_SIZE = 15000 // Riot API coordinate max
const mapImageSrc = '/assets/images/summoners-rift-minimap.png'

// Side filter options
const sideOptions = [
  { value: 'all', label: 'All Deaths' },
  { value: 'blue', label: 'Blue Side' },
  { value: 'red', label: 'Red Side' }
]

// Phase colors
const phaseColors = {
  early: '#3b82f6',
  mid: '#eab308',
  late: '#f97316',
  veryLate: '#ef4444'
}

const phaseLabels = {
  early: 'Early',
  mid: 'Mid',
  late: 'Late',
  veryLate: 'Very Late'
}

// Computed
const hasData = computed(() => props.deaths && props.deaths.length > 0)

const filteredDeaths = computed(() => {
  if (selectedPhase.value === 'all') {
    return props.deaths
  }
  return props.deaths.filter(death => death.phase === selectedPhase.value)
})

const phaseOptions = computed(() => {
  const summary = props.phaseSummary
  return [
    { value: 'all', label: 'All', count: props.totalDeaths },
    { value: 'early', label: 'Early', count: summary.early },
    { value: 'mid', label: 'Mid', count: summary.mid },
    { value: 'late', label: 'Late', count: summary.late },
    { value: 'veryLate', label: 'Very Late', count: summary.veryLate }
  ]
})

const phaseSegments = computed(() => {
  const summary = props.phaseSummary
  const total = props.totalDeaths || 1 // Avoid division by zero
  
  return [
    {
      phase: 'early',
      label: phaseLabels.early,
      color: phaseColors.early,
      count: summary.early,
      widthPercent: (summary.early / total) * 100
    },
    {
      phase: 'mid',
      label: phaseLabels.mid,
      color: phaseColors.mid,
      count: summary.mid,
      widthPercent: (summary.mid / total) * 100
    },
    {
      phase: 'late',
      label: phaseLabels.late,
      color: phaseColors.late,
      count: summary.late,
      widthPercent: (summary.late / total) * 100
    },
    {
      phase: 'veryLate',
      label: phaseLabels.veryLate,
      color: phaseColors.veryLate,
      count: summary.veryLate,
      widthPercent: (summary.veryLate / total) * 100
    }
  ].filter(segment => segment.count > 0)
})

// Methods
function selectPhase(phase) {
  selectedPhase.value = phase
}

function selectSide(side) {
  if (selectedSide.value !== side) {
    selectedSide.value = side
    emit('update:side', side)
  }
}

function onMapLoaded() {
  if (!mapImage.value || !heatCanvas.value) return
  
  const img = mapImage.value
  heatCanvas.value.width = img.clientWidth
  heatCanvas.value.height = img.clientHeight

  // Create simpleheat instance once per canvas mount
  const canvas = heatCanvas.value
  heatInstance = simpleheat(canvas)
  heatInstance.radius(25, 35) // radius, blur
  heatInstance.max(2) // max data value for color scaling
  heatInstance.gradient({
    0.0: 'rgba(0, 0, 255, 0)',
    0.2: 'rgba(0, 0, 255, 0.5)',
    0.4: 'rgba(0, 255, 255, 0.6)',
    0.6: 'rgba(0, 255, 0, 0.7)',
    0.8: 'rgba(255, 255, 0, 0.8)',
    1.0: 'rgba(255, 0, 0, 0.9)'
  })

  mapLoaded.value = true
  
  // Initial render
  nextTick(() => {
    renderHeatmap()
  })
}

function riotToCanvas(x, y, canvasWidth, canvasHeight) {
  // Riot coords: (0,0) bottom-left, (15000,15000) top-right
  // Canvas: (0,0) top-left
  const normalizedX = x / MAP_SIZE
  const normalizedY = 1 - (y / MAP_SIZE) // Flip Y axis
  
  return {
    x: normalizedX * canvasWidth,
    y: normalizedY * canvasHeight
  }
}

function renderHeatmap() {
  if (!heatCanvas.value || !mapLoaded.value || !heatInstance) {
    return
  }

  const canvas = heatCanvas.value
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  // Compute points from filteredDeaths using current (live) canvas dimensions
  // This avoids stale coordinates from a computed that caches non-reactive canvas.width/height
  const points = filteredDeaths.value.map(death => {
    const coords = riotToCanvas(death.x, death.y, canvas.width, canvas.height)
    return [coords.x, coords.y, 1]
  })

  // Always clear canvas, even when there are no points (e.g. switching to an empty phase filter)
  heatInstance.clear()

  if (points.length === 0) {
    return
  }

  heatInstance.data(points)
  heatInstance.draw()
}

// Watchers

// Re-render when filtered deaths change (covers both new data from API and phase filter toggles)
watch(filteredDeaths, () => {
  if (mapLoaded.value) {
    nextTick(() => {
      renderHeatmap()
    })
  }
})

// Reset mapLoaded when loading starts — the map content unmounts (v-if chain),
// so the canvas will be a new DOM element when it re-mounts.
// This prevents rendering with stale/default canvas dimensions before onMapLoaded fires.
watch(() => props.loading, (isLoading) => {
  if (isLoading) {
    mapLoaded.value = false
    heatInstance = null
  }
})

// Reset phase filter when side changes
watch(selectedSide, () => {
  selectedPhase.value = 'all'
})
</script>

<style scoped>
.danger-zones-map {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.side-filters {
  display: flex;
  gap: var(--spacing-sm);
  justify-content: center;
  margin-bottom: var(--spacing-sm);
}

.side-btn {
  padding: var(--spacing-xs) var(--spacing-md);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.2s ease;
}

.side-btn:hover {
  background: var(--color-elevated);
  border-color: var(--color-primary);
}

.side-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: white;
}

.loading-state,
.error-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  color: var(--color-text-secondary);
  text-align: center;
}

.skeleton-map {
  width: 512px;
  height: 512px;
  max-width: 100%;
  background: linear-gradient(
    90deg,
    var(--color-surface) 25%,
    var(--color-elevated) 50%,
    var(--color-surface) 75%
  );
  background-size: 200% 100%;
  animation: skeleton-loading 1.5s ease-in-out infinite;
  border-radius: var(--radius-md);
}

@keyframes skeleton-loading {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

.empty-hint {
  font-size: var(--font-size-sm);
  margin-top: var(--spacing-sm);
  opacity: 0.7;
}

.map-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.map-container {
  position: relative;
  width: 512px;
  height: 512px;
  max-width: 100%;
  margin: 0 auto;
}

.minimap-image {
  width: 100%;
  height: 100%;
  display: block;
  border-radius: var(--radius-md);
}

.heat-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  border-radius: var(--radius-md);
}

.phase-filters {
  display: flex;
  gap: var(--spacing-sm);
  justify-content: center;
  flex-wrap: wrap;
}

.phase-btn {
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.2s ease;
}

.phase-btn:hover {
  background: var(--color-elevated);
  border-color: var(--color-primary);
}

.phase-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
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
  border-radius: var(--radius-sm);
  overflow: hidden;
  background: var(--color-surface);
}

.phase-segment {
  transition: width 0.3s ease;
}

.phase-labels {
  display: flex;
  gap: var(--spacing-md);
  justify-content: center;
  font-size: var(--font-size-sm);
  flex-wrap: wrap;
}

.context-text {
  text-align: center;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  margin: 0;
}

/* Responsive */
@media (max-width: 640px) {
  .map-container {
    width: 100%;
    height: auto;
    aspect-ratio: 1;
  }
}
</style>
