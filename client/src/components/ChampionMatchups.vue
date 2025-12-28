<template>
  <div class="champion-matchups-container">
    <div v-if="loading" class="matchups-loading">Loading matchup data…</div>
    <div v-else-if="error" class="matchups-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matchups-empty">No matchup data available.</div>

    <div v-else class="matchups-card">
      <h3 class="card-title">Champion Matchups</h3>
      
      <!-- Controls: Role Filter and Search -->
      <div class="controls">
        <div class="control-group">
          <label for="role-filter">Filter by Role:</label>
          <select id="role-filter" v-model="selectedRole" class="role-select">
            <option value="ALL">All Roles</option>
            <option value="TOP">Top</option>
            <option value="JUNGLE">Jungle</option>
            <option value="MIDDLE">Mid</option>
            <option value="BOTTOM">ADC</option>
            <option value="UTILITY">Support</option>
          </select>
        </div>

        <div class="control-group">
          <label for="opponent-search">Search Opponent:</label>
          <input
            id="opponent-search"
            v-model="opponentSearch"
            type="text"
            placeholder="e.g., Fizz, Yasuo..."
            class="search-input"
          />
        </div>
      </div>

      <!-- Search Results View (when searching for opponent) -->
      <div v-if="opponentSearch.trim()" class="search-results">
        <h4>Your Champions vs {{ opponentSearch }}</h4>
        <div v-if="searchResults.length === 0" class="no-results">
          No matchups found against "{{ opponentSearch }}"
        </div>
        <div v-else class="results-list">
          <div
            v-for="result in searchResults"
            :key="`${result.championName}-${result.role}`"
            class="result-item"
          >
            <div class="result-header">
              <img
                :src="getChampionImageUrl(result.championName)"
                :alt="result.championName"
                class="champion-icon"
              />
              <div class="result-info">
                <span class="champion-name">{{ result.championName }}</span>
                <span class="role-badge">{{ formatRole(result.role) }}</span>
              </div>
              <div class="result-stats">
                <span class="games">{{ result.gamesPlayed }}G</span>
                <span class="record">{{ result.wins }}W {{ result.losses }}L</span>
                <span class="winrate" :class="getWinrateClass(result.winrate)">
                  {{ result.winrate.toFixed(1) }}%
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Normal View (filtered by role) -->
      <div v-else class="matchups-list">
        <div
          v-for="matchup in filteredMatchups"
          :key="`${matchup.championName}-${matchup.role}`"
          class="matchup-card"
        >
          <!-- Champion Header -->
          <div class="matchup-header">
            <img
              :src="getChampionImageUrl(matchup.championName)"
              :alt="matchup.championName"
              class="champion-icon"
            />
            <div class="header-info">
              <div class="champion-title">
                <span class="champion-name">{{ matchup.championName }}</span>
                <span class="role-badge">{{ formatRole(matchup.role) }}</span>
              </div>
              <div class="overall-stats">
                <span class="games">{{ matchup.totalGames }}G</span>
                <span class="record">{{ matchup.totalWins }}W {{ matchup.totalGames - matchup.totalWins }}L</span>
                <span class="winrate" :class="getWinrateClass(matchup.winrate)">
                  {{ matchup.winrate.toFixed(1) }}%
                </span>
              </div>
            </div>
          </div>

          <!-- Low games warning -->
          <div v-if="matchup.totalGames < 5" class="warning">
            ⚠️ Low sample size ({{ matchup.totalGames }} games)
          </div>

          <!-- Opponents List -->
          <div class="opponents-list">
            <div class="opponents-header">
              <span>Opponent</span>
              <span>Record</span>
              <span>Winrate</span>
            </div>
            
            <div
              v-for="(opponent, index) in getVisibleOpponents(matchup)"
              :key="opponent.opponentChampionName"
              class="opponent-row"
            >
              <div class="opponent-info">
                <img
                  :src="getChampionImageUrl(opponent.opponentChampionName)"
                  :alt="opponent.opponentChampionName"
                  class="opponent-icon"
                />
                <span class="opponent-name">{{ opponent.opponentChampionName }}</span>
              </div>
              <div class="opponent-record">
                {{ opponent.wins }}W {{ opponent.losses }}L ({{ opponent.gamesPlayed }}G)
              </div>
              <div class="opponent-winrate" :class="getWinrateClass(opponent.winrate)">
                {{ opponent.winrate.toFixed(1) }}%
              </div>
            </div>

            <!-- Expand/Collapse Button -->
            <button
              v-if="matchup.opponents.length > 3"
              @click="toggleExpand(matchup)"
              class="expand-btn"
            >
              {{ isExpanded(matchup) ? 'Show Less' : `Show All (${matchup.opponents.length})` }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import getChampionMatchups from '@/assets/getChampionMatchups.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const matchupsData = ref(null);
const selectedRole = ref('ALL');
const opponentSearch = ref('');
const expandedMatchups = ref(new Set());

const hasData = computed(() => matchupsData.value?.matchups?.length > 0);

// Filtered matchups by selected role
const filteredMatchups = computed(() => {
  if (!matchupsData.value?.matchups) return [];

  let filtered = matchupsData.value.matchups;

  if (selectedRole.value !== 'ALL') {
    filtered = filtered.filter(m => m.role === selectedRole.value);
  }

  // Show top 5 champions for the selected role
  return filtered.slice(0, 5);
});

// Search results when looking for opponent
const searchResults = computed(() => {
  if (!opponentSearch.value.trim() || !matchupsData.value?.matchups) return [];

  const searchTerm = opponentSearch.value.toLowerCase();
  const results = [];

  console.log('Searching for:', searchTerm);
  console.log('Total matchups:', matchupsData.value.matchups.length);

  matchupsData.value.matchups.forEach(matchup => {
    console.log(`Checking matchup: ${matchup.championName} (${matchup.role}), opponents:`, matchup.opponents.length);
    matchup.opponents.forEach(opponent => {
      const opponentNameLower = opponent.opponentChampionName.toLowerCase();
      console.log(`  - Opponent: ${opponent.opponentChampionName} (${opponentNameLower})`);
      if (opponentNameLower.includes(searchTerm)) {
        console.log(`    ✓ MATCH FOUND!`);
        results.push({
          championName: matchup.championName,
          championId: matchup.championId,
          role: matchup.role,
          opponentName: opponent.opponentChampionName,
          gamesPlayed: opponent.gamesPlayed,
          wins: opponent.wins,
          losses: opponent.losses,
          winrate: opponent.winrate
        });
      }
    });
  });

  console.log('Search results:', results.length);

  // Sort by winrate descending
  return results.sort((a, b) => b.winrate - a.winrate);
});

// Helper functions
function formatRole(role) {
  const roleMap = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support',
    UNKNOWN: 'Other'
  };
  return roleMap[role] || role;
}

