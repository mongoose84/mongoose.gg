<template>
  <div class="side-win-rate-container">
    <div v-if="loading" class="loading">Loading side statistics…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No side data available.</div>

    <ChartCard v-else title="🗺️ Map Side Win Rate">
      <div class="chart-content">
        <!-- Horizontal Bar Chart -->
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart">
          <!-- Blue side bar -->
          <g class="bar-group">
            <text :x="padding.left - 8" :y="getBarY(0) + barHeight / 2 + 5" class="bar-label" text-anchor="end">Blue</text>
            <rect
              :x="padding.left"
              :y="getBarY(0)"
              :width="getBarWidth(sideData.blueWinRate)"
              :height="barHeight"
              class="bar blue-bar"
            />
            <text :x="padding.left + getBarWidth(sideData.blueWinRate) + 8" :y="getBarY(0) + barHeight / 2 + 5" class="bar-value">
              {{ sideData.blueWinRate.toFixed(1) }}%
            </text>
          </g>

          <!-- Red side bar -->
          <g class="bar-group">
            <text :x="padding.left - 8" :y="getBarY(1) + barHeight / 2 + 5" class="bar-label" text-anchor="end">Red</text>
            <rect
              :x="padding.left"
              :y="getBarY(1)"
              :width="getBarWidth(sideData.redWinRate)"
              :height="barHeight"
              class="bar red-bar"
            />
            <text :x="padding.left + getBarWidth(sideData.redWinRate) + 8" :y="getBarY(1) + barHeight / 2 + 5" class="bar-value">
              {{ sideData.redWinRate.toFixed(1) }}%
            </text>
          </g>

          <!-- X-axis labels -->
          <g class="x-axis">
            <line :x1="padding.left" :y1="chartHeight - padding.bottom + 10" :x2="chartWidth - padding.right" :y2="chartHeight - padding.bottom + 10" stroke="var(--color-border)" />
            <text v-for="tick in xAxisTicks" :key="tick" :x="getXPosition(tick)" :y="chartHeight - padding.bottom + 25" class="axis-label" text-anchor="middle">
              {{ tick }}%
            </text>
          </g>
        </svg>

        <!-- Summary text -->
        <p class="summary-text">
          You played <strong>{{ sideData.blueGames }}</strong> games on the blue side
          (<strong>{{ sideData.bluePercentage.toFixed(1) }}%</strong> of total games)
          and <strong>{{ sideData.redGames }}</strong> games on the red side
          (<strong>{{ sideData.redPercentage.toFixed(1) }}%</strong> of total games)
        </p>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getSideStats from '@/assets/getSideStats.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
  mode: {
    type: String,
    default: null, // 'solo', 'duo', 'team', or null for auto-detect
  },
});

const loading = ref(false);
const error = ref(null);
const sideData = ref(null);

// Chart dimensions
const chartWidth = 320;
const chartHeight = 120;
const padding = { top: 15, right: 50, bottom: 30, left: 50 };
const barHeight = 28;
const barGap = 12;

const xAxisTicks = [0, 25, 50, 75, 100];

const hasData = computed(() => sideData.value?.totalGames > 0);

const plotWidth = computed(() => chartWidth - padding.left - padding.right);

function getBarY(index) {
  return padding.top + index * (barHeight + barGap);
}

function getBarWidth(winRate) {
  return (winRate / 100) * plotWidth.value;
}

function getXPosition(value) {
  return padding.left + (value / 100) * plotWidth.value;
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    sideData.value = await getSideStats(props.userId, props.mode);
  } catch (e) {
    console.error('Error loading side stats:', e);
    error.value = e?.message || 'Failed to load side statistics.';
    sideData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
watch(() => props.mode, load);
onMounted(load);
</script>

<style scoped>
.side-win-rate-container {
  width: 100%;
}

.side-win-rate-container :deep(.chart-card) {
  min-height: 200px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.side-win-rate-container :deep(.chart-card > div:last-child) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.loading, .error, .empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.error {
  color: var(--color-danger);
}

.chart-content {
  display: flex;
  flex-direction: column;
  justify-content: center;
  flex: 1;
}

.bar-chart {
  width: 100%;
  height: auto;
}

.bar-label {
  font-size: 0.8rem;
  fill: var(--color-text);
  font-weight: 500;
}

.bar {
  rx: 4;
  ry: 4;
}

.blue-bar {
  fill: #3b82f6;
}

.red-bar {
  fill: #ef4444;
}

.bar-value {
  font-size: 0.75rem;
  fill: var(--color-text);
  font-weight: 600;
}

.axis-label {
  font-size: 0.65rem;
  fill: var(--color-text-muted);
}

.summary-text {
  margin-top: 0.75rem;
  font-size: 0.8rem;
  color: var(--color-text-muted);
  text-align: center;
  line-height: 1.5;
}

.summary-text strong {
  color: var(--color-text);
}

@media (max-width: 1200px) {
  .side-win-rate-container {
    max-width: 100%;
  }
}
</style>

