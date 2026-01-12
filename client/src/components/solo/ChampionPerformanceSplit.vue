<template>
  <div class="champion-performance-container">
    <div v-if="loading" class="champion-loading">Loading champion data…</div>
    <div v-else-if="error" class="champion-error">{{ error }}</div>
    <div v-else-if="!hasData" class="champion-empty">No champion data available.</div>

    <ChartCard v-else title="Champion Performance Split">
      <div class="champion-content">
        <!-- Grouped Bar Chart -->
        <div class="bar-chart-container">
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

            <!-- Grouped bars for each champion -->
            <g class="bars">
              <g v-for="(champ, champIndex) in top5Champions" :key="champ.championId">
                <!-- Bars for each server within this champion group -->
                <g v-for="(server, serverIndex) in champ.servers" :key="server.serverName">
                  <rect
                    :x="getGroupedBarX(champIndex, serverIndex, champ.servers.length)"
                    :y="getBarY(server.winrate)"
                    :width="barWidth"
                    :height="getBarHeight(server.winrate)"
                    :fill="getServerColor(server.serverName)"
                    class="bar"
                    @mouseenter="setHoveredBar(champ.championName, server)"
                    @mouseleave="hoveredBar = null"
                  />
                  <!-- Winrate label on top of bar -->
                  <text
                    :x="getGroupedBarX(champIndex, serverIndex, champ.servers.length) + barWidth / 2"
                    :y="getBarY(server.winrate) - 6"
                    text-anchor="middle"
                    class="winrate-label">
                    {{ server.winrate.toFixed(0) }}
                  </text>
                </g>
                <!-- Champion image below the group -->
                <image
                  :x="getChampionLabelX(champIndex, champ.servers.length) - 20"
                  :y="chartHeight - padding.bottom + 4"
                  width="40"
                  height="40"
                  :href="getChampionImageUrl(champ.championName)"
                  class="champion-image"
                  :aria-label="champ.championName"
                />
              </g>
            </g>

            <!-- Legend -->
            <g class="legend" :transform="`translate(${chartWidth / 2 - 100}, ${chartHeight - 10})`">
              <g v-for="(gamerName, i) in gamerNames" :key="gamerName"
                :transform="`translate(${i * 120}, 0)`">
                <rect :fill="getColorByGamerName(gamerName)" width="14" height="14" rx="2" />
                <text x="20" y="11" class="legend-text">{{ gamerName }}</text>
              </g>
            </g>
          </svg>

          <!-- Hover tooltip -->
          <div v-if="hoveredBar" class="tooltip">
            <strong>{{ hoveredBar.championName }}</strong><br>
            Server: {{ hoveredBar.server.serverName }}<br>
            Games: {{ hoveredBar.server.gamesPlayed }}<br>
            Wins: {{ hoveredBar.server.wins }}<br>
            Winrate: {{ hoveredBar.server.winrate.toFixed(1) }}%
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from '@/components/shared/ChartCard.vue';
import { getChampionPerformance } from '@/api/solo.js';
import { sortGamerNames, getGamerColor as getGamerColorUtil } from '@/composables/useGamerColors.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const championData = ref(null);
const hoveredBar = ref(null);

// Chart dimensions - match RadarChart aspect ratio (400x400)
const chartWidth = 600;
const chartHeight = 400;
const padding = { top: 20, right: 20, bottom: 70, left: 50 }; // Increased bottom padding for champion images
const barWidth = 35;
const barGap = 5; // Gap between bars in a group
const groupGap = 20; // Gap between champion groups
const yAxisLabels = [0, 25, 50, 75, 100];

const hasData = computed(() => championData.value?.champions?.length > 0);

// Get total games for a champion across all servers
function getTotalGames(champion) {
  return champion.servers.reduce((sum, s) => sum + s.gamesPlayed, 0);
}

// Top 5 champions by total games played
const top5Champions = computed(() => {
  if (!championData.value?.champions) return [];

  const champions = [...championData.value.champions];
  return champions
    .sort((a, b) => getTotalGames(b) - getTotalGames(a))
    .slice(0, 5);
});

