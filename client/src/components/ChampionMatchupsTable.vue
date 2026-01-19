<template>
  <section class="matchups-card">
    <header class="header">
      <div class="header-left">
        <h2 class="title">Champion Matchups</h2>
        <p class="subtitle">{{ isSearching ? 'Your champions vs ' + searchQuery : 'Top champions with opponent performance' }}</p>
      </div>
      <div class="header-right">
        <div v-if="roles.length > 0" class="role-tabs" role="tablist">
          <button
            type="button"
            class="role-pill"
            :class="{ active: selectedRole === null }"
            @click="selectRole(null)"
            role="tab"
            :aria-selected="selectedRole === null"
          >
            All
          </button>
          <button
            v-for="role in roles"
            :key="role"
            type="button"
            class="role-pill"
            :class="{ active: role === selectedRole }"
            @click="selectRole(role)"
            role="tab"
            :aria-selected="role === selectedRole"
          >
            {{ roleLabel(role) }}
          </button>
        </div>
        <div class="search-wrapper">
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Search opponent..."
            @input="onSearchInput"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="search-clear"
            @click="clearSearch"
            aria-label="Clear search"
          >
            ✕
          </button>
        </div>
      </div>
    </header>

    <!-- Inverse view: searching for opponent -->
    <div v-if="isSearching && inverseMatchups.length > 0" class="matchups-table-wrapper">
      <table class="matchups-table">
        <thead>
          <tr>
            <th class="th-champion">Your Champion</th>
            <th class="th-opponent">vs {{ searchQuery }}</th>
            <th class="th-record">Record</th>
            <th class="th-winrate">Winrate</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in inverseMatchups" :key="item.championId" class="inverse-row">
            <td class="td-champion">
              <div class="champion-info">
                <img
                  class="champion-icon"
                  :src="getChampionIconUrl(item.championName)"
                  :alt="item.championName"
                  loading="lazy"
                />
                <div class="champion-details">
                  <span class="champion-name">{{ item.championName }}</span>
                  <span class="role-badge">{{ roleLabel(item.role) }}</span>
                </div>
              </div>
            </td>
            <td class="td-opponent">
              <div class="opponent-mini">
                <img
                  class="opponent-icon-sm"
                  :src="getChampionIconUrl(item.opponentName)"
                  :alt="item.opponentName"
                  loading="lazy"
                />
              </div>
            </td>
            <td class="td-record">{{ item.wins }}-{{ item.losses }}</td>
            <td :class="['td-winrate', getWinRateColorClass(item.winRate)]">
              {{ formatWinRate(item.winRate) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- No results for search -->
    <div v-else-if="isSearching && inverseMatchups.length === 0" class="empty-state">
      <p>No matchups found against "{{ searchQuery }}"</p>
    </div>

    <!-- Normal view: top champions with expandable opponents -->
    <div v-else-if="hasData && filteredMatchups.length > 0" class="matchups-table-wrapper">
      <table class="matchups-table">
        <thead>
          <tr>
            <th class="th-champion">Champion</th>
            <th class="th-record">Record</th>
            <th class="th-winrate">Winrate</th>
            <th class="th-expand"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="matchup in filteredMatchups" :key="matchup.championId">
            <!-- Champion row -->
            <tr class="champion-row" @click="toggleExpanded(matchup.championId)">
              <td class="td-champion">
                <div class="champion-info">
                  <img
                    class="champion-icon"
                    :src="getChampionIconUrl(matchup.championName)"
                    :alt="matchup.championName"
                    loading="lazy"
                  />
                  <div class="champion-details">
                    <span class="champion-name">{{ matchup.championName }}</span>
                    <span class="role-badge">{{ roleLabel(matchup.role) }}</span>
                  </div>
                </div>
              </td>
              <td class="td-record">{{ matchup.wins }}-{{ matchup.totalGames - matchup.wins }}</td>
              <td :class="['td-winrate', getWinRateColorClass(matchup.winRate)]">
                {{ formatWinRate(matchup.winRate) }}
              </td>
              <td class="td-expand">
                <button
                  type="button"
                  class="expand-btn"
                  :aria-expanded="isExpanded(matchup.championId)"
                  :aria-label="isExpanded(matchup.championId) ? 'Collapse opponents' : 'Expand opponents'"
                >
                  <span class="expand-icon" :class="{ rotated: isExpanded(matchup.championId) }">▼</span>
                </button>
              </td>
            </tr>
            <!-- Opponent rows (expandable) -->
            <tr v-if="isExpanded(matchup.championId) && visibleOpponents(matchup).length > 0" class="opponents-row">
              <td colspan="4" class="opponents-cell">
                <div class="opponents-list">
                  <div
                    v-for="opp in visibleOpponents(matchup)"
                    :key="opp.opponentChampionId"
                    class="opponent-item"
                  >
                    <img
                      class="opponent-icon"
                      :src="getChampionIconUrl(opp.opponentChampionName)"
                      :alt="opp.opponentChampionName"
                      loading="lazy"
                    />
                    <span class="opponent-name">{{ opp.opponentChampionName }}</span>
                    <span class="opponent-record">{{ opp.wins }}-{{ opp.losses }}</span>
                    <span :class="['opponent-winrate', getWinRateColorClass(opp.winRate)]">
                      {{ formatWinRate(opp.winRate) }}
                    </span>
                  </div>
                  <button
                    v-if="matchup.opponents.length > 3 && !showAllOpponents[matchup.championId]"
                    type="button"
                    class="show-all-btn"
                    @click.stop="showAllOpponents[matchup.championId] = true"
                  >
                    Show All ({{ matchup.opponents.length - 3 }} more)
                  </button>
                  <button
                    v-else-if="matchup.opponents.length > 3 && showAllOpponents[matchup.championId]"
                    type="button"
                    class="show-all-btn"
                    @click.stop="showAllOpponents[matchup.championId] = false"
                  >
                    Show Less
                  </button>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- No data for selected role -->
    <div v-else-if="hasData && filteredMatchups.length === 0" class="empty-state">
      <p>No matchups found for this role.</p>
    </div>

    <div v-else class="empty-state">
      <p>No matchup data available for this filter.</p>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, reactive, watch, onUnmounted } from 'vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'
import { trackFeature } from '../services/analyticsApi'

const props = defineProps({
  matchups: {
    type: Array,
    default: () => []
  }
})

const expandedChampions = reactive({})
const showAllOpponents = reactive({})
const searchQuery = ref('')
const selectedRole = ref(null)

const hasData = computed(() => props.matchups && props.matchups.length > 0)
const isSearching = computed(() => searchQuery.value.trim().length >= 2)

// Extract unique roles from matchups data
const roles = computed(() => {
  if (!props.matchups) return []
  const roleSet = new Set(props.matchups.map(m => m.role).filter(Boolean))
  // Sort roles in standard order
  const roleOrder = ['TOP', 'JUNGLE', 'MIDDLE', 'BOTTOM', 'UTILITY']
  return roleOrder.filter(r => roleSet.has(r))
})

function selectRole(role) {
  selectedRole.value = role
}

// Filter matchups by selected role
const filteredMatchups = computed(() => {
  if (!props.matchups) return []
  if (selectedRole.value === null) return props.matchups
  return props.matchups.filter(m => m.role === selectedRole.value)
})

// Inverse matchups: when searching, find all my champions that faced the searched opponent
const inverseMatchups = computed(() => {
  if (!isSearching.value || !props.matchups) return []

  const query = searchQuery.value.trim().toLowerCase()
  const results = []

  // Use filteredMatchups to respect role filter
  const sourceMatchups = selectedRole.value === null ? props.matchups : filteredMatchups.value

  for (const matchup of sourceMatchups) {
    for (const opp of matchup.opponents) {
      // Match opponent name (partial, case-insensitive)
      if (opp.opponentChampionName.toLowerCase().includes(query)) {
        results.push({
          championId: matchup.championId,
          championName: matchup.championName,
          role: matchup.role,
          opponentId: opp.opponentChampionId,
          opponentName: opp.opponentChampionName,
          wins: opp.wins,
          losses: opp.losses,
          gamesPlayed: opp.gamesPlayed,
          winRate: opp.winRate
        })
      }
    }
  }

  // Sort by winrate descending (best counters first)
  return results.sort((a, b) => b.winRate - a.winRate)
})

// Track search when user finishes typing (debounced via watcher)
let searchDebounceTimer = null

onUnmounted(() => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
    searchDebounceTimer = null
  }
})
watch(searchQuery, (newQuery) => {
  clearTimeout(searchDebounceTimer)
  if (newQuery.trim().length >= 2) {
    searchDebounceTimer = setTimeout(() => {
      trackFeature('champion_matchup_search', {
        query: newQuery.trim(),
        resultsCount: inverseMatchups.value.length,
        role: selectedRole.value
      })
    }, 500) // Wait 500ms after typing stops
  }
})

