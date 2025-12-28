<template>
  <div class="champion-performance-container">
    <div v-if="loading" class="champion-loading">Loading champion data…</div>
    <div v-else-if="error" class="champion-error">{{ error }}</div>
    <div v-else-if="!hasData" class="champion-empty">No champion data available.</div>

    <ChartCard v-else title="Champion Performance Split">
      <div class="champion-content">
        <!-- Filter Buttons -->
        <div class="filter-buttons">
          <button
            :class="['filter-btn', { active: filterMode === 'top5' }]"
            @click="filterMode = 'top5'"
          >
            Top 5 Played
          </button>
          <button
            :class="['filter-btn', { active: filterMode === 'delta' }]"
            @click="filterMode = 'delta'"
          >
            Biggest Winrate Delta
          </button>
        </div>

        <!-- Champion Button Splits -->
        <div class="champion-buttons">
          <button
            v-for="champ in filteredChampions"
            :key="champ.championId"
            :class="['champion-btn', { active: selectedChampionId === champ.championId }]"
            @click="selectedChampionId = champ.championId"
          >
            {{ champ.championName }}
          </button>
        </div>

        <!-- Bar Chart -->
        <div v-if="selectedChampion" class="bar-chart-container">
          <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="bar-chart" aria-label="Champion winrate comparison">
            <!-- Y-axis labels (winrate %) -->
            <g class="y-labels">
              <text v-for="(label, i) in yAxisLabels" :key="'y-' + i"
                :x="padding.left - 8" :y="getYPosition(label)" 
                text-anchor="end" dominant-baseline="middle" class="axis-label">
                {{ label }}%
              </text>
            </g>

            <!-- Grid lines -->
            <g class="grid">
              <line v-for="label in yAxisLabels" :key="'grid-' + label"
                :x1="padding.left" :x2="chartWidth - padding.right"
                :y1="getYPosition(label)" :y2="getYPosition(label)"
                stroke="var(--color-border)" stroke-width="1" opacity="0.3" />
            </g>

            <!-- Bars for each server -->
            <g class="bars">
              <g v-for="(server, i) in selectedChampion.servers" :key="server.serverName">
                <rect
                  :x="getBarX(i)"
                  :y="getBarY(server.winrate)"
                  :width="barWidth"
                  :height="getBarHeight(server.winrate)"
                  :fill="getServerColor(server.serverName)"
                  class="bar"
                  @mouseenter="hoveredServer = server"
                  @mouseleave="hoveredServer = null"
                />
                <!-- Server label -->
                <text
                  :x="getBarX(i) + barWidth / 2"
                  :y="chartHeight - padding.bottom + 16"
                  text-anchor="middle"
                  class="server-label">
                  {{ server.serverName }}
                </text>
                <!-- Winrate label on top of bar -->
                <text
                  :x="getBarX(i) + barWidth / 2"
                  :y="getBarY(server.winrate) - 6"
                  text-anchor="middle"
                  class="winrate-label">
                  {{ server.winrate.toFixed(1) }}%
                </text>
              </g>
            </g>
          </svg>

          <!-- Hover tooltip -->
          <div v-if="hoveredServer" class="tooltip">
            <strong>{{ hoveredServer.serverName }}</strong><br>
            Games: {{ hoveredServer.gamesPlayed }}<br>
            Wins: {{ hoveredServer.wins }}<br>
            Winrate: {{ hoveredServer.winrate.toFixed(1) }}%
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getChampionPerformance from '@/assets/getChampionPerformance.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const championData = ref(null);
const selectedChampionId = ref(null);
const filterMode = ref('top5'); // 'top5' or 'delta'
const hoveredServer = ref(null);

// Chart dimensions
const chartWidth = 400;
const chartHeight = 280;
const padding = { top: 20, right: 20, bottom: 50, left: 50 };
const barWidth = 80;
const yAxisLabels = [0, 25, 50, 75, 100];

const hasData = computed(() => championData.value?.champions?.length > 0);

// Get total games for a champion across all servers
function getTotalGames(champion) {
  return champion.servers.reduce((sum, s) => sum + s.gamesPlayed, 0);
}

