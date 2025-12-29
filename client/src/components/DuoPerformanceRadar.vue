<template>
  <div class="radar-container">
    <div v-if="loading" class="loading">Loading performance radar…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No performance data available.</div>

    <ChartCard v-else title="🎯 Duo Performance Radar">
      <div class="radar-content">
        <svg :viewBox="`0 0 ${size} ${size}`" class="radar-svg">
          <g class="grid-circles">
            <circle v-for="level in [0.2, 0.4, 0.6, 0.8, 1]" :key="'grid-' + level"
              :cx="center" :cy="center" :r="radius * level"
              fill="none" stroke="var(--color-border)" stroke-width="1" opacity="0.3" />
          </g>
          <g class="axes">
            <g v-for="(metric, i) in metrics" :key="'axis-' + i">
              <line :x1="center" :y1="center" :x2="getAxisPoint(i).x" :y2="getAxisPoint(i).y"
                stroke="var(--color-border)" stroke-width="1" opacity="0.5" />
              <text :x="getLabelPoint(i).x" :y="getLabelPoint(i).y"
                text-anchor="middle" dominant-baseline="middle" class="axis-label">
                {{ metric.label }}
              </text>
            </g>
          </g>
          <polygon :points="polygonPoints" fill="var(--color-primary)" fill-opacity="0.3"
            stroke="var(--color-primary)" stroke-width="2" stroke-linejoin="round" />
          <circle v-for="(value, i) in normalizedValues" :key="'point-' + i"
            :cx="getDataPoint(i, value).x" :cy="getDataPoint(i, value).y"
            fill="var(--color-primary)" r="5" />
        </svg>

        <div class="metrics-list">
          <div v-for="(metric, i) in metrics" :key="'metric-' + i" class="metric-row">
            <span class="metric-label">{{ metric.label }}</span>
            <span class="metric-value">{{ getMetricValue(metric.key) }}</span>
          </div>
          <div class="games-played">{{ data.gamesPlayed }} games analyzed</div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoPerformanceRadar from '@/assets/getDuoPerformanceRadar.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const size = 280;
const center = size / 2;
const radius = 100;
const labelOffset = 30;

const metrics = [
  { key: 'kills', label: 'Kills' },
  { key: 'survival', label: 'Survival' },
  { key: 'assists', label: 'Assists' },
  { key: 'farming', label: 'Farming' },
  { key: 'gold', label: 'Gold' },
  { key: 'winRate', label: 'Win Rate' }
];

const hasData = computed(() => data.value?.gamesPlayed > 0);

const normalizedValues = computed(() => {
  if (!data.value?.normalized) return metrics.map(() => 0);
  const n = data.value.normalized;
  return [n.kills / 100, n.survival / 100, n.assists / 100, n.farming / 100, n.gold / 100, n.winRate / 100];
});

function getAxisPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return { x: center + radius * Math.cos(angle), y: center + radius * Math.sin(angle) };
}

function getLabelPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return { x: center + (radius + labelOffset) * Math.cos(angle), y: center + (radius + labelOffset) * Math.sin(angle) };
}

function getDataPoint(index, value) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  const r = radius * value;
  return { x: center + r * Math.cos(angle), y: center + r * Math.sin(angle) };
}

const polygonPoints = computed(() => {
  return normalizedValues.value.map((v, i) => {
    const pt = getDataPoint(i, v);
    return `${pt.x},${pt.y}`;
  }).join(' ');
});

function getMetricValue(key) {
  if (!data.value?.metrics) return '0';
  const m = data.value.metrics;
  switch (key) {
    case 'kills': return m.avgKills;
    case 'survival': return (10 - m.avgDeaths).toFixed(1);
    case 'assists': return m.avgAssists;
    case 'farming': return m.avgCs;
    case 'gold': return Math.round(m.avgGoldEarned / 1000) + 'k';
    case 'winRate': return m.winRate + '%';
    default: return '0';
  }
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try { data.value = await getDuoPerformanceRadar(props.userId); }
  catch (e) { error.value = e?.message || 'Failed to load performance radar.'; }
  finally { loading.value = false; }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.radar-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }
.radar-content { display: flex; gap: 1rem; align-items: center; padding: 1rem 0; flex: 1; justify-content: center; }
.radar-svg { flex: 0 0 200px; height: 200px; }
.axis-label { font-size: 11px; font-weight: 600; fill: var(--color-text); }
.metrics-list { width: 100%; display: flex; flex-direction: column; gap: 0.5rem; }
.metric-row { display: flex; justify-content: space-between; padding: 0.3rem 0; border-bottom: 1px solid var(--color-border); }
.metric-label { font-size: 0.85rem; color: var(--color-text-muted); }
.metric-value { font-size: 0.85rem; font-weight: 600; color: var(--color-text); }
.games-played { margin-top: 0.5rem; font-size: 0.8rem; color: var(--color-text-muted); text-align: center; }
</style>

