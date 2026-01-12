<template>
  <div class="radar-chart-container">
    <div v-if="loading" class="radar-loading">Loading radar data…</div>
    <div v-else-if="error" class="radar-error">{{ error }}</div>
    <div v-else-if="!hasData" class="radar-empty">No radar data available.</div>

    <ChartCard v-else title="Radar Chart – EUW vs EUNE">
      <div class="radar-content">
        <svg :viewBox="`0 0 ${size} ${size}`" class="radar-svg" aria-label="Performance radar chart">
        <!-- Background grid circles -->
        <g class="grid-circles">
          <circle v-for="level in [0.2, 0.4, 0.6, 0.8, 1]" :key="'grid-' + level"
            :cx="centerX" :cy="centerY" :r="radius * level"
            fill="none" stroke="var(--color-border)" stroke-width="1" opacity="0.3" />
        </g>
        
        <!-- Axis lines and labels -->
        <g class="axes">
          <g v-for="(metric, i) in metrics" :key="'axis-' + i">
            <line 
              :x1="centerX" :y1="centerY"
              :x2="getAxisPoint(i).x" :y2="getAxisPoint(i).y"
              stroke="var(--color-border)" stroke-width="1" opacity="0.5" />
            <text 
              :x="getLabelPoint(i).x" :y="getLabelPoint(i).y"
              text-anchor="middle" dominant-baseline="middle"
              class="axis-label">
              {{ metric.label }}
            </text>
          </g>
        </g>
        
        <!-- Data polygons for each gamer -->
        <g class="data-polygons">
          <g v-for="(gamer, gi) in radarData" :key="'polygon-' + gi">
            <polygon
              :points="getPolygonPoints(gamer)"
              :fill="getColor(gi)"
              :stroke="getColor(gi)"
              fill-opacity="0.2"
              stroke-width="2"
              stroke-linejoin="round" />
            <!-- Data points -->
            <circle v-for="(value, mi) in gamer.values" :key="'point-' + gi + '-' + mi"
              :cx="getDataPoint(mi, value).x" :cy="getDataPoint(mi, value).y"
              :fill="getColor(gi)" r="4" />
          </g>
        </g>
        
        <!-- Legend -->
        <g class="legend" :transform="`translate(${legendX}, ${legendY})`">
          <g v-for="(gamer, gi) in radarData" :key="'legend-' + gi"
            :transform="`translate(0, ${gi * 20})`">
            <rect :fill="getColor(gi)" width="14" height="14" rx="2" />
            <text x="20" y="11" class="legend-text">{{ gamer.name }}</text>
          </g>
        </g>
      </svg>

      <!-- Suggestions Panel -->
      <div class="suggestions-panel">
        <h5 class="suggestions-title">Top Priority</h5>
        <div class="suggestions-list">
          <div v-for="(suggestion, index) in suggestions" :key="index" class="suggestion-item">
            <div class="suggestion-icon">💡</div>
            <div class="suggestion-content">
              <div class="suggestion-header">
                <strong>{{ suggestion.metric }}</strong>
              </div>
              <div class="suggestion-text">{{ suggestion.text }}</div>
              <div class="suggestion-target">{{ suggestion.target }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from '@/components/shared/ChartCard.vue';
import { getComparison } from '@/api/solo.js';
import { sortGamerNames, GAMER_COLORS } from '@/composables/useGamerColors.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const comparisonData = ref(null);

// Chart dimensions - shifted to the left to make room for suggestions
const size = 400;
const centerX = 180; // Shifted left from center (size / 2 would be 200)
const centerY = size / 2;
const radius = 140;
const labelOffset = 45; // Distance of labels from graph
const legendX = 20;
const legendY = size - 60;

// Metrics configuration with normalization ranges
const metrics = [
  { key: 'kills', label: 'Kills', max: 10 },
  { key: 'deaths', label: 'Deaths', max: 10, inverse: true }, // Lower is better
  { key: 'assists', label: 'Assists', max: 10 },
  { key: 'csPerMin', label: 'CS/min', max: 10 },
  { key: 'goldPerMin', label: 'Gold/min', max: 600 },
  { key: 'timeDead', label: 'Time Dead', max: 300, inverse: true }, // Lower is better
];

const getColor = (index) => GAMER_COLORS[index % GAMER_COLORS.length];

// Get axis endpoint coordinates
function getAxisPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return {
    x: centerX + radius * Math.cos(angle),
    y: centerY + radius * Math.sin(angle),
  };
}

// Get label position (further out from axis endpoint)
function getLabelPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return {
    x: centerX + (radius + labelOffset) * Math.cos(angle),
    y: centerY + (radius + labelOffset) * Math.sin(angle),
  };
}

// Get data point coordinates
function getDataPoint(metricIndex, normalizedValue) {
  const angle = (Math.PI * 2 * metricIndex) / metrics.length - Math.PI / 2;
  const r = radius * normalizedValue;
  return {
    x: centerX + r * Math.cos(angle),
    y: centerY + r * Math.sin(angle),
  };
}

