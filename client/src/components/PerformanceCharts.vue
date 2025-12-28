<template>
  <div class="performance-charts">
    <div class="charts-header">
      <h3>Performance Over Time</h3>
      <div class="limit-toggle">
        <button 
          v-for="opt in periodOptions" 
          :key="opt.value"
          :class="['limit-btn', { active: period === opt.value }]"
          @click="period = opt.value"
        >
          {{ opt.label }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="charts-loading">Loading performance data…</div>
    <div v-else-if="error" class="charts-error">{{ error }}</div>
    <div v-else-if="!hasData" class="charts-empty">No performance data available yet.</div>
    
    <div v-else class="charts-grid">
      <!-- Win-rate Over Time (10-game intervals) -->
      <ChartCard title="Win-rate Over Time (10-game intervals)">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart" aria-label="Win-rate over time">
          <!-- Y-axis grid lines and labels (0% to 100%) -->
          <g class="grid">
            <line v-for="y in [0, 0.25, 0.5, 0.75, 1]" :key="'wr-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="barY(y)" :y2="barY(y)" />
          </g>
          <g class="y-labels">
            <text v-for="(label, i) in [100, 75, 50, 25, 0]" :key="'wr-y-' + i"
              :x="padding.left - 8" :y="barY(label/100)" text-anchor="end" dominant-baseline="middle">
              {{ label }}%
            </text>
          </g>
          <!-- X-axis: dates -->
          <g class="x-labels">
            <text v-for="(tick, i) in winrateXTicks" :key="'wr-x-' + i"
              :x="tick.x" :y="chartHeight - padding.bottom + 14" text-anchor="middle" dominant-baseline="hanging">
              {{ tick.label }}
            </text>
          </g>
          <!-- Win-rate lines for each gamer/server -->
          <g v-for="(gamer, gi) in chartData" :key="'wr-line-' + gi">
            <polyline
              :points="getIntervalWinratePoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-linejoin="round"
            />
          </g>
          <!-- Legend -->
          <g class="legend" :transform="`translate(${padding.left}, ${chartHeight + 20})`">
            <g v-for="(gamer, gi) in chartData" :key="'wr-leg-' + gi"
              :transform="`translate(${gi * 160}, 0)`">
              <rect :fill="getColor(gi)" width="16" height="16" rx="3" />
              <text x="22" y="13" class="legend-text">{{ gamer.gamerName }}</text>
            </g>
          </g>
        </svg>
      </ChartCard>

      <!-- Gold/min Chart (10-game intervals) -->
      <ChartCard title="Gold/min (10-game intervals)">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart" aria-label="Gold per 10 games">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yGridLinesEcon" :key="'gold-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="y" :y2="y" />
          </g>
          <!-- X-axis date labels -->
          <g class="x-labels">
            <text v-for="(tick, i) in econIntervalXTicks" :key="'gold-x-' + i"
              :x="tick.x" :y="chartHeight - padding.bottom + 14" text-anchor="middle" dominant-baseline="hanging">
              {{ tick.label }}
            </text>
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="(label, i) in yLabelsGold" :key="'gold-y-' + i"
              :x="padding.left - 8" :y="yGridLinesEcon[i]" text-anchor="end" dominant-baseline="middle">
              {{ label }}
            </text>
          </g>
          <!-- Gold lines (solid) -->
          <g v-for="(gamer, gi) in chartData" :key="'gold-line-' + gi">
            <polyline
              :points="getIntervalGoldPoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-linejoin="round"
            />
          </g>
          <!-- Legend -->
          <g class="legend" :transform="`translate(${padding.left}, ${chartHeight + 20})`">
            <g v-for="(gamer, gi) in chartData" :key="'gold-leg-' + gi"
              :transform="`translate(${gi * 160}, 0)`">
              <rect :fill="getColor(gi)" width="16" height="16" rx="3" />
              <text x="22" y="13" class="legend-text">{{ gamer.gamerName }}</text>
            </g>
          </g>
        </svg>
      </ChartCard>

      <!-- CS/min Chart (10-game intervals) -->
      <ChartCard title="CS/min (10-game intervals)">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart" aria-label="CS per 10 games">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yGridLinesCs" :key="'cs-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="y" :y2="y" />
          </g>
          <!-- X-axis date labels -->
          <g class="x-labels">
            <text v-for="(tick, i) in econIntervalXTicks" :key="'cs-x-' + i"
              :x="tick.x" :y="chartHeight - padding.bottom + 14" text-anchor="middle" dominant-baseline="hanging">
              {{ tick.label }}
            </text>
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="(label, i) in yLabelsCs" :key="'cs-y-' + i"
              :x="padding.left - 8" :y="yGridLinesCs[i]" text-anchor="end" dominant-baseline="middle">
              {{ label }}
            </text>
          </g>
          <!-- CS lines (solid) -->
          <g v-for="(gamer, gi) in chartData" :key="'cs-line-' + gi">
            <polyline
              :points="getIntervalCsPoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-linejoin="round"
            />
          </g>
          <!-- Legend -->
          <g class="legend" :transform="`translate(${padding.left}, ${chartHeight + 20})`">
            <g v-for="(gamer, gi) in chartData" :key="'cs-leg-' + gi"
              :transform="`translate(${gi * 160}, 0)`">
              <rect :fill="getColor(gi)" width="16" height="16" rx="3" />
              <text x="22" y="13" class="legend-text">{{ gamer.gamerName }}</text>
            </g>
          </g>
        </svg>
      </ChartCard>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import getPerformance from '@/assets/getPerformance.js';
import ChartCard from './ChartCard.vue';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const periodOptions = [
  { value: '20', label: '20 Games' },
  { value: '50', label: '50 Games' },
  { value: '100', label: '100 Games' },
  { value: 'all', label: 'All' },
];
const period = ref('50');
const loading = ref(false);
const error = ref(null);

// This will hold the performance data for the selected period
const filteredPerformanceData = ref(null);

// Helper: Compute win-rate over time in 10-game intervals for each gamer
function getIntervalWinratePoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  const interval = 10;
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const intervals = [];
  for (let i = 0; i < points.length; i += interval) {
    const chunk = points.slice(i, i + interval);
    if (chunk.length === 0) continue;
    const wins = chunk.filter(d => d.win).length;
    const winrate = wins / chunk.length;
    intervals.push({
      idx: i / interval,
      winrate,
      start: i,
      end: i + chunk.length - 1
    });
  }
  const denom = Math.max(1, intervals.length - 1);
  return intervals.map((d, i) => {
    const x = padding.left + (i / denom) * plotWidth;
    const y = padding.top + plotHeight * (1 - d.winrate);
    return `${x},${y}`;
  }).join(' ');
}