// Get winrate delta (difference between servers) for a champion
function getWinrateDelta(champion) {
  if (champion.servers.length < 2) return 0;
  const winrates = champion.servers.map(s => s.winrate);
  return Math.abs(Math.max(...winrates) - Math.min(...winrates));
}

// Filtered champions based on selected filter mode
const filteredChampions = computed(() => {
  if (!championData.value?.champions) return [];

  const champions = [...championData.value.champions];

  if (filterMode.value === 'top5') {
    // Top 5 by total games played
    return champions
      .sort((a, b) => getTotalGames(b) - getTotalGames(a))
      .slice(0, 5);
  } else {
    // Top 5 by biggest winrate delta
    return champions
      .filter(c => c.servers.length >= 2) // Only champions played on multiple servers
      .sort((a, b) => getWinrateDelta(b) - getWinrateDelta(a))
      .slice(0, 5);
  }
});

// Selected champion object
const selectedChampion = computed(() => {
  if (!selectedChampionId.value || !championData.value?.champions) return null;
  return championData.value.champions.find(c => c.championId === selectedChampionId.value);
});

// Chart helper functions
function getYPosition(winrate) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - winrate / 100);
}

function getBarX(index) {
  const plotWidth = chartWidth - padding.left - padding.right;
  const totalBars = selectedChampion.value?.servers.length || 1;
  const spacing = (plotWidth - totalBars * barWidth) / (totalBars + 1);
  return padding.left + spacing + index * (barWidth + spacing);
}

function getBarY(winrate) {
  return getYPosition(winrate);
}

function getBarHeight(winrate) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return plotHeight * (winrate / 100);
}

function getServerColor(serverName) {
  const colors = {
    'EUW': 'var(--color-primary)',
    'EUNE': 'var(--color-success)',
  };
  return colors[serverName] || '#f59e0b';
}

// Load data
async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    championData.value = await getChampionPerformance(props.userId);

    // Auto-select first champion if available
    if (filteredChampions.value.length > 0 && !selectedChampionId.value) {
      selectedChampionId.value = filteredChampions.value[0].championId;
    }
  } catch (e) {
    console.error('Error loading champion performance data:', e);
    error.value = e?.message || 'Failed to load champion performance data.';
    championData.value = null;
  } finally {
    loading.value = false;
  }
}

// Watch for filter mode changes and update selection
watch(filterMode, () => {
  if (filteredChampions.value.length > 0) {
    selectedChampionId.value = filteredChampions.value[0].championId;
  }
});

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.champion-performance-container {
  width: 100%;
  max-width: 100%;
}

/* Override ChartCard max-width for champion performance */
.champion-performance-container :deep(.chart-card) {
  max-width: 100%;
  height: auto;
  min-height: 400px;
}

.champion-loading,
.champion-error,
.champion-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.champion-error {
  color: var(--color-danger);
}

.champion-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-top: 1.5rem;
  overflow: visible;
}

.filter-buttons {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}

.filter-btn {
  padding: 0.4rem 0.8rem;
  border: 1px solid var(--color-border);
  background: var(--color-bg);
  color: var(--color-text);
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.85rem;
  transition: all 0.2s;
}

.filter-btn:hover {
  background: var(--color-bg-elev);
}

.filter-btn.active {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
}

.champion-buttons {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
  flex-wrap: wrap;
}

.champion-btn {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: var(--color-bg);
  color: var(--color-text);
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s;
  white-space: nowrap;
}

.champion-btn:hover {
  background: var(--color-bg-elev);
  border-color: var(--color-primary);
}

.champion-btn.active {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
  font-weight: 600;
}

.bar-chart-container {
  flex: 1;
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

.bar-chart {
  width: 100%;
  max-width: 400px;
  height: 280px;
  display: block;
}

.bar {
  cursor: pointer;
  transition: opacity 0.2s;
}

.bar:hover {
  opacity: 0.8;
}

.axis-label,
.server-label,
.winrate-label {
  fill: var(--color-text);
  font-size: 12px;
}

.server-label {
  font-weight: 600;
}

.winrate-label {
  font-weight: 600;
  font-size: 13px;
}

.tooltip {
  position: absolute;
  top: 10px;
  right: 10px;
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.5rem 0.75rem;
  font-size: 0.85rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  pointer-events: none;
}
</style>