// Generate polygon points string
function getPolygonPoints(gamer) {
  return gamer.values
    .map((value, i) => {
      const point = getDataPoint(i, value);
      return `${point.x},${point.y}`;
    })
    .join(' ');
}

// Compute radar data from comparison data
const radarData = computed(() => {
  if (!comparisonData.value) return [];

  // Extract gamer names from any metric array and sort them alphabetically
  const names = comparisonData.value.winrate?.map(g => g.gamerName) || [];
  const gamerNames = sortGamerNames(names);

  return gamerNames.map(name => {
    // Get actual data from the API
    const kills = comparisonData.value.avgKills?.find(g => g.gamerName === name)?.value || 0;
    const deaths = comparisonData.value.avgDeaths?.find(g => g.gamerName === name)?.value || 0;
    const assists = comparisonData.value.avgAssists?.find(g => g.gamerName === name)?.value || 0;
    const csPerMin = comparisonData.value.csPrMin?.find(g => g.gamerName === name)?.value || 0;
    const goldPerMin = comparisonData.value.goldPrMin?.find(g => g.gamerName === name)?.value || 0;
    const timeDead = comparisonData.value.avgTimeDeadSeconds?.find(g => g.gamerName === name)?.value || 0;

    // Normalize values to 0-1 range
    const values = metrics.map(metric => {
      let rawValue;
      switch (metric.key) {
        case 'kills': rawValue = kills; break;
        case 'deaths': rawValue = deaths; break;
        case 'assists': rawValue = assists; break;
        case 'csPerMin': rawValue = csPerMin; break;
        case 'goldPerMin': rawValue = goldPerMin; break;
        case 'timeDead': rawValue = timeDead; break;
        default: rawValue = 0;
      }

      // Normalize to 0-1
      let normalized = Math.min(rawValue / metric.max, 1);

      // For inverse metrics (lower is better), invert the normalized value
      if (metric.inverse) {
        normalized = 1 - normalized;
      }

      return Math.max(0, normalized);
    });

    return {
      name,
      values,
    };
  });
});

const hasData = computed(() => radarData.value.length > 0);