function getChampionImageUrl(championName) {
  const championKey = championName.replace(/['\s]/g, '');
  const version = '14.24.1';
  return `https://ddragon.leagueoflegends.com/cdn/${version}/img/champion/${championKey}.png`;
}

function getWinrateClass(winrate) {
  if (winrate >= 50) return 'positive';
  return 'negative';
}

function isExpanded(matchup) {
  return expandedMatchups.value.has(`${matchup.championName}-${matchup.role}`);
}

function toggleExpand(matchup) {
  const key = `${matchup.championName}-${matchup.role}`;
  if (expandedMatchups.value.has(key)) {
    expandedMatchups.value.delete(key);
  } else {
    expandedMatchups.value.add(key);
  }
}

function getVisibleOpponents(matchup) {
  if (isExpanded(matchup)) {
    return matchup.opponents;
  }
  return matchup.opponents.slice(0, 3);
}

// Load data
async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    matchupsData.value = await getChampionMatchups(props.userId);
    console.log('Loaded matchups data:', matchupsData.value);
    if (matchupsData.value?.matchups) {
      console.log('Total matchups loaded:', matchupsData.value.matchups.length);
      matchupsData.value.matchups.forEach(m => {
        console.log(`${m.championName} (${m.role}): ${m.opponents.length} opponents`);
      });
    }
  } catch (e) {
    console.error('Error loading champion matchups data:', e);
    error.value = e?.message || 'Failed to load champion matchups data.';
    matchupsData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.champion-matchups-container {
  width: 100%;
  margin-top: 1.2rem;
}

.matchups-loading,
.matchups-error,
.matchups-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.matchups-error {
  color: var(--color-danger);
}

.matchups-card {
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px 0 rgba(44, 11, 58, 0.08);
}

.card-title {
  margin: 0 0 1.2rem 0;
  font-size: 1.3rem;
  color: var(--color-text);
  font-weight: 600;
}

/* Controls */
.controls {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
}

.control-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  flex: 1;
  min-width: 200px;
}

.control-group label {
  font-size: 0.85rem;
  font-weight: 500;
  color: var(--color-text-muted);
}

.role-select,
.search-input {
  padding: 0.6rem 0.8rem;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  color: var(--color-text);
  font-size: 0.95rem;
  transition: border-color 0.2s;
}

.role-select:hover,
.search-input:hover {
  border-color: var(--color-primary);
}

.role-select:focus,
.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px rgba(124, 58, 237, 0.1);
}