// Helper: Format date for X-axis labels
function formatDate(dateString) {
  const date = new Date(dateString);
  const day = date.getDate();
  const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const month = monthNames[date.getMonth()];
  return `${day}. ${month}`;
}

// Helper: X-axis ticks for win-rate over time chart
const winrateXTicks = computed(() => {
  // Use the gamer with the most data points as reference
  const gamerWithMostData = chartData.value.reduce((max, g) =>
    (g.dataPoints?.length || 0) > (max.dataPoints?.length || 0) ? g : max
  , { dataPoints: [] });

  const points = gamerWithMostData.dataPoints || [];
  if (points.length === 0) return [];

  const interval = 10;
  const nIntervals = Math.ceil(points.length / interval);
  const plotWidth = chartWidth - padding.left - padding.right;

  // Limit to max 10 ticks for readability
  const maxTicks = 10;
  const tickStep = Math.max(1, Math.ceil(nIntervals / maxTicks));
  const ticks = [];

  for (let i = 0; i < nIntervals; i += tickStep) {
    const pointIndex = i * interval;
    if (pointIndex < points.length) {
      const x = padding.left + (i / Math.max(1, nIntervals - 1)) * plotWidth;
      const label = formatDate(points[pointIndex].gameEndTimestamp);
      ticks.push({ x, label });
    }
  }

  // Always add the last game date as the final tick if not already included
  const lastPointIndex = points.length - 1;
  const lastIntervalIndex = Math.floor(lastPointIndex / interval);
  const lastTickIntervalIndex = Math.floor((nIntervals - 1) / tickStep) * tickStep;

  if (lastIntervalIndex !== lastTickIntervalIndex && points.length > 0) {
    const x = padding.left + plotWidth; // Rightmost position
    const label = formatDate(points[lastPointIndex].gameEndTimestamp);
    ticks.push({ x, label });
  }

  return ticks;
});

