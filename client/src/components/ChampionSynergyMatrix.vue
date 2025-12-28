<template>
  <div class="champion-synergy-matrix">
    <div class="matrix-header">
      <h3>Champion Synergy Matrix</h3>
      <p class="subtitle">What should we pick together?</p>
    </div>

    <div v-if="loading" class="matrix-loading">Loading synergy data…</div>
    <div v-else-if="error" class="matrix-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matrix-empty">Not enough duo games for synergy analysis yet.</div>
    
    <div v-else class="matrix-content">
      <!-- Heatmap Table -->
      <div class="heatmap-container">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th class="corner-cell"></th>
              <th v-for="champ in player2Champions" :key="'header-' + champ.championId" class="champion-header">
                {{ champ.championName }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="champ1 in player1Champions" :key="'row-' + champ1.championId">
              <th class="champion-header">{{ champ1.championName }}</th>
              <td 
                v-for="champ2 in player2Champions" 
                :key="'cell-' + champ1.championId + '-' + champ2.championId"
                :class="['synergy-cell', getWinRateClass(getSynergyData(champ1.championId, champ2.championId))]"
                :title="getCellTooltip(champ1, champ2)">
                <div class="cell-content">
                  <div class="winrate">{{ getSynergyWinRate(champ1.championId, champ2.championId) }}</div>
                  <div class="games">{{ getSynergyGames(champ1.championId, champ2.championId) }}g</div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import getChampionSynergy from '@/assets/getChampionSynergy.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true
  },
  gamers: {
    type: Array,
    required: true
  }
});

const loading = ref(false);
const error = ref(null);
const synergyData = ref(null);

// Fetch synergy data
async function loadSynergyData() {
  loading.value = true;
  error.value = null;

  try {
    const data = await getChampionSynergy(props.userId);
    synergyData.value = data;
  } catch (e) {
    error.value = e?.message || 'Failed to load champion synergy data.';
    console.error('Error loading synergy data:', e);
  } finally {
    loading.value = false;
  }
}

const hasData = computed(() => {
  return synergyData.value?.synergies && synergyData.value.synergies.length > 0;
});

// Build synergy lookup map
const synergyMap = computed(() => {
  if (!hasData.value) return new Map();
  
  const map = new Map();
  synergyData.value.synergies.forEach(s => {
    const key = `${s.championId1}-${s.championId2}`;
    map.set(key, s);
  });
  return map;
});

// Get unique champions for each player
const player1Champions = computed(() => {
  if (!hasData.value) return [];
  
  const champMap = new Map();
  synergyData.value.synergies.forEach(s => {
    if (!champMap.has(s.championId1)) {
      champMap.set(s.championId1, {
        championId: s.championId1,
        championName: s.championName1,
        totalGames: 0
      });
    }
    champMap.get(s.championId1).totalGames += s.gamesPlayed;
  });
  
  const champs = Array.from(champMap.values());
  return sortChampions(champs);
});

const player2Champions = computed(() => {
  if (!hasData.value) return [];
  
  const champMap = new Map();
  synergyData.value.synergies.forEach(s => {
    if (!champMap.has(s.championId2)) {
      champMap.set(s.championId2, {
        championId: s.championId2,
        championName: s.championName2,
        totalGames: 0
      });
    }
    champMap.get(s.championId2).totalGames += s.gamesPlayed;
  });
  
  const champs = Array.from(champMap.values());
  return sortChampions(champs);
});

function sortChampions(champions) {
  // Sort by most played and limit to top 5 champions for readability
  const sorted = [...champions];
  sorted.sort((a, b) => b.totalGames - a.totalGames);
  return sorted.slice(0, 5);
}

function getSynergyData(championId1, championId2) {
  const key = `${championId1}-${championId2}`;
  return synergyMap.value.get(key) || null;
}

function getSynergyWinRate(championId1, championId2) {
  const data = getSynergyData(championId1, championId2);
  if (!data || data.gamesPlayed === 0) return '-';
  return `${data.winrate.toFixed(0)}%`;
}

function getSynergyGames(championId1, championId2) {
  const data = getSynergyData(championId1, championId2);
  return data ? data.gamesPlayed : 0;
}

function getWinRateClass(data) {
  if (!data || data.gamesPlayed === 0) return 'no-data';
  if (data.gamesPlayed < 3) return 'low-sample';
  if (data.winrate >= 60) return 'wr-excellent';
  if (data.winrate >= 55) return 'wr-good';
  if (data.winrate >= 50) return 'wr-neutral';
  if (data.winrate >= 45) return 'wr-bad';
  return 'wr-terrible';
}

function getCellTooltip(champ1, champ2) {
  const data = getSynergyData(champ1.championId, champ2.championId);
  if (!data) return `${champ1.championName} + ${champ2.championName}: No games`;
  return `${champ1.championName} + ${champ2.championName}\n${data.wins}W ${data.losses}L (${data.winrate.toFixed(1)}%)`;
}

onMounted(() => {
  loadSynergyData();
});

watch(() => props.userId, () => {
  loadSynergyData();
});
</script>

<style scoped>
.champion-synergy-matrix {
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px 0 rgba(44, 11, 58, 0.08);
}

.matrix-header {
  margin-bottom: 1rem;
}

.matrix-header h3 {
  margin: 0 0 0.25rem 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text);
}

.subtitle {
  margin: 0;
  font-size: 0.9rem;
  color: var(--color-text-muted);
  font-style: italic;
}

.matrix-loading,
.matrix-error,
.matrix-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.matrix-error {
  color: var(--color-danger);
}

.heatmap-container {
  overflow-x: auto;
}

.heatmap-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8rem;
}

.corner-cell {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
}

.champion-header {
  background: var(--color-bg);
  padding: 0.5rem;
  text-align: center;
  font-weight: 600;
  border: 1px solid var(--color-border);
  font-size: 0.75rem;
  min-width: 70px;
  color: var(--color-text);
}

.synergy-cell {
  padding: 0.25rem;
  text-align: center;
  border: 1px solid var(--color-border);
  cursor: help;
  transition: transform 0.1s, box-shadow 0.1s;
}

.synergy-cell:hover {
  transform: scale(1.05);
  z-index: 1;
  box-shadow: 0 4px 12px rgba(124, 58, 237, 0.3);
}

.cell-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.winrate {
  font-weight: 600;
  font-size: 0.85rem;
}

.games {
  font-size: 0.7rem;
  opacity: 0.8;
}

/* Win rate color classes */
.no-data {
  background: var(--color-bg);
  color: var(--color-text-muted);
}

.low-sample {
  background: rgba(124, 58, 237, 0.15);
  color: var(--color-text-muted);
}

.wr-excellent {
  background: var(--color-success);
  color: var(--color-text);
}

.wr-good {
  background: rgba(33, 156, 78, 0.7);
  color: var(--color-text);
}

.wr-neutral {
  background: rgba(124, 58, 237, 0.4);
  color: var(--color-text);
}

.wr-bad {
  background: rgba(168, 66, 66, 0.6);
  color: var(--color-text);
}

.wr-terrible {
  background: var(--color-danger);
  color: var(--color-text);
}
</style>


