<template>
  <div class="duo-vs-solo-performance">
    <div class="charts-header">
      <h3>Duo vs Solo Performance</h3>
      <p class="subtitle">Apes together strong?</p>
    </div>

    <div v-if="loading" class="charts-loading">Loading performance comparison…</div>
    <div v-else-if="error" class="charts-error">{{ error }}</div>
    <div v-else-if="!hasData" class="charts-empty">Not enough data for comparison yet.</div>
    
    <div v-else class="charts-grid">
      <!-- Winrate Chart -->
      <ChartCard title="Win Rate %">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart" aria-label="Win rate comparison">
          <!-- Y-axis grid lines and labels (0% to 100%) -->
          <g class="grid">
            <line v-for="y in [0, 25, 50, 75, 100]" :key="'wr-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y)" :y2="getY(y)" />
          </g>
          <g class="y-labels">
            <text v-for="label in [100, 75, 50, 25, 0]" :key="'wr-y-' + label"
              :x="padding.left - 8" :y="getY(label)" text-anchor="end" dominant-baseline="middle">
              {{ label }}
            </text>
          </g>

          <!-- Bars -->
          <g class="bars">
            <rect v-for="(bar, i) in winRateBars" :key="'wr-bar-' + i"
              :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height"
              :fill="bar.color" rx="3" />
          </g>

          <!-- Value labels inside bars -->
          <g class="bar-labels">
            <text v-for="(bar, i) in winRateBars" :key="'wr-label-' + i"
              :x="bar.x + bar.width / 2" :y="bar.y + 12"
              text-anchor="middle" class="bar-value-text">
              {{ bar.value }}%
            </text>
          </g>
        </svg>
      </ChartCard>

      <!-- Gold/min Chart -->
      <ChartCard title="Gold/min">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart" aria-label="Gold per minute comparison">
          <!-- Y-axis grid lines and labels -->
          <g class="grid">
            <line v-for="y in goldYTicks" :key="'gold-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y, maxGold)" :y2="getY(y, maxGold)" />
          </g>
          <g class="y-labels">
            <text v-for="label in goldYTicks" :key="'gold-y-' + label"
              :x="padding.left - 8" :y="getY(label, maxGold)" text-anchor="end" dominant-baseline="middle">
              {{ Math.round(label) }}
            </text>
          </g>

          <!-- Bars -->
          <g class="bars">
            <rect v-for="(bar, i) in goldBars" :key="'gold-bar-' + i"
              :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height"
              :fill="bar.color" rx="3" />
          </g>

          <!-- Value labels inside bars -->
          <g class="bar-labels">
            <text v-for="(bar, i) in goldBars" :key="'gold-label-' + i"
              :x="bar.x + bar.width / 2" :y="bar.y + 12"
              text-anchor="middle" class="bar-value-text">
              {{ bar.value }}
            </text>
          </g>
        </svg>
      </ChartCard>

      <!-- KDA Chart -->
      <ChartCard title="KDA">
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart" aria-label="KDA comparison">
          <!-- Y-axis grid lines and labels -->
          <g class="grid">
            <line v-for="y in kdaYTicks" :key="'kda-grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y, maxKda)" :y2="getY(y, maxKda)" />
          </g>
          <g class="y-labels">
            <text v-for="label in kdaYTicks" :key="'kda-y-' + label"
              :x="padding.left - 8" :y="getY(label, maxKda)" text-anchor="end" dominant-baseline="middle">
              {{ label.toFixed(1) }}
            </text>
          </g>

          <!-- Bars -->
          <g class="bars">
            <rect v-for="(bar, i) in kdaBars" :key="'kda-bar-' + i"
              :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height"
              :fill="bar.color" rx="3" />
          </g>

          <!-- Value labels inside bars -->
          <g class="bar-labels">
            <text v-for="(bar, i) in kdaBars" :key="'kda-label-' + i"
              :x="bar.x + bar.width / 2" :y="bar.y + 12"
              text-anchor="middle" class="bar-value-text">
              {{ bar.value }}
            </text>
          </g>
        </svg>
      </ChartCard>
    </div>

    <!-- Legend below all charts -->
    <div class="chart-legend">
      <div class="legend-item">
        <span class="legend-color" :style="{ backgroundColor: duoColor }"></span>
        <span class="legend-label">Duo</span>
      </div>
      <div class="legend-item">
        <span class="legend-color" :style="{ backgroundColor: soloAColor }"></span>
        <span class="legend-label">{{ gamer1Name }} (solo)</span>
      </div>
      <div class="legend-item">
        <span class="legend-color" :style="{ backgroundColor: soloBColor }"></span>
        <span class="legend-label">{{ gamer2Name }} (solo)</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoVsSoloPerformance from '@/assets/getDuoVsSoloPerformance.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
  gamers: {
    type: Array,
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const performanceData = ref(null);

// Chart dimensions
const chartWidth = 320;
const chartHeight = 180;
const padding = { top: 20, right: 20, bottom: 20, left: 50 };

// Colors
const duoColor = 'var(--color-primary)';  // Purple for duo
const soloAColor = 'var(--color-success)'; // Green for Solo A
const soloBColor = '#f59e0b';              // Amber for Solo B

const hasData = computed(() => performanceData.value !== null);

// Helper function to calculate Y position
function getY(value, max = 100) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / max);
}

