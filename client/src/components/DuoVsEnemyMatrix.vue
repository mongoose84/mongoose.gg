<template>
  <div class="duo-vs-enemy-matrix">
    <div class="matrix-header">
      <h3>Duo vs Enemy Champions</h3>
      <p class="subtitle">Draft prep for ranked & clash</p>
    </div>

    <div v-if="loading" class="matrix-loading">Loading matchup data…</div>
    <div v-else-if="error" class="matrix-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matrix-empty">Not enough duo games for matchup analysis yet.</div>
    
    <div v-else class="matrix-content">
      <!-- Duo Champion Selector -->
      <div class="duo-selector">
        <label>Select Duo Combo:</label>
        <select v-model="selectedDuoCombo" class="duo-combo-select">
          <option v-for="combo in duoCombos" :key="combo.key" :value="combo.key">
            {{ combo.label }} ({{ combo.games }}g, {{ combo.winrate }}%)
          </option>
        </select>
      </div>

      <!-- Enemy Lane Selector -->
      <div v-if="relevantLanes.length > 0" class="lane-selector">
        <div class="lane-selector-label">Enemy Lane:</div>
        <button
          v-for="lane in relevantLanes"
          :key="lane"
          :class="['lane-btn', { active: selectedLane === lane }]"
          @click="selectedLane = lane">
          {{ lane }}
        </button>
      </div>

      <!-- Heatmap Table -->
      <div v-if="selectedDuoCombo && selectedLane" class="heatmap-container">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th class="corner-cell">vs {{ selectedLane }}</th>
              <th class="winrate-header">Win Rate</th>
              <th class="games-header">Games</th>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="enemy in enemyChampions" 
              :key="enemy.championId"
              :class="['enemy-row', getWinRateClass(enemy)]">
              <td class="champion-name">{{ enemy.championName }}</td>
              <td class="winrate-cell">
                <div class="winrate-bar-container">
                  <div class="winrate-bar" :style="{ width: enemy.winrate + '%' }"></div>
                  <span class="winrate-text">{{ enemy.winrate.toFixed(0) }}%</span>
                </div>
              </td>
              <td class="games-cell">{{ enemy.gamesPlayed }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import getDuoVsEnemy from '@/assets/getDuoVsEnemy.js';

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
const matchupData = ref(null);
const selectedDuoCombo = ref(null);
const selectedLane = ref(null);

// Fetch matchup data
async function loadMatchupData() {
  loading.value = true;
  error.value = null;

  try {
    const data = await getDuoVsEnemy(props.userId);
    matchupData.value = data;
    
    // Auto-select first duo combo and lane
    if (duoCombos.value.length > 0) {
      selectedDuoCombo.value = duoCombos.value[0].key;
    }
    if (availableLanes.value.length > 0) {
      selectedLane.value = availableLanes.value[0];
    }
  } catch (e) {
    error.value = e?.message || 'Failed to load duo vs enemy data.';
    console.error('Error loading matchup data:', e);
  } finally {
    loading.value = false;
  }
}

const hasData = computed(() => {
  return matchupData.value?.matchups && matchupData.value.matchups.length > 0;
});

// Get unique duo combinations
const duoCombos = computed(() => {
  if (!hasData.value) return [];
  
  const comboMap = new Map();
  matchupData.value.matchups.forEach(m => {
    const key = `${m.duoChampionId1}-${m.duoChampionId2}`;
    if (!comboMap.has(key)) {
      comboMap.set(key, {
        key,
        label: `${m.duoChampionName1} + ${m.duoChampionName2}`,
        championId1: m.duoChampionId1,
        championName1: m.duoChampionName1,
        championId2: m.duoChampionId2,
        championName2: m.duoChampionName2,
        games: 0,
        wins: 0
      });
    }
    const combo = comboMap.get(key);
    combo.games += m.gamesPlayed;
    combo.wins += m.wins;
  });
  
  const combos = Array.from(comboMap.values());
  combos.forEach(c => {
    c.winrate = c.games > 0 ? ((c.wins / c.games) * 100).toFixed(0) : 0;
  });
  
  // Sort by games played
  combos.sort((a, b) => b.games - a.games);
  return combos;
});

// Detect which lanes/roles the duo plays for the selected combo
const duoLanes = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value) return new Set();

  const lanes = new Set();
  const matchups = matchupData.value.matchups
    .filter(m => `${m.duoChampionId1}-${m.duoChampionId2}` === selectedDuoCombo.value);

  if (matchups.length > 0) {
    // Get the duo's own lanes from the first matchup (they should be consistent)
    const firstMatch = matchups[0];
    if (firstMatch.duoLane1 && firstMatch.duoLane1 !== 'UNKNOWN') {
      lanes.add(firstMatch.duoLane1);
    }
    if (firstMatch.duoLane2 && firstMatch.duoLane2 !== 'UNKNOWN') {
      lanes.add(firstMatch.duoLane2);
    }
  }

  return lanes;
});

