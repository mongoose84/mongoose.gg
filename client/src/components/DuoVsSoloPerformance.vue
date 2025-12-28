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
          
          <!-- X-axis labels -->
          <g class="x-labels">
            <text v-for="(label, i) in xLabels" :key="'wr-x-' + i"
              :x="label.x" :y="chartHeight - padding.bottom + 14" 
              text-anchor="middle" dominant-baseline="hanging">
              {{ label.text }}
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
          
          <!-- X-axis labels -->
          <g class="x-labels">
            <text v-for="(label, i) in xLabels" :key="'gold-x-' + i"
              :x="label.x" :y="chartHeight - padding.bottom + 14" 
              text-anchor="middle" dominant-baseline="hanging">
              {{ label.text }}
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
          
          <!-- X-axis labels -->
          <g class="x-labels">
            <text v-for="(label, i) in xLabels" :key="'kda-x-' + i"
              :x="label.x" :y="chartHeight - padding.bottom + 14" 
              text-anchor="middle" dominant-baseline="hanging">
              {{ label.text }}
            </text>
          </g>
        </svg>
      </ChartCard>
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
const chartHeight = 200;
const padding = { top: 20, right: 20, bottom: 40, left: 50 };

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

// X-axis labels with real gamer names
const xLabels = computed(() => {
  const plotWidth = chartWidth - padding.left - padding.right;
  const barWidth = 40;
  const groupWidth = barWidth * 3 + 20; // 3 bars + spacing
  const startX = padding.left + (plotWidth - groupWidth) / 2;

  // Get gamer names, fallback to generic labels if not available
  const gamer1Name = props.gamers?.[0]?.gamerName || 'Player 1';
  const gamer2Name = props.gamers?.[1]?.gamerName || 'Player 2';

  return [
    { x: startX + barWidth / 2, text: 'Duo' },
    { x: startX + barWidth + 10 + barWidth / 2, text: `${gamer1Name} (solo)` },
    { x: startX + barWidth * 2 + 20 + barWidth / 2, text: `${gamer2Name} (solo)` }
  ];
});

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
    color: duoColor
  });

  // Solo A bar
  const soloAWR = data.soloAWinRate || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAWR),
    width: barWidth,
    height: plotHeight * (soloAWR / 100),
    color: soloAColor
  });

  // Solo B bar
  const soloBWR = data.soloBWinRate || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBWR),
    width: barWidth,
    height: plotHeight * (soloBWR / 100),
    color: soloBColor
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
    color: duoColor
  });

  // Solo A bar
  const soloAGold = data.soloAGoldPerMin || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAGold, max),
    width: barWidth,
    height: plotHeight * (soloAGold / max),
    color: soloAColor
  });

  // Solo B bar
  const soloBGold = data.soloBGoldPerMin || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBGold, max),
    width: barWidth,
    height: plotHeight * (soloBGold / max),
    color: soloBColor
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
    color: duoColor
  });

  // Solo A bar
  const soloAKda = data.soloAKda || 0;
  bars.push({
    x: startX + barWidth + 10,
    y: getY(soloAKda, max),
    width: barWidth,
    height: plotHeight * (soloAKda / max),
    color: soloAColor
  });

  // Solo B bar
  const soloBKda = data.soloBKda || 0;
  bars.push({
    x: startX + barWidth * 2 + 20,
    y: getY(soloBKda, max),
    width: barWidth,
    height: plotHeight * (soloBKda / max),
    color: soloBColor
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
  margin-top: 2rem;
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

.bar-chart .bars rect:hover {
  opacity: 0.8;
}

/* Mobile stacking */
@media (max-width: 900px) {
  .charts-grid {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }
}
</style>


