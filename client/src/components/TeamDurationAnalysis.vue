<template>
  <div class="team-duration-container">
    <div v-if="loading" class="duration-loading">Loading duration analysis…</div>
    <div v-else-if="error" class="duration-error">{{ error }}</div>
    <div v-else-if="!hasData" class="duration-empty">
      Not enough data for duration analysis.
      <span class="requirement-hint">(Minimum: 1 game where all team members played together)</span>
    </div>

    <ChartCard v-else title="⏱️ Game Duration Analysis">
      <div class="duration-content">
        <!-- Best Duration Highlight -->
        <div v-if="durationData.bestDuration !== 'Not enough data'" class="best-duration">
          <span class="best-label">Best Performance:</span>
          <span class="best-value">{{ durationData.bestDuration }}</span>
          <span class="best-wr">{{ durationData.bestWinRate }}% WR</span>
        </div>

        <!-- Bar Chart -->
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart">
          <!-- Y-axis grid lines -->
          <g class="grid">
            <line v-for="y in [0, 25, 50, 75, 100]" :key="'grid-' + y"
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="getY(y)" :y2="getY(y)" />
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="label in [100, 50, 0]" :key="'y-' + label"
              :x="padding.left - 8" :y="getY(label)" text-anchor="end" dominant-baseline="middle">
              {{ label }}%
            </text>
          </g>
          <!-- 50% reference line -->
          <line class="reference-line" :x1="padding.left" :x2="chartWidth - padding.right" :y1="getY(50)" :y2="getY(50)" />
          <!-- Bars -->
          <g v-for="(bar, i) in bars" :key="'bar-' + i">
            <rect :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height" :class="bar.class" rx="4" />
            <text :x="bar.x + bar.width / 2" :y="bar.y - 5" text-anchor="middle" class="bar-value">
              {{ bar.winRate }}%
            </text>
            <text :x="bar.x + bar.width / 2" :y="chartHeight - 5" text-anchor="middle" class="bar-label">
              {{ bar.shortLabel }}
            </text>
            <text :x="bar.x + bar.width / 2" :y="chartHeight + 12" text-anchor="middle" class="bar-games">
              {{ bar.games }} games
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
import getTeamDurationAnalysis from '@/assets/getTeamDurationAnalysis.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const durationData = ref(null);

const chartWidth = 500;
const chartHeight = 220;
const padding = { top: 30, right: 15, bottom: 40, left: 40 };

const hasData = computed(() => durationData.value?.buckets?.some(b => b.gamesPlayed > 0));

function getY(value) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - value / 100);
}

const bars = computed(() => {
  if (!durationData.value?.buckets) return [];
  const buckets = durationData.value.buckets;
  const plotWidth = chartWidth - padding.left - padding.right;
  // Adjusted spacing for 6 bars
  const barGap = 8;
  const barWidth = (plotWidth - (buckets.length - 1) * barGap) / buckets.length;
  const plotHeight = chartHeight - padding.top - padding.bottom;

  return buckets.map((bucket, i) => {
    const x = padding.left + i * (barWidth + barGap);
    const height = (bucket.winRate / 100) * plotHeight;
    return {
      x, y: getY(bucket.winRate), width: barWidth, height,
      winRate: bucket.winRate.toFixed(0),
      // Use the label directly, it's already short like "20-25 min"
      shortLabel: bucket.label.replace(' min', ''),
      games: bucket.gamesPlayed,
      class: bucket.winRate >= 50 ? 'bar-positive' : 'bar-negative'
    };
  });
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    durationData.value = await getTeamDurationAnalysis(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load duration analysis.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-duration-container { width: 100%; }
.duration-loading, .duration-error, .duration-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.duration-error { color: var(--color-danger); }
.duration-empty .requirement-hint { display: block; margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.7; }
.duration-content { padding: 2rem 0 0.5rem 0; display: flex; flex-direction: column; gap: 1rem; }
.best-duration { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; background: var(--color-bg-elev); border-radius: 6px; }
.best-label { color: var(--color-text-muted); font-size: 0.9rem; }
.best-value { font-weight: 600; }
.best-wr { color: var(--color-success); font-weight: 600; }
.bar-chart { width: 100%; height: auto; max-height: 240px; }
.grid line { stroke: var(--color-border); stroke-dasharray: 2,2; }
.y-labels text { fill: var(--color-text-muted); font-size: 10px; }
.reference-line { stroke: var(--color-text-muted); stroke-width: 1; opacity: 0.5; }
.bar-positive { fill: var(--color-success); opacity: 0.8; }
.bar-negative { fill: var(--color-danger); opacity: 0.8; }
.bar-value { fill: var(--color-text); font-size: 12px; font-weight: 600; }
.bar-label { fill: var(--color-text); font-size: 11px; }
.bar-games { fill: var(--color-text-muted); font-size: 9px; }
</style>