// Gamer names for legend
const gamer1Name = computed(() => props.gamers?.[0]?.gamerName || 'Player 1');
const gamer2Name = computed(() => props.gamers?.[1]?.gamerName || 'Player 2');

// Win rate bars
const winRateBars = computed(() => {
  if (!performanceData.value) return [];

  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = 40;
  const groupWidth = barWidth * 3 + 20;
  const startX = padding.left + (plotWidth - groupWidth) / 2;

  const data = performanceData.value;
  const bars = [];

  // Duo bar
  const duoWR = data.duoWinRate || 0;
  bars.push({
    x: startX,
    y: getY(duoWR),
    width: barWidth,
    height: plotHeight * (duoWR / 100),
    color: duoColor,
    value: duoWR.toFixed(1),
    label: 'Duo'
  });

  // Solo A bar
  const soloAWR = data.soloAWinRate || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAWR),
    width: barWidth,
    height: plotHeight * (soloAWR / 100),
    color: soloAColor,
    value: soloAWR.toFixed(1),
    label: `${gamer1Name.value} (solo)`
  });

  // Solo B bar
  const soloBWR = data.soloBWinRate || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBWR),
    width: barWidth,
    height: plotHeight * (soloBWR / 100),
    color: soloBColor,
    value: soloBWR.toFixed(1),
    label: `${gamer2Name.value} (solo)`
  });

  return bars;
});

// Gold/min bars
const maxGold = computed(() => {
  if (!performanceData.value) return 500;
  const values = [
    performanceData.value.duoGoldPerMin || 0,
    performanceData.value.soloAGoldPerMin || 0,
    performanceData.value.soloBGoldPerMin || 0
  ];
  const max = Math.max(...values);
  return Math.ceil(max / 100) * 100; // Round up to nearest 100
});

const goldYTicks = computed(() => {
  const max = maxGold.value;
  return [max, max * 0.75, max * 0.5, max * 0.25, 0];
});

const goldBars = computed(() => {
  if (!performanceData.value) return [];

  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = 40;
  const groupWidth = barWidth * 3 + 20;
  const startX = padding.left + (plotWidth - groupWidth) / 2;
  const max = maxGold.value;

  const data = performanceData.value;
  const bars = [];

  // Duo bar
  const duoGold = data.duoGoldPerMin || 0;
  bars.push({
    x: startX,
    y: getY(duoGold, max),
    width: barWidth,
    height: plotHeight * (duoGold / max),
    color: duoColor,
    value: Math.round(duoGold),
    label: 'Duo'
  });

  // Solo A bar
  const soloAGold = data.soloAGoldPerMin || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAGold, max),
    width: barWidth,
    height: plotHeight * (soloAGold / max),
    color: soloAColor,
    value: Math.round(soloAGold),
    label: `${gamer1Name.value} (solo)`
  });

  // Solo B bar
  const soloBGold = data.soloBGoldPerMin || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBGold, max),
    width: barWidth,
    height: plotHeight * (soloBGold / max),
    color: soloBColor,
    value: Math.round(soloBGold),
    label: `${gamer2Name.value} (solo)`
  });

  return bars;
});

