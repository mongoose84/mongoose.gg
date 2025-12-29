<template>
  <div class="kills-duration-container">
    <div v-if="loading" class="loading">Loading kills by duration…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No kills by duration data available.</div>

    <ChartCard v-else title="🗡️ Kills by Game Length">
      <div class="chart-content">
        <!-- SVG Bar Chart -->
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yTicks" :key="'grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y)" :y2="getY(y)" />
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="y in yTicks" :key="'y-' + y"
              :x="padding.left - 8" :y="getY(y)" text-anchor="end" dominant-baseline="middle">
              {{ y }}
            </text>
          </g>
          <!-- Bars -->
          <g class="bars">
            <rect v-for="(bar, i) in bars" :key="'bar-' + i"
              :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height"
              :fill="bar.color" rx="3" />
            <!-- Value labels on bars -->
            <text v-for="(bar, i) in bars" :key="'val-' + i"
              :x="bar.x + bar.width / 2" :y="bar.y - 5" text-anchor="middle" class="bar-value">
              {{ bar.value }}
            </text>
          </g>
          <!-- X-axis labels -->
          <g class="x-labels">
            <text v-for="(bar, i) in bars" :key="'x-' + i"
              :x="bar.x + bar.width / 2" :y="chartHeight - 5" text-anchor="middle">
              {{ bar.label }}
            </text>
          </g>
        </svg>

        <!-- Legend -->
        <div class="legend">
          <span class="legend-item">
            <span class="dot high"></span> High kills
          </span>
          <span class="legend-item">
            <span class="dot low"></span> Low kills
          </span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamKillsByPhase from '@/assets/getTeamKillsByPhase.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const chartWidth = 350;
const chartHeight = 180;
const padding = { top: 25, right: 20, bottom: 30, left: 40 };

const hasData = computed(() => data.value?.buckets?.some(b => b.gamesPlayed > 0));

const maxKills = computed(() => {
  if (!data.value?.buckets) return 30;
  const max = Math.max(...data.value.buckets.map(b => b.avgKills));
  return Math.ceil(max / 5) * 5 || 30;
});

const yTicks = computed(() => {
  const max = maxKills.value;
  return [max, max * 0.75, max * 0.5, max * 0.25, 0];
});

function getY(value) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / maxKills.value);
}

const bars = computed(() => {
  if (!data.value?.buckets) return [];
  const buckets = data.value.buckets;
  const plotWidth = chartWidth - padding.left - padding.right;
  const barWidth = plotWidth / buckets.length - 15;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const avgKills = buckets.reduce((sum, b) => sum + b.avgKills, 0) / buckets.length;

  return buckets.map((bucket, i) => {
    const x = padding.left + (plotWidth / buckets.length) * i + 7.5;
    const height = (bucket.avgKills / maxKills.value) * plotHeight;
    const isHigh = bucket.avgKills > avgKills;
    return {
      x, y: getY(bucket.avgKills), width: barWidth, height,
      value: bucket.avgKills.toFixed(1),
      label: bucket.label.replace(' min', ''),
      color: isHigh ? 'var(--color-success)' : 'var(--color-danger)',
      games: bucket.gamesPlayed
    };
  });
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getTeamKillsByPhase(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load kills by duration data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.kills-duration-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.kills-duration-container :deep(.chart-card) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.chart-content {
  padding: 1rem 0;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.bar-chart {
  width: 100%;
  flex: 1;
  min-height: 120px;
}

.grid line { stroke: var(--color-border); stroke-dasharray: 2,2; }
.y-labels text { fill: var(--color-text-muted); font-size: 10px; }
.x-labels text { fill: var(--color-text-muted); font-size: 10px; }
.bar-value { fill: var(--color-text); font-size: 11px; font-weight: 500; }

.legend {
  display: flex;
  justify-content: center;
  gap: 1.5rem;
  margin-top: auto;
  padding-top: 0.5rem;
}

.legend-item { display: flex; align-items: center; gap: 0.4rem; font-size: 0.75rem; color: var(--color-text-muted); }
.dot { width: 10px; height: 10px; border-radius: 50%; }
.dot.high { background: var(--color-success); }
.dot.low { background: var(--color-danger); }
</style>