// Helper: X-axis ticks for gold/cs interval chart
const econIntervalXTicks = computed(() => {
  // Use the gamer with the most data points as reference
  const gamerWithMostData = chartData.value.reduce((max, g) =>
    (g.dataPoints?.length || 0) > (max.dataPoints?.length || 0) ? g : max
  , { dataPoints: [] });

  const points = gamerWithMostData.dataPoints || [];
  if (points.length === 0) return [];

  const interval = 10;
  const nIntervals = Math.ceil(points.length / interval);
  const plotWidth = chartWidth - padding.left - padding.right;

  // Limit to max 10 ticks for readability
  const maxTicks = 10;
  const tickStep = Math.max(1, Math.ceil(nIntervals / maxTicks));
  const ticks = [];

  for (let i = 0; i < nIntervals; i += tickStep) {
    const pointIndex = i * interval;
    if (pointIndex < points.length) {
      const x = padding.left + (i / Math.max(1, nIntervals - 1)) * plotWidth;
      const label = formatDate(points[pointIndex].gameEndTimestamp);
      ticks.push({ x, label });
    }
  }

  // Always add the last game date as the final tick if not already included
  const lastPointIndex = points.length - 1;
  const lastIntervalIndex = Math.floor(lastPointIndex / interval);
  const lastTickIntervalIndex = Math.floor((nIntervals - 1) / tickStep) * tickStep;

  if (lastIntervalIndex !== lastTickIntervalIndex && points.length > 0) {
    const x = padding.left + plotWidth; // Rightmost position
    const label = formatDate(points[lastPointIndex].gameEndTimestamp);
    ticks.push({ x, label });
  }

  return ticks;
});

// Helper: Gold/min points over 10-game intervals
function getIntervalGoldPoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  const interval = 10;
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const max = maxEconValue.value * 30;
  const intervals = [];
  for (let i = 0; i < points.length; i += interval) {
    const chunk = points.slice(i, i + interval);
    if (chunk.length === 0) continue;
    const avgGold = chunk.reduce((sum, d) => sum + (d.goldPerMin || 0), 0) / chunk.length;
    intervals.push({
      idx: i / interval,
      avgGold,
      start: i,
      end: i + chunk.length - 1
    });
  }
  const denom = Math.max(1, intervals.length - 1);
  return intervals.map((d, i) => {
    const x = padding.left + (i / denom) * plotWidth;
    const y = padding.top + plotHeight * (1 - Math.min(d.avgGold, max) / max);
    return `${x},${y}`;
  }).join(' ');
}

// Helper: CS/min points over 10-game intervals (fixed y-axis max = 10)
function getIntervalCsPoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  const interval = 10;
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const max = 10;
  const intervals = [];
  for (let i = 0; i < points.length; i += interval) {
    const chunk = points.slice(i, i + interval);
    if (chunk.length === 0) continue;
    const avgCs = chunk.reduce((sum, d) => sum + (d.csPerMin || 0), 0) / chunk.length;
    intervals.push({
      idx: i / interval,
      avgCs,
      start: i,
      end: i + chunk.length - 1
    });
  }
  const denom = Math.max(1, intervals.length - 1);
  return intervals.map((d, i) => {
    const x = padding.left + (i / denom) * plotWidth;
    const y = padding.top + plotHeight * (1 - Math.min(d.avgCs, max) / max);
    return `${x},${y}`;
  }).join(' ');
}

// Chart dimensions (wider graph area, side-by-side layout)
const chartWidth = 520;
const chartHeight = 220;
const padding = { top: 28, right: 32, bottom: 38, left: 55 };


const colors = [
  'var(--color-primary)',      // Purple
  'var(--color-success)',      // Green
  '#f59e0b',                   // Amber
  '#ec4899',                   // Pink
  '#06b6d4',                   // Cyan
];

const getColor = (index) => colors[index % colors.length];


// Compute win-rate and loss-rate per gamer/server, using filtered data
const chartData = computed(() => {
  if (!filteredPerformanceData.value?.gamers) return [];
  return filteredPerformanceData.value.gamers.map(g => {
    const total = g.dataPoints?.length || 0;
    const wins = g.dataPoints?.filter(d => d.win).length || 0;
    const losses = total - wins;
    return {
      ...g,
      shortName: g.gamerName,
      winRate: total > 0 ? wins / total : 0,
      lossRate: total > 0 ? losses / total : 0,
    };
  });
});

