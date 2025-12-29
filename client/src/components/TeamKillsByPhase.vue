<template>
  <div class="kills-duration-container">
    <div v-if="loading" class="loading">Loading kills by duration…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No kills by duration data available.</div>

    <ChartCard v-else title="🗡️ Kills by Game Duration">
      <div class="chart-content">
        <!-- Best Duration Highlight -->
        <div v-if="data.bestDuration !== 'Not enough data'" class="best-duration">
          <span class="best-label">Best Kills:</span>
          <span class="best-value">{{ data.bestDuration }}</span>
          <span class="best-kills">{{ data.bestAvgKills }} avg</span>
        </div>

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
          <g v-for="(bar, i) in bars" :key="'bar-' + i">
            <rect :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height" :class="bar.class" rx="3" />
            <text :x="bar.x + bar.width / 2" :y="bar.y - 5" text-anchor="middle" class="bar-value">
              {{ bar.value }}
            </text>
            <text :x="bar.x + bar.width / 2" :y="chartHeight - 5" text-anchor="middle" class="bar-label">
              {{ bar.label }}
            </text>
            <text :x="bar.x + bar.width / 2" :y="chartHeight + 10" text-anchor="middle" class="bar-games">
              {{ bar.games }}g
            </text>
          </g>
        </svg>
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
const padding = { top: 25, right: 15, bottom: 35, left: 35 };

const hasData = computed(() => data.value?.buckets?.some(b => b.gamesPlayed > 0));

const maxKills = computed(() => {
  if (!data.value?.buckets) return 30;
  const max = Math.max(...data.value.buckets.map(b => b.avgKills));
  return Math.ceil(max / 5) * 5 || 30;
});

const yTicks = computed(() => {
  const max = maxKills.value;
  return [max, max * 0.5, 0];
});

function getY(value) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / maxKills.value);
}

const bars = computed(() => {
  if (!data.value?.buckets) return [];
  const buckets = data.value.buckets;
  const plotWidth = chartWidth - padding.left - padding.right;
  const barGap = 6;
  const barWidth = (plotWidth - (buckets.length - 1) * barGap) / buckets.length;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const avgKills = buckets.reduce((sum, b) => sum + b.avgKills, 0) / buckets.length;

  return buckets.map((bucket, i) => {
    const x = padding.left + i * (barWidth + barGap);
    const height = (bucket.avgKills / maxKills.value) * plotHeight;
    const isHigh = bucket.avgKills > avgKills;
    return {
      x, y: getY(bucket.avgKills), width: barWidth, height,
      value: bucket.avgKills.toFixed(1),
      label: bucket.label.replace(' min', ''),
      class: isHigh ? 'bar-high' : 'bar-low',
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
.kills-duration-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.kills-duration-container :deep(.chart-card) { flex: 1; display: flex; flex-direction: column; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.chart-content { padding: 0.5rem 0; flex: 1; display: flex; flex-direction: column; gap: 0.5rem; }

.best-duration { display: flex; align-items: center; gap: 0.5rem; padding: 0.4rem 0.75rem; background: var(--color-bg-elev); border-radius: 6px; }
.best-label { color: var(--color-text-muted); font-size: 0.8rem; }
.best-value { font-weight: 600; font-size: 0.85rem; }
.best-kills { color: var(--color-success); font-weight: 600; font-size: 0.85rem; }

.bar-chart { width: 100%; flex: 1; min-height: 100px; }

.grid line { stroke: var(--color-border); stroke-dasharray: 2,2; }
.y-labels text { fill: var(--color-text-muted); font-size: 9px; }
.bar-value { fill: var(--color-text); font-size: 10px; font-weight: 500; }
.bar-label { fill: var(--color-text); font-size: 9px; }
.bar-games { fill: var(--color-text-muted); font-size: 8px; }

.bar-high { fill: var(--color-success); opacity: 0.8; }
.bar-low { fill: var(--color-primary); opacity: 0.6; }
</style>