// Generate improvement suggestion - compute all 6 metrics and show the most impactful one
const suggestions = computed(() => {
  if (!comparisonData.value || radarData.value.length === 0) return [];

  // Get the first gamer's data (or average if multiple)
  const firstGamer = radarData.value[0];
  if (!firstGamer) return [];

  // Find gamer's raw data
  const gamerName = firstGamer.name;
  const kills = comparisonData.value.avgKills?.find(g => g.gamerName === gamerName)?.value || 0;
  const deaths = comparisonData.value.avgDeaths?.find(g => g.gamerName === gamerName)?.value || 0;
  const assists = comparisonData.value.avgAssists?.find(g => g.gamerName === gamerName)?.value || 0;
  const csPerMin = comparisonData.value.csPrMin?.find(g => g.gamerName === gamerName)?.value || 0;
  const goldPerMin = comparisonData.value.goldPrMin?.find(g => g.gamerName === gamerName)?.value || 0;
  const timeDead = comparisonData.value.avgTimeDeadSeconds?.find(g => g.gamerName === gamerName)?.value || 0;

  // Calculate priority scores for ALL 6 metrics (higher = more important to improve)
  const allMetrics = [];

  // 1. Kills - progressive target based on current performance
  const killsTarget = kills < 4 ? 5 : kills < 6 ? 7 : 8;
  if (kills < killsTarget) {
    const gap = killsTarget - kills;
    allMetrics.push({
      priority: gap * 2,
      metric: 'Kills',
      text: 'Focus on getting more last hits on enemy champions. Work on your damage timing and positioning during fights.',
      target: `Target: ${killsTarget}+ kills per game (currently ${kills.toFixed(1)})`
    });
  }

  // 2. Deaths - progressive target (weighted higher - most impactful)
  const deathsTarget = deaths > 8 ? 7 : deaths > 6 ? 5 : 4;
  if (deaths > deathsTarget) {
    const gap = deaths - deathsTarget;
    allMetrics.push({
      priority: gap * 2.5,
      metric: 'Deaths',
      text: 'Try to position better in teamfights to avoid getting focused. Ward more and check the minimap frequently.',
      target: `Target: <${deathsTarget} deaths per game (currently ${deaths.toFixed(1)})`
    });
  }

  // 3. Assists - progressive target
  const assistsTarget = assists < 5 ? 6 : assists < 7 ? 8 : 10;
  if (assists < assistsTarget) {
    const gap = assistsTarget - assists;
    allMetrics.push({
      priority: gap * 1.5,
      metric: 'Assists',
      text: 'Participate more in team objectives and fights. Roam when possible to help your teammates.',
      target: `Target: ${assistsTarget}+ assists per game (currently ${assists.toFixed(1)})`
    });
  }

  // 4. CS/min - progressive target
  const csTarget = csPerMin < 4 ? 5 : csPerMin < 5.5 ? 6 : 7;
  if (csPerMin < csTarget) {
    const gap = csTarget - csPerMin;
    allMetrics.push({
      priority: gap * 2,
      metric: 'CS/min',
      text: 'Improve your CS by practicing last-hitting in practice tool. Focus on wave management.',
      target: `Target: ${csTarget}+ CS/min (currently ${csPerMin.toFixed(1)})`
    });
  }

  // 5. Gold/min - progressive target
  const goldTarget = goldPerMin < 300 ? 330 : goldPerMin < 350 ? 380 : 420;
  if (goldPerMin < goldTarget) {
    const gap = goldTarget - goldPerMin;
    allMetrics.push({
      priority: gap * 0.05,
      metric: 'Gold/min',
      text: 'Focus on farming more efficiently and taking objectives. Participate in tower takedowns and secure neutral objectives.',
      target: `Target: ${goldTarget}+ gold/min (currently ${goldPerMin.toFixed(0)})`
    });
  }

  // 6. Time Dead - realistic progressive target (reduce by 20-30%)
  const timeDeadTarget = timeDead > 200 ? Math.round(timeDead * 0.75) : timeDead > 100 ? Math.round(timeDead * 0.8) : 60;
  if (timeDead > timeDeadTarget) {
    const gap = timeDead - timeDeadTarget;
    allMetrics.push({
      priority: gap * 0.15,
      metric: 'Time Dead',
      text: 'Avoid late game deaths as they have much longer timers. Play extra safe after 25 minutes and avoid risky plays when death timers are high.',
      target: `Target: <${timeDeadTarget} seconds per game (currently ${timeDead.toFixed(0)}s)`
    });
  }

  // Sort by priority and return only the MOST impactful suggestion
  if (allMetrics.length > 0) {
    allMetrics.sort((a, b) => b.priority - a.priority);
    return [allMetrics[0]]; // Return only the top suggestion
  }

  // If no issues found, provide positive feedback
  return [{
    metric: 'Overall',
    text: 'Great performance! Keep up the good work and maintain consistency across all metrics.',
    target: 'Keep performing at this level!'
  }];
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    comparisonData.value = await getComparison(props.userId);
  } catch (e) {
    console.error('Error loading radar chart data:', e);
    error.value = e?.message || 'Failed to load radar chart data.';
    comparisonData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.radar-chart-container {
  width: 100%;
  max-width: 100%;
}

/* Override ChartCard max-width for radar chart */
.radar-chart-container :deep(.chart-card) {
  max-width: 100%;
  height: auto;
  min-height: 400px;
}

.radar-loading,
.radar-error,
.radar-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.radar-error {
  color: var(--color-danger);
}

/* Layout: chart on left, suggestions on right */
.radar-content {
  display: flex;
  gap: 1.5rem;
  align-items: flex-start;
  overflow: visible;
}

.radar-svg {
  flex: 0 0 400px;
  height: 100%;
  display: block;
}

/* Suggestions Panel */
.suggestions-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.5rem 0;
  min-width: 0;
}

.suggestions-title {
  margin: 0 0 0.5rem 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-text);
  text-transform: uppercase;
  letter-spacing: 0.03em;
  opacity: 0.85;
}

.suggestions-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.suggestion-item {
  display: flex;
  gap: 0.75rem;
  padding: 0.85rem;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  transition: all 0.2s ease;
}

.suggestion-item:hover {
  border-color: var(--color-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.suggestion-icon {
  font-size: 1.3rem;
  flex-shrink: 0;
  line-height: 1;
  margin-top: 0.1rem;
}

.suggestion-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  min-width: 0;
}

.suggestion-header strong {
  color: var(--color-primary);
  font-weight: 600;
  font-size: 0.9rem;
}

.suggestion-text {
  font-size: 0.85rem;
  line-height: 1.5;
  color: var(--color-text);
}

.suggestion-target {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-muted);
  padding: 0.35rem 0.6rem;
  background: var(--color-bg-elev);
  border-radius: 4px;
  border-left: 3px solid var(--color-primary);
  margin-top: 0.2rem;
}

.axis-label {
  font-size: 13px;
  font-weight: 600;
  fill: var(--color-text);
}

.legend-text {
  font-size: 13px;
  font-weight: 600;
  fill: var(--color-text);
}

/* Responsive: stack vertically on smaller screens */
@media (max-width: 900px) {
  .radar-content {
    flex-direction: column;
    align-items: center;
  }

  .radar-svg {
    flex: 0 0 auto;
    width: 100%;
    max-width: 400px;
  }

  .suggestions-panel {
    width: 100%;
  }
}
</style>