/* Search Results */
.search-results {
  margin-top: 1rem;
}

.search-results h4 {
  margin: 0 0 1rem 0;
  font-size: 1.1rem;
  color: var(--color-text);
}

.no-results {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
  font-style: italic;
}

.results-list {
  display: flex;
  flex-direction: column;
  gap: 0.8rem;
}

.result-item {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 0.8rem;
  transition: background-color 0.2s;
}

.result-item:hover {
  background: var(--color-bg-hover);
}

.result-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.result-info {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex: 1;
}

.result-stats {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  font-size: 0.9rem;
}

/* Matchups List */
.matchups-list {
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
}

.matchup-card {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  padding: 1rem;
}

.matchup-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.8rem;
}

.champion-icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  border: 2px solid var(--color-border);
}

.opponent-icon {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid var(--color-border);
}

.header-info {
  flex: 1;
}

.champion-title {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.3rem;
}

.champion-name {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--color-text);
}

.role-badge {
  display: inline-block;
  padding: 0.2rem 0.6rem;
  background: var(--color-primary);
  color: white;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 500;
  text-transform: uppercase;
}

.overall-stats {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  font-size: 0.9rem;
  color: var(--color-text-muted);
}

.games {
  font-weight: 500;
}

.record {
  color: var(--color-text-muted);
}

.winrate {
  font-weight: 600;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
}

.winrate.positive {
  color: var(--color-success);
  background: rgba(33, 156, 78, 0.1);
}

.winrate.negative {
  color: var(--color-danger);
  background: rgba(168, 66, 66, 0.1);
}

.warning {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.3);
  border-radius: 6px;
  padding: 0.5rem 0.8rem;
  margin-bottom: 0.8rem;
  font-size: 0.85rem;
  color: #f59e0b;
}

/* Opponents List */
.opponents-list {
  margin-top: 0.8rem;
}

.opponents-header {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr;
  gap: 1rem;
  padding: 0.5rem 0.8rem;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-muted);
  text-transform: uppercase;
  border-bottom: 1px solid var(--color-border);
}

.opponent-row {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr;
  gap: 1rem;
  padding: 0.6rem 0.8rem;
  align-items: center;
  border-bottom: 1px solid var(--color-border);
  transition: background-color 0.2s;
}

.opponent-row:last-child {
  border-bottom: none;
}

.opponent-row:hover {
  background: var(--color-bg-hover);
}

.opponent-info {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.opponent-name {
  font-size: 0.95rem;
  color: var(--color-text);
}

.opponent-record {
  font-size: 0.9rem;
  color: var(--color-text-muted);
}

.opponent-winrate {
  font-weight: 600;
  font-size: 0.95rem;
}

.expand-btn {
  width: 100%;
  margin-top: 0.8rem;
  padding: 0.6rem;
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  color: var(--color-text);
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.expand-btn:hover {
  background: var(--color-bg-hover);
  border-color: var(--color-primary);
}

/* Responsive */
@media (max-width: 768px) {
  .controls {
    flex-direction: column;
  }

  .control-group {
    min-width: 100%;
  }

  .opponents-header,
  .opponent-row {
    grid-template-columns: 1.5fr 1fr 0.8fr;
    gap: 0.5rem;
    font-size: 0.85rem;
  }

  .champion-icon {
    width: 40px;
    height: 40px;
  }

  .opponent-icon {
    width: 28px;
    height: 28px;
  }
}
</style>