// Get unique gamer names from the data (sorted alphabetically for consistent color mapping)
const gamerNames = computed(() => {
  if (!championData.value?.champions || championData.value.champions.length === 0) return [];
  const names = [];
  championData.value.champions.forEach(champ => {
    champ.servers.forEach(server => {
      if (server.gamerName) {
        names.push(server.gamerName);
      }
    });
  });
  return sortGamerNames(names);
});

// Chart helper functions
function getYPosition(winrate) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return padding.top + plotHeight * (1 - winrate / 100);
}

function getBarY(winrate) {
  return getYPosition(winrate);
}

function getBarHeight(winrate) {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return plotHeight * (winrate / 100);
}

// Calculate X position for a bar in a grouped bar chart
function getGroupedBarX(championIndex, serverIndex, serversCount) {
  const plotWidth = chartWidth - padding.left - padding.right;
  const totalChampions = top5Champions.value.length;

  // Width of one champion group (all its server bars + gaps between them)
  const groupWidth = serversCount * barWidth + (serversCount - 1) * barGap;

  // Total width needed for all groups and gaps
  const totalGroupsWidth = totalChampions * groupWidth + (totalChampions - 1) * groupGap;

  // Starting X position (centered)
  const startX = padding.left + (plotWidth - totalGroupsWidth) / 2;

  // X position of this champion group
  const groupX = startX + championIndex * (groupWidth + groupGap);

  // X position of this specific bar within the group
  return groupX + serverIndex * (barWidth + barGap);
}

// Calculate X position for champion label (centered under the group)
function getChampionLabelX(championIndex, serversCount) {
  const groupWidth = serversCount * barWidth + (serversCount - 1) * barGap;
  const plotWidth = chartWidth - padding.left - padding.right;
  const totalChampions = top5Champions.value.length;
  const totalGroupsWidth = totalChampions * groupWidth + (totalChampions - 1) * groupGap;
  const startX = padding.left + (plotWidth - totalGroupsWidth) / 2;
  const groupX = startX + championIndex * (groupWidth + groupGap);
  return groupX + groupWidth / 2;
}

// Get color based on gamer name (alphabetically sorted)
function getColorByGamerName(gamerName) {
  return getGamerColorUtil(gamerName, gamerNames.value);
}

function getServerColor(serverName) {
  // Map by gamer name to ensure consistent colors
  const gamerName = championData.value?.champions
    ?.flatMap(c => c.servers)
    ?.find(s => s.serverName === serverName)?.gamerName;

  if (!gamerName) return '#f59e0b';
  return getColorByGamerName(gamerName);
}

function setHoveredBar(championName, server) {
  hoveredBar.value = { championName, server };
}

// Get champion image URL from Data Dragon CDN
// Using the latest version (14.24.1) - you may want to make this dynamic
function getChampionImageUrl(championName) {
  // Data Dragon uses specific champion keys that sometimes differ from display names
  // Most champions use their name, but some have special cases
  const championKey = championName.replace(/['\s]/g, ''); // Remove apostrophes and spaces
  const version = '14.24.1'; // Latest LoL version
  return `https://ddragon.leagueoflegends.com/cdn/${version}/img/champion/${championKey}.png`;
}

// Load data
async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    championData.value = await getChampionPerformance(props.userId);
  } catch (e) {
    console.error('Error loading champion performance data:', e);
    error.value = e?.message || 'Failed to load champion performance data.';
    championData.value = null;
  } finally {
    loading.value = false;
  }
}

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
  padding-top: 1rem;
  overflow: visible;
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
  max-width: 600px;
  height: 400px;
  display: block;
}

.bar {
  cursor: pointer;
  transition: opacity 0.2s;
}

.bar:hover {
  opacity: 0.8;
}

.axis-label {
  fill: var(--color-text);
  font-size: 12px;
}

.champion-label {
  fill: var(--color-text);
  font-size: 12px;
  font-weight: 600;
}

.champion-image {
  border-radius: 50%;
  cursor: pointer;
  transition: transform 0.2s;
}

.champion-image:hover {
  transform: scale(1.1);
}

.winrate-label {
  fill: var(--color-text);
  font-weight: 600;
  font-size: 11px;
}

.legend-text {
  fill: var(--color-text);
  font-size: 12px;
  font-weight: 500;
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

