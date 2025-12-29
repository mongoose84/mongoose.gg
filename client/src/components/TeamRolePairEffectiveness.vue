<template>
  <div class="team-role-pair-container">
    <div v-if="loading" class="role-loading">Loading role pair analysis…</div>
    <div v-else-if="error" class="role-error">{{ error }}</div>
    <div v-else-if="!hasData" class="role-empty">
      Not enough data for role pair analysis.
      <span class="requirement-hint">(Minimum: 3 games together)</span>
    </div>

    <ChartCard v-else title="🎯 Player Synergy Matrix">
      <div class="role-content">
        <!-- Heatmap Grid using player pairs -->
        <div class="heatmap-container">
          <svg :viewBox="`0 0 ${heatmapSize} ${heatmapSize}`" class="heatmap">
            <!-- Column headers (all players) -->
            <text v-for="(player, i) in players" :key="'col-' + i"
              :x="cellSize + cellSize * i + cellSize / 2" :y="20" text-anchor="middle" class="header-label">
              {{ getShortName(player.name) }}
            </text>
            <text v-for="(player, i) in players" :key="'col-role-' + i"
              :x="cellSize + cellSize * i + cellSize / 2" :y="32" text-anchor="middle" class="role-label">
              {{ getRoleShort(player.role) }}
            </text>
            <!-- Row headers (all players) -->
            <text v-for="(player, i) in players" :key="'row-' + i"
              :x="cellSize - 5" :y="cellSize + cellSize * i + cellSize / 2 - 4" text-anchor="end" class="header-label">
              {{ getShortName(player.name) }}
            </text>
            <text v-for="(player, i) in players" :key="'row-role-' + i"
              :x="cellSize - 5" :y="cellSize + cellSize * i + cellSize / 2 + 8" text-anchor="end" class="role-label">
              {{ getRoleShort(player.role) }}
            </text>
            <!-- Cells -->
            <g v-for="(cell, i) in heatmapCells" :key="'cell-' + i">
              <rect :x="cell.x" :y="cell.y" :width="cellSize - 4" :height="cellSize - 4" rx="4" :class="cell.class" />
              <text v-if="cell.hasData" :x="cell.x + cellSize / 2 - 2" :y="cell.y + cellSize / 2"
                text-anchor="middle" dominant-baseline="middle" class="cell-value">
                {{ cell.winRate }}%
              </text>
            </g>
          </svg>
        </div>

        <!-- Legend -->
        <div class="legend">
          <span class="legend-item"><span class="swatch low"></span>&lt;45%</span>
          <span class="legend-item"><span class="swatch mid"></span>45-55%</span>
          <span class="legend-item"><span class="swatch high"></span>&gt;55%</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamSynergy from '@/assets/getTeamSynergy.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const synergyData = ref(null);

const cellSize = 72;

const hasData = computed(() => synergyData.value?.playerPairs?.length > 0);

const players = computed(() => synergyData.value?.players || []);

const heatmapSize = computed(() => cellSize * (players.value.length + 1));

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

function getRoleShort(role) {
  const roleMap = {
    'Top': 'TOP',
    'Jungle': 'JNG',
    'Mid': 'MID',
    'Bot': 'BOT',
    'Support': 'SUP'
  };
  return roleMap[role] || role;
}

const heatmapCells = computed(() => {
  if (!synergyData.value?.playerPairs || players.value.length === 0) return [];

  // Build a map of player pairs to their synergy data
  const pairMap = {};
  synergyData.value.playerPairs.forEach(p => {
    const key1 = `${p.player1}|${p.player2}`;
    const key2 = `${p.player2}|${p.player1}`;
    pairMap[key1] = p;
    pairMap[key2] = p;
  });

  const cells = [];
  const playerList = players.value;

  // Create full matrix (upper triangle has data, diagonal and lower are empty/disabled)
  for (let row = 0; row < playerList.length; row++) {
    for (let col = 0; col < playerList.length; col++) {
      if (col <= row) {
        // Diagonal or lower triangle - show as disabled
        cells.push({
          x: cellSize * col + cellSize + 2,
          y: cellSize * row + cellSize + 2,
          winRate: '',
          hasData: false,
          class: 'cell-disabled'
        });
      } else {
        // Upper triangle - show pair data
        const pair = pairMap[`${playerList[row].name}|${playerList[col].name}`];
        const hasPairData = pair && pair.gamesPlayed > 0;
        let cellClass = 'cell-empty';
        if (hasPairData) {
          cellClass = pair.winRate >= 55 ? 'cell-high' : pair.winRate >= 45 ? 'cell-mid' : 'cell-low';
        }
        cells.push({
          x: cellSize * col + cellSize + 2,
          y: cellSize * row + cellSize + 2,
          winRate: hasPairData ? Math.round(pair.winRate) : '',
          hasData: hasPairData,
          class: cellClass
        });
      }
    }
  }
  return cells;
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    synergyData.value = await getTeamSynergy(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load synergy data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-role-pair-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.team-role-pair-container :deep(.chart-card) { height: 100%; display: flex; flex-direction: column; }
.team-role-pair-container :deep(.chart-card-body) { flex: 1; display: flex; flex-direction: column; justify-content: center; }
.role-loading, .role-error, .role-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.role-error { color: var(--color-danger); }
.role-empty .requirement-hint { display: block; margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.7; }
.role-content { padding: 1rem 0 0.5rem 0; display: flex; flex-direction: column; gap: 1rem; align-items: center; justify-content: center; width: 100%; }
.heatmap-container { display: flex; justify-content: center; width: 100%; }
.heatmap { width: 100%; max-width: 600px; height: auto; }
.header-label { fill: var(--color-text); font-size: 12px; font-weight: 600; }
.role-label { fill: var(--color-text-muted); font-size: 11px; font-weight: 500; }
.cell-empty { fill: var(--color-bg-elev); }
.cell-disabled { fill: var(--color-bg-base); opacity: 0.3; }
.cell-low { fill: rgba(239, 68, 68, 0.6); }
.cell-mid { fill: rgba(245, 158, 11, 0.6); }
.cell-high { fill: rgba(34, 197, 94, 0.6); }
.cell-value { fill: var(--color-text); font-size: 14px; font-weight: 600; }
.legend { display: flex; justify-content: center; gap: 1rem; font-size: 0.75rem; }
.legend-item { display: flex; align-items: center; gap: 0.25rem; }
.swatch { width: 10px; height: 10px; border-radius: 2px; }
.swatch.low { background: rgba(239, 68, 68, 0.6); }
.swatch.mid { background: rgba(245, 158, 11, 0.6); }
.swatch.high { background: rgba(34, 197, 94, 0.6); }
</style>