// Get all available enemy lanes for selected duo combo
const availableLanes = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value) return [];

  const lanes = new Set();
  matchupData.value.matchups
    .filter(m => `${m.duoChampionId1}-${m.duoChampionId2}` === selectedDuoCombo.value)
    .forEach(m => lanes.add(m.enemyLane));

  return Array.from(lanes).sort();
});

// Filter to only show enemy lanes that are relevant to the duo's roles
// Logic:
// - If duo plays BOTTOM + UTILITY (bot lane duo), show all enemy lanes (they face everyone)
// - If duo plays TOP + JUNGLE, show TOP, MIDDLE, JUNGLE (lanes they interact with)
// - If duo plays MIDDLE + JUNGLE, show TOP, MIDDLE, BOTTOM (lanes they interact with)
const relevantLanes = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value) return [];

  const allEnemyLanes = availableLanes.value;
  const myLanes = duoLanes.value;

  // If we don't know the duo's lanes, show all available enemy lanes
  if (myLanes.size === 0) {
    return allEnemyLanes;
  }

  // Bot lane duo (BOTTOM + UTILITY) - show all lanes (they face everyone)
  if (myLanes.has('BOTTOM') && myLanes.has('UTILITY')) {
    return allEnemyLanes;
  }

  // For other duos, show only lanes they directly interact with
  // This means: their own lanes + adjacent lanes
  const relevantEnemyLanes = new Set();

  myLanes.forEach(lane => {
    // Always include the same lane (direct matchup)
    if (allEnemyLanes.includes(lane)) {
      relevantEnemyLanes.add(lane);
    }

    // Add adjacent/interacting lanes based on role
    if (lane === 'TOP') {
      if (allEnemyLanes.includes('JUNGLE')) relevantEnemyLanes.add('JUNGLE');
      if (allEnemyLanes.includes('MIDDLE')) relevantEnemyLanes.add('MIDDLE');
    } else if (lane === 'JUNGLE') {
      // Jungle interacts with all lanes
      allEnemyLanes.forEach(l => relevantEnemyLanes.add(l));
    } else if (lane === 'MIDDLE') {
      if (allEnemyLanes.includes('JUNGLE')) relevantEnemyLanes.add('JUNGLE');
      if (allEnemyLanes.includes('TOP')) relevantEnemyLanes.add('TOP');
      if (allEnemyLanes.includes('BOTTOM')) relevantEnemyLanes.add('BOTTOM');
    }
  });

  // If no relevant lanes found, show all (fallback)
  if (relevantEnemyLanes.size === 0) {
    return allEnemyLanes;
  }

  return Array.from(relevantEnemyLanes).sort();
});

