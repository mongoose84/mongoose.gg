<template>
  <div class="duo-trend-container">
    <div v-if="loading" class="trend-loading">Loading win rate trend…</div>
    <div v-else-if="error" class="trend-error">{{ error }}</div>
    <div v-else-if="!hasData" class="trend-empty">
      Not enough data for trend analysis.
      <span class="requirement-hint">(Minimum: 1 game where both players played together)</span>
    </div>

    <ChartCard v-else title="📈 Win Rate Trend">
      <div class="trend-content">
        <div class="trend-indicator" :class="trendData.trendDirection">
          <span class="trend-arrow">{{ getTrendArrow(trendData.trendDirection) }}</span>
          <span class="trend-label">{{ getTrendLabel(trendData.trendDirection) }}</span>
        </div>

        <div class="trend-stats">
          <div class="stat">
            <span class="stat-value">{{ trendData.overallWinRate }}%</span>
            <span class="stat-label">Overall WR</span>
          </div>
          <div class="stat">
            <span class="stat-value" :class="getRecentClass()">{{ trendData.recentWinRate }}%</span>
            <span class="stat-label">Recent 10 WR</span>
          </div>
        </div>

        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart">
          <g class="grid">
            <line v-for="y in [0, 25, 50, 75, 100]" :key="'grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y)" :y2="getY(y)" />
          </g>
          <g class="y-labels">
            <text v-for="label in [100, 50, 0]" :key="'y-' + label"
              :x="padding.left - 8" :y="getY(label)" text-anchor="end" dominant-baseline="middle">
              {{ label }}%
            </text>
          </g>
          <g class="x-labels">
            <text v-for="dateLabel in dateLabels" :key="'x-' + dateLabel.index"
              :x="dateLabel.x" :y="chartHeight - 5" text-anchor="middle">
              {{ dateLabel.label }}
            </text>
          </g>
          <line class="reference-line" :x1="padding.left" :x2="chartWidth - padding.right" :y1="getY(50)" :y2="getY(50)" />
          <polyline :points="linePoints" class="trend-line" fill="none" stroke-width="2" stroke-linejoin="round" />
          <circle v-for="(point, i) in chartPoints" :key="'pt-' + i"
            :cx="point.x" :cy="point.y" r="4" :class="point.win ? 'point-win' : 'point-loss'" />
        </svg>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoWinRateTrend from '@/assets/getDuoWinRateTrend.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const trendData = ref(null);

const chartWidth = 500;
const chartHeight = 200;
const padding = { top: 20, right: 20, bottom: 30, left: 45 };
const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const hasData = computed(() => trendData.value?.dataPoints?.length > 0);

function getY(value) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / 100);
}

const chartPoints = computed(() => {
  if (!trendData.value?.dataPoints) return [];
  const points = trendData.value.dataPoints;
  const plotWidth = chartWidth - padding.left - padding.right;
  return points.map((p, i) => ({
    x: padding.left + (i / (points.length - 1 || 1)) * plotWidth,
    y: getY(p.rollingWinRate),
    win: p.win
  }));
});

const linePoints = computed(() => chartPoints.value.map(p => `${p.x},${p.y}`).join(' '));

const dateLabels = computed(() => {
  if (!trendData.value?.dataPoints) return [];
  const points = trendData.value.dataPoints;
  if (points.length === 0) return [];
  const plotWidth = chartWidth - padding.left - padding.right;
  const maxLabels = 10;
  const step = Math.max(1, Math.ceil(points.length / maxLabels));
  const labels = [];
  for (let i = 0; i < points.length; i += step) {
    const p = points[i];
    const x = padding.left + (i / (points.length - 1 || 1)) * plotWidth;
    const date = new Date(p.gameDate);
    labels.push({ index: i, x, label: `${date.getDate()}. ${monthNames[date.getMonth()]}` });
  }
  const lastIndex = points.length - 1;
  if (labels.length === 0 || labels[labels.length - 1].index !== lastIndex) {
    const p = points[lastIndex];
    const x = padding.left + plotWidth;
    const date = new Date(p.gameDate);
    labels.push({ index: lastIndex, x, label: `${date.getDate()}. ${monthNames[date.getMonth()]}` });
  }
  return labels;
});

function getTrendArrow(direction) {
  return { improving: '↑', declining: '↓', neutral: '→' }[direction] || '→';
}
function getTrendLabel(direction) {
  return { improving: 'Improving', declining: 'Declining', neutral: 'Stable' }[direction] || 'Stable';
}
function getRecentClass() {
  if (!trendData.value) return '';
  return trendData.value.recentWinRate >= 50 ? 'win-rate-high' : 'win-rate-low';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try { trendData.value = await getDuoWinRateTrend(props.userId); }
  catch (e) { error.value = e?.message || 'Failed to load trend data.'; }
  finally { loading.value = false; }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-trend-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.trend-loading, .trend-error, .trend-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.trend-error { color: var(--color-danger); }
.trend-empty .requirement-hint { display: block; margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.7; }
.trend-content { padding: 2rem 0 0.5rem 0; display: flex; flex-direction: column; gap: 1rem; flex: 1; justify-content: space-between; }
.trend-indicator { display: flex; align-items: center; gap: 0.5rem; font-weight: 600; }
.trend-arrow { font-size: 1.5rem; }
.trend-indicator.improving { color: var(--color-success); }
.trend-indicator.declining { color: var(--color-danger); }
.trend-indicator.neutral { color: var(--color-text-muted); }
.trend-stats { display: flex; gap: 2rem; }
.stat { display: flex; flex-direction: column; }
.stat-value { font-size: 1.25rem; font-weight: 600; }
.stat-label { font-size: 0.8rem; color: var(--color-text-muted); }
.win-rate-high { color: var(--color-success); }
.win-rate-low { color: var(--color-danger); }
.line-chart { width: 100%; height: auto; max-height: 220px; }
.grid line { stroke: var(--color-border); stroke-dasharray: 2,2; }
.y-labels text { fill: var(--color-text-muted); font-size: 10px; }
.x-labels text { fill: var(--color-text-muted); font-size: 9px; }
.reference-line { stroke: var(--color-text-muted); stroke-width: 1; opacity: 0.5; }
.trend-line { stroke: var(--color-primary); }
.point-win { fill: var(--color-success); }
.point-loss { fill: var(--color-danger); }
</style>