function onSearchInput() {
  // Search tracking handled by watcher with debounce
}

function clearSearch() {
  searchQuery.value = ''
}

function isExpanded(championId) {
  return expandedChampions[championId] === true
}

function toggleExpanded(championId) {
  expandedChampions[championId] = !expandedChampions[championId]
}

function visibleOpponents(matchup) {
  if (showAllOpponents[matchup.championId]) {
    return matchup.opponents
  }
  // Sort by games played descending, then show top 3
  const sorted = [...matchup.opponents].sort((a, b) => b.gamesPlayed - a.gamesPlayed)
  return sorted.slice(0, 3)
}

// Data Dragon version for champion icons
const ddVersion = '16.1.1'

function normalizeChampionName(name) {
  if (!name) return ''
  return name.replace(/[^A-Za-z0-9]/g, '')
}

function getChampionIconUrl(name) {
  const normalized = normalizeChampionName(name)
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/champion/${normalized}.png`
}

function roleLabel(role) {
  const map = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support',
    UNKNOWN: 'Fill'
  }
  return map[role] || role
}

function formatWinRate(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(1)}%`
}
</script>

<style scoped>
.matchups-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  flex-wrap: wrap;
}

.header-left {
  flex: 1;
  min-width: 150px;
}

.header-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: var(--spacing-sm);
  flex-shrink: 0;
}