// Get enemy champions for selected duo combo and lane
const enemyChampions = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value || !selectedLane.value) return [];

  const enemies = matchupData.value.matchups
    .filter(m =>
      `${m.duoChampionId1}-${m.duoChampionId2}` === selectedDuoCombo.value &&
      m.enemyLane === selectedLane.value
    )
    .map(m => ({
      championId: m.enemyChampionId,
      championName: m.enemyChampionName,
      gamesPlayed: m.gamesPlayed,
      wins: m.wins,
      losses: m.losses,
      winrate: m.winrate
    }));

  // Sort by games played
  enemies.sort((a, b) => b.gamesPlayed - a.gamesPlayed);

  // Limit to top 10 for readability
  return enemies.slice(0, 10);
});

function getWinRateClass(enemy) {
  if (enemy.gamesPlayed < 3) return 'low-sample';
  if (enemy.winrate >= 60) return 'wr-excellent';
  if (enemy.winrate >= 55) return 'wr-good';
  if (enemy.winrate >= 50) return 'wr-neutral';
  if (enemy.winrate >= 45) return 'wr-bad';
  return 'wr-terrible';
}

onMounted(() => {
  loadMatchupData();
});

watch(() => props.userId, () => {
  loadMatchupData();
});
</script>

<style scoped>
.duo-vs-enemy-matrix {
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

.duo-selector {
  margin-bottom: 1rem;
}

.duo-selector label {
  display: block;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text);
}

.duo-combo-select {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.875rem;
  background: var(--color-bg-elev);
  color: var(--color-text);
  cursor: pointer;
  transition: all 0.2s;
}

.duo-combo-select:hover,
.duo-combo-select:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 2px rgba(124, 58, 237, 0.25);
}

.duo-combo-select option {
  background: var(--color-bg-elev);
  color: var(--color-text);
}

.lane-selector {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  align-items: center;
}

.lane-selector-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text);
  margin-right: 0.25rem;
}

.lane-btn {
  padding: 0.5rem 1rem;
  border: 1px solid var(--color-border);
  background: var(--color-bg-elev);
  color: var(--color-text);
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.8rem;
  transition: all 0.2s;
}

.lane-btn:hover {
  background: var(--color-bg-hover);
  border-color: var(--color-primary);
}

.lane-btn.active {
  background: var(--color-primary);
  color: var(--color-text);
  border-color: var(--color-primary);
}

.heatmap-container {
  overflow-x: auto;
}

.heatmap-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.corner-cell,
.winrate-header,
.games-header {
  background: var(--color-bg);
  padding: 0.75rem;
  text-align: left;
  font-weight: 600;
  border: 1px solid var(--color-border);
  font-size: 0.8rem;
  color: var(--color-text);
}

.winrate-header {
  width: 60%;
}

.games-header {
  width: 15%;
  text-align: center;
}

.enemy-row {
  transition: background 0.2s;
}

.enemy-row:hover {
  background: var(--color-bg-hover);
}

.champion-name {
  padding: 0.75rem;
  font-weight: 500;
  border: 1px solid var(--color-border);
  color: var(--color-text);
}

.winrate-cell {
  padding: 0.75rem;
  border: 1px solid var(--color-border);
}

.winrate-bar-container {
  position: relative;
  height: 24px;
  background: var(--color-bg);
  border-radius: 4px;
  overflow: hidden;
  border: 1px solid var(--color-border);
}

.winrate-bar {
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  transition: width 0.3s, background 0.3s;
}

.winrate-text {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-weight: 600;
  font-size: 0.8rem;
  color: var(--color-text);
  z-index: 1;
}

.games-cell {
  padding: 0.75rem;
  text-align: center;
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
}

/* Win rate color classes for rows */
.wr-excellent .winrate-bar {
  background: var(--color-success);
}

.wr-good .winrate-bar {
  background: rgba(33, 156, 78, 0.7);
}

.wr-neutral .winrate-bar {
  background: rgba(124, 58, 237, 0.6);
}

.wr-bad .winrate-bar {
  background: rgba(168, 66, 66, 0.6);
}

.wr-terrible .winrate-bar {
  background: var(--color-danger);
}

.low-sample .winrate-bar {
  background: rgba(124, 58, 237, 0.2);
}

.low-sample .champion-name {
  opacity: 0.7;
}
</style>
