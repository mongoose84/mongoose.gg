<template>
  <div class="deaths-trend-container">
    <div v-if="loading" class="loading">Loading deaths trend…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No deaths trend data available.</div>

    <ChartCard v-else title="📈 Deaths Trend">
      <div class="trend-content">
        <!-- Summary Stats -->
        <div class="summary-row">
          <div class="stat">
            <span class="stat-label">Overall Avg</span>
            <span class="stat-value">{{ data.overallAvgDeaths }}</span>
          </div>
          <div class="stat">
            <span class="stat-label">Recent Avg</span>
            <span class="stat-value" :class="trendClass">{{ data.recentAvgDeaths }}</span>
          </div>
          <div class="stat trend-indicator">
            <span class="trend-icon">{{ trendIcon }}</span>
            <span class="trend-text">{{ trendText }}</span>
          </div>
        </div>

        <!-- Line Chart -->
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yTicks" :key="'grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y)" :y2="getY(y)" />
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="y in yTicks" :key="'y-' + y"
              :x="padding.left - 5" :y="getY(y)" text-anchor="end" dominant-baseline="middle">
              {{ y }}
            </text>
          </g>
          <!-- Rolling average line -->
          <polyline :points="rollingLinePoints" class="rolling-line" />
          <!-- Data points -->
          <g class="data-points">
            <circle v-for="(pt, i) in dataPoints" :key="'pt-' + i"
              :cx="pt.x" :cy="pt.y" r="4"
              :class="pt.win ? 'win-point' : 'loss-point'" />
          </g>
        </svg>

        <!-- Legend -->
        <div class="legend">
          <span class="legend-item"><span class="dot win"></span> Win</span>
          <span class="legend-item"><span class="dot loss"></span> Loss</span>
          <span class="legend-item"><span class="line-sample"></span> Rolling Avg</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamDeathsTrend from '@/assets/getTeamDeathsTrend.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const chartWidth = 400;
const chartHeight = 160;
const padding = { top: 15, right: 15, bottom: 20, left: 35 };

const hasData = computed(() => data.value?.dataPoints?.length > 0);

const maxDeaths = computed(() => {
  if (!data.value?.dataPoints) return 30;
  const max = Math.max(...data.value.dataPoints.map(d => d.teamDeaths));
  return Math.ceil(max / 5) * 5 || 30;
});

const yTicks = computed(() => {
  const max = maxDeaths.value;
  return [max, max * 0.5, 0];
});

function getY(value) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / maxDeaths.value);
}

function getX(index, total) {
  const plotWidth = chartWidth - padding.left - padding.right;
  return padding.left + (plotWidth / (total - 1 || 1)) * index;
}

const dataPoints = computed(() => {
  if (!data.value?.dataPoints) return [];
  const pts = data.value.dataPoints;
  return pts.map((d, i) => ({
    x: getX(i, pts.length),
    y: getY(d.teamDeaths),
    win: d.win
  }));
});

const rollingLinePoints = computed(() => {
  if (!data.value?.dataPoints) return '';
  const pts = data.value.dataPoints;
  return pts.map((d, i) => `${getX(i, pts.length)},${getY(d.rollingAvgDeaths)}`).join(' ');
});

const trendClass = computed(() => {
  if (!data.value) return '';
  return data.value.trendDirection === 'improving' ? 'improving' : 
         data.value.trendDirection === 'declining' ? 'declining' : '';
});

const trendIcon = computed(() => {
  if (!data.value) return '➡️';
  return data.value.trendDirection === 'improving' ? '📉' : 
         data.value.trendDirection === 'declining' ? '📈' : '➡️';
});

const trendText = computed(() => {
  if (!data.value) return 'Stable';
  return data.value.trendDirection === 'improving' ? 'Improving' : 
         data.value.trendDirection === 'declining' ? 'Declining' : 'Stable';
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getTeamDeathsTrend(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load deaths trend data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.deaths-trend-container { width: 100%; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.trend-content { padding: 0.5rem 0; }

.summary-row { display: flex; gap: 1.5rem; margin-bottom: 0.75rem; align-items: center; }
.stat { display: flex; flex-direction: column; }
.stat-label { font-size: 0.7rem; color: var(--color-text-muted); }
.stat-value { font-size: 1rem; font-weight: 600; }
.stat-value.improving { color: var(--color-success); }
.stat-value.declining { color: var(--color-danger); }

.trend-indicator { flex-direction: row; gap: 0.3rem; }
.trend-icon { font-size: 1rem; }
.trend-text { font-size: 0.8rem; font-weight: 500; }

.line-chart { width: 100%; height: auto; }
.grid line { stroke: var(--color-border); stroke-dasharray: 2,2; }
.y-labels text { fill: var(--color-text-muted); font-size: 9px; }
.rolling-line { fill: none; stroke: var(--color-primary); stroke-width: 2; }
.win-point { fill: var(--color-success); }
.loss-point { fill: var(--color-danger); }

.legend { display: flex; justify-content: center; gap: 1rem; margin-top: 0.5rem; }
.legend-item { display: flex; align-items: center; gap: 0.3rem; font-size: 0.7rem; color: var(--color-text-muted); }
.dot { width: 8px; height: 8px; border-radius: 50%; }
.dot.win { background: var(--color-success); }
.dot.loss { background: var(--color-danger); }
.line-sample { width: 16px; height: 2px; background: var(--color-primary); }
</style>