// Helper function for Y-axis positioning
function barY(rate) {
  // rate: 0-1
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - rate);
}

const hasData = computed(() => chartData.value.length > 0 && chartData.value.some(g => g.dataPoints?.length > 0));

// Y-axis for Gold/min (dynamic)
const maxEconValue = computed(() => {
  let max = 10;
  chartData.value.forEach(g => {
    g.dataPoints?.forEach(d => {
      max = Math.max(max, d.goldPerMin / 30); // Only gold for gold chart
    });
  });
  return Math.ceil(max / 2) * 2; // Round up to even number
});

const yGridLinesEcon = computed(() => {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return [0, 0.25, 0.5, 0.75, 1].map(frac => padding.top + plotHeight * frac);
});

const yLabelsGold = computed(() => {
  const max = maxEconValue.value;
  return [max, max * 0.75, max * 0.5, max * 0.25, 0].map(v => Math.round(v));
});

// Y-axis for CS/min (fixed at 10)
const yGridLinesCs = computed(() => {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return [0, 0.25, 0.5, 0.75, 1].map(frac => padding.top + plotHeight * frac);
});
const yLabelsCs = [10, 7.5, 5, 2.5, 0];

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    // Fetch data from server with the specified limit
    console.log('Loading performance data with period:', period.value);
    filteredPerformanceData.value = await getPerformance(props.userId, period.value);
    console.log('Loaded data:', filteredPerformanceData.value);

    // Debug: Log date range for each gamer
    if (filteredPerformanceData.value?.gamers) {
      filteredPerformanceData.value.gamers.forEach(gamer => {
        const points = gamer.dataPoints || [];
        if (points.length > 0) {
          const firstDate = points[0].gameEndTimestamp;
          const lastDate = points[points.length - 1].gameEndTimestamp;
          console.log(`${gamer.gamerName}: ${points.length} games from ${firstDate} to ${lastDate}`);
        }
      });
    }
  } catch (e) {
    console.error('Error loading performance data:', e);
    error.value = e?.message || 'Failed to load performance data.';
    filteredPerformanceData.value = null;
  } finally {
    loading.value = false;
  }
}

// Watch period and reload data when changed
watch(period, load);

onMounted(load);
watch(() => props.userId, load);
</script>

<style scoped>
.performance-charts {
  margin-top: 2rem;
  padding: 1rem;
  background: var(--color-bg-elev);
  border-radius: 12px;
  border: 1px solid var(--color-border);
  max-width: 100%; /* Prevent overflow */
  overflow-x: hidden; /* Hide any horizontal overflow */
  box-sizing: border-box;
}

.charts-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1rem;
}

.charts-header h3 {
  margin: 0;
  font-size: 1.1rem;
  color: var(--color-text);
}

.limit-toggle {
  display: flex;
  gap: 0.25rem;
}

.limit-btn {
  padding: 0.35rem 0.75rem;
  font-size: 0.85rem;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}

.limit-btn:first-child { border-radius: 6px 0 0 6px; }
.limit-btn:last-child { border-radius: 0 6px 6px 0; }

.limit-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: var(--color-text);
}

.limit-btn:hover:not(.active) {
  background: var(--color-bg-hover);
}

.charts-loading,
.charts-error,
.charts-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.charts-error {
  color: var(--color-danger);
}

.charts-grid {
  display: flex;
  flex-direction: row;
  gap: 1rem;
  justify-content: space-between;
  align-items: flex-start;
  max-width: 100%; /* Prevent overflow */
  overflow-x: auto; /* Allow scrolling if charts are too wide */
  flex-wrap: nowrap; /* Keep charts in a single row */
}


.line-chart {
  width: 100%;
  max-width: 100%; /* Prevent SVG from overflowing */
  height: 220px;
  display: block;
}


    
/* Style for SVG legend text */
.line-chart .legend-text {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.01em;
}
.line-chart text,
.line-chart .legend-text {
  fill: var(--color-text) !important;
}

/* Add subtle background to legend for better readability */
.line-chart .legend {
  filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.1));
}

.chart-footnote {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  text-align: center;
  margin-top: 0.5rem;
  opacity: 0.8;
}

/* Mobile stacking */
@media (max-width: 1200px) {
  .charts-grid {
    flex-direction: column;
    gap: 1.2rem;
  }
  .line-chart {
    min-width: 0;
    max-width: 100vw;
  }
}
</style>