.title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.subtitle {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: var(--spacing-xs) 0 0 0;
}

.role-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-xs);
}

.role-pill {
  padding: 4px 10px;
  border-radius: 999px;
  border: 1px solid var(--color-border);
  background: var(--color-elevated);
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.15s ease;
}

.role-pill:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
}

.role-pill.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: #fff;
}

.search-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.search-input {
  width: 160px;
  padding: var(--spacing-xs) var(--spacing-sm);
  padding-right: 28px;
  font-size: var(--font-size-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-elevated);
  color: var(--color-text);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.search-input::placeholder {
  color: var(--color-text-secondary);
}

.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px var(--color-primary-soft);
}

.search-clear {
  position: absolute;
  right: 6px;
  background: transparent;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  padding: 2px 4px;
  font-size: 12px;
  line-height: 1;
  transition: color 0.15s ease;
}

.search-clear:hover {
  color: var(--color-text);
}

.matchups-table-wrapper {
  overflow-x: auto;
}

.matchups-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.matchups-table th {
  text-align: left;
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  padding: var(--spacing-sm) var(--spacing-xs);
  border-bottom: 1px solid var(--color-border);
}

.th-champion { width: 40%; }
.th-opponent { width: 15%; text-align: center; }
.th-record { width: 20%; text-align: center; }
.th-winrate { width: 15%; text-align: center; }
.th-expand { width: 10%; text-align: center; }

.champion-row,
.inverse-row {
  cursor: pointer;
  transition: background 0.15s ease;
}

.champion-row:hover,
.inverse-row:hover {
  background: var(--color-elevated);
}

.champion-row td,
.inverse-row td {
  padding: var(--spacing-sm) var(--spacing-xs);
  border-bottom: 1px solid var(--color-border);
}

.inverse-row {
  cursor: default;
}

.td-champion { vertical-align: middle; }
.td-opponent { text-align: center; vertical-align: middle; }
.td-record { text-align: center; vertical-align: middle; color: var(--color-text); }
.td-winrate { text-align: center; vertical-align: middle; font-weight: var(--font-weight-semibold); }
.td-expand { text-align: center; vertical-align: middle; }

.opponent-mini {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.opponent-icon-sm {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.champion-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-icon {
  width: 36px;
  height: 36px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.champion-details {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.champion-name {
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.role-badge {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.expand-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: var(--spacing-xs);
  color: var(--color-text-secondary);
  transition: color 0.15s ease;
}

.expand-btn:hover {
  color: var(--color-text);
}

.expand-icon {
  display: inline-block;
  font-size: 10px;
  transition: transform 0.2s ease;
}

.expand-icon.rotated {
  transform: rotate(180deg);
}

.opponents-row td {
  padding: 0;
  border-bottom: 1px solid var(--color-border);
}

.opponents-cell {
  background: var(--color-elevated);
}

.opponents-list {
  padding: var(--spacing-sm) var(--spacing-md);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.opponent-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-xs) 0;
}

.opponent-icon {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.opponent-name {
  flex: 1;
  color: var(--color-text);
  font-size: var(--font-size-sm);
}

.opponent-record {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  min-width: 40px;
  text-align: center;
}

.opponent-winrate {
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  min-width: 50px;
  text-align: right;
}

.show-all-btn {
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-xs);
  color: var(--color-primary);
  cursor: pointer;
  margin-top: var(--spacing-xs);
  transition: all 0.15s ease;
}

.show-all-btn:hover {
  background: var(--color-primary-soft);
  border-color: var(--color-primary);
}

.empty-state {
  padding: var(--spacing-lg);
  text-align: center;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

/* Win rate color classes */
.winrate-red { color: #ef4444; }
.winrate-redorange { color: #f97316; }
.winrate-orange { color: #fdba74; }
.winrate-yellow { color: #eab308; }
.winrate-yellowgreen { color: #84cc16; }
.winrate-green { color: #22c55e; }
.winrate-neutral { color: var(--color-text); }

@media (max-width: 640px) {
  .matchups-table {
    font-size: var(--font-size-xs);
  }

  .champion-icon {
    width: 28px;
    height: 28px;
  }

  .th-record,
  .th-winrate {
    width: auto;
  }
}
</style>