// KDA bars
const maxKda = computed(() => {
  if (!performanceData.value) return 5;
  const values = [
    performanceData.value.duoKda || 0,
    performanceData.value.soloAKda || 0,
    performanceData.value.soloBKda || 0
  ];
  const max = Math.max(...values);
  return Math.ceil(max);
});

const kdaYTicks = computed(() => {
  const max = maxKda.value;
  return [max, max * 0.75, max * 0.5, max * 0.25, 0];
});

const kdaBars = computed(() => {
  if (!performanceData.value) return [];

  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = 40;
  const groupWidth = barWidth * 3 + 20;
  const startX = padding.left + (plotWidth - groupWidth) / 2;
  const max = maxKda.value;

  const data = performanceData.value;
  const bars = [];

  // Duo bar
  const duoKda = data.duoKda || 0;
  bars.push({
    x: startX,
    y: getY(duoKda, max),
    width: barWidth,
    height: plotHeight * (duoKda / max),
    color: duoColor,
    value: duoKda.toFixed(2),
    label: 'Duo'
  });

  // Solo A bar
  const soloAKda = data.soloAKda || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAKda, max),
    width: barWidth,
    height: plotHeight * (soloAKda / max),
    color: soloAColor,
    value: soloAKda.toFixed(2),
    label: `${gamer1Name.value} (solo)`
  });

  // Solo B bar
  const soloBKda = data.soloBKda || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBKda, max),
    width: barWidth,
    height: plotHeight * (soloBKda / max),
    color: soloBColor,
    value: soloBKda.toFixed(2),
    label: `${gamer2Name.value} (solo)`
  });

  return bars;
});

// Load function - fetches real data from API
async function load() {
  if (!props.userId) return;

  loading.value = true;
  error.value = null;

  try {
    const data = await getDuoVsSoloPerformance(props.userId);
    performanceData.value = data;
  } catch (e) {
    console.error('Error loading duo vs solo performance:', e);
    error.value = e?.message || 'Failed to load performance comparison.';
    performanceData.value = null;
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(() => props.userId, load);

defineExpose({ load });
</script>

<style scoped>
.duo-vs-solo-performance {
  padding: 1.5rem;
  background: var(--color-bg-elev);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.charts-header {
  margin-bottom: 1.5rem;
}

.charts-header h3 {
  margin: 0 0 0.25rem 0;
  font-size: 1.25rem;
  color: var(--color-text);
}

.charts-header .subtitle {
  margin: 0;
  font-size: 0.9rem;
  color: var(--color-text-muted);
  font-style: italic;
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
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}



.bar-chart {
  width: 100%;
  height: auto;
  display: block;
}

.bar-chart .grid line {
  stroke: var(--color-border);
  stroke-width: 1;
  opacity: 0.3;
}

.bar-chart text {
  fill: var(--color-text);
  font-size: 12px;
}

.bar-chart .y-labels text {
  font-size: 11px;
  fill: var(--color-text-muted);
}

.bar-chart .x-labels text {
  font-size: 12px;
  font-weight: 600;
  fill: var(--color-text);
}

.bar-chart .bars rect {
  transition: opacity 0.2s ease;
}

/* Bar value labels */
.bar-value-text {
  font-size: 11px;
  font-weight: 600;
  fill: white;
  pointer-events: none;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
}

/* Legend */
.chart-legend {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 2rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.legend-color {
  width: 16px;
  height: 16px;
  border-radius: 3px;
  display: inline-block;
}

.legend-label {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--color-text);
}

/* Mobile stacking */
@media (max-width: 900px) {
  .charts-grid {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .chart-legend {
    flex-direction: column;
    gap: 0.75rem;
  }
}
</style>


