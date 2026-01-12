<template>
  <div class="champion-matchups card">
    <div class="card-header">
      <h3 class="card-title">Champion Matchups</h3>
      <div class="header-controls">
        <!-- Search opponent filter -->
        <div class="search-box" v-if="!loading && matchups?.length">
          <input
            v-model="opponentSearch"
            type="text"
            placeholder="Search opponent..."
            class="search-input"
          />
        </div>
        <span v-if="matchups?.length" class="matchup-count">{{ filteredMatchups.length }} champions</span>
      </div>
    </div>

    <div v-if="loading" class="loading-skeleton">
      <div class="skeleton-row" v-for="i in 5" :key="i"></div>
    </div>

    <div v-else-if="matchups && matchups.length" class="matchups-content">
      <!-- Table Header -->
      <div class="matchups-header">
        <span class="col-champion">Champion</span>
        <span class="col-role">Role</span>
        <span class="col-games">Games</span>
        <span class="col-winrate">Win Rate</span>
        <span class="col-expand"></span>
      </div>

      <!-- Champion Rows -->
      <div
        v-for="matchup in displayedMatchups"
        :key="`${matchup.championId}-${matchup.role}`"
        class="matchup-group"
      >
        <div
          class="matchup-row"
          :class="{ expanded: isExpanded(matchup) }"
          @click="toggleExpand(matchup)"
        >
          <div class="col-champion">
            <img
              :src="getChampionIcon(matchup.championName)"
              :alt="matchup.championName"
              class="champion-icon"
            />
            <span class="champion-name">{{ matchup.championName }}</span>
          </div>
          <span class="col-role">{{ formatRole(matchup.role) }}</span>
          <span class="col-games">{{ matchup.totalGames }}</span>
          <span class="col-winrate" :class="getWinrateClass(matchup.winrate)">
            {{ formatPercent(matchup.winrate) }}
          </span>
          <span class="col-expand">
            <svg v-if="matchup.opponents?.length" class="expand-icon" :class="{ rotated: isExpanded(matchup) }" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
            </svg>
          </span>
        </div>

        <!-- Opponent details (expandable) -->
        <div v-if="isExpanded(matchup) && matchup.opponents?.length" class="opponents-panel">
          <div class="opponents-header">
            <span class="opp-col-champ">vs Opponent</span>
            <span class="opp-col-games">Games</span>
            <span class="opp-col-record">W-L</span>
            <span class="opp-col-winrate">Win Rate</span>
          </div>
          <div
            v-for="opp in getFilteredOpponents(matchup)"
            :key="opp.opponentChampionId"
            class="opponent-row"
          >
            <div class="opp-col-champ">
              <img
                :src="getChampionIcon(opp.opponentChampionName)"
                :alt="opp.opponentChampionName"
                class="opp-icon"
              />
              <span>{{ opp.opponentChampionName }}</span>
            </div>
            <span class="opp-col-games">{{ opp.gamesPlayed }}</span>
            <span class="opp-col-record">{{ opp.wins }}-{{ opp.losses }}</span>
            <span class="opp-col-winrate" :class="getWinrateClass(opp.winrate)">
              {{ formatPercent(opp.winrate) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Show More Button -->
      <button
        v-if="filteredMatchups.length > displayLimit && !showAll"
        class="btn-show-more"
        @click="showAll = true"
      >
        Show All ({{ filteredMatchups.length - displayLimit }} more)
      </button>
    </div>

    <div v-else class="empty-state">
      <p>No champion matchup data available</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  matchups: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const showAll = ref(false)
const displayLimit = 10
const opponentSearch = ref('')
const expandedItems = ref(new Set())

// Filter matchups by opponent search
const filteredMatchups = computed(() => {
  if (!props.matchups) return []
  const searchTerm = opponentSearch.value.toLowerCase().trim()

  if (!searchTerm) {
    return [...props.matchups].sort((a, b) => b.totalGames - a.totalGames)
  }

  // Filter to champions that have matchups against the searched opponent
  return props.matchups
    .filter(m => m.opponents?.some(o =>
      o.opponentChampionName.toLowerCase().includes(searchTerm)
    ))
    .sort((a, b) => b.totalGames - a.totalGames)
})

const displayedMatchups = computed(() => {
  return showAll.value ? filteredMatchups.value : filteredMatchups.value.slice(0, displayLimit)
})

function getMatchupKey(matchup) {
  return `${matchup.championId}-${matchup.role}`
}

function isExpanded(matchup) {
  return expandedItems.value.has(getMatchupKey(matchup))
}

function toggleExpand(matchup) {
  const key = getMatchupKey(matchup)
  if (expandedItems.value.has(key)) {
    expandedItems.value.delete(key)
  } else {
    expandedItems.value.add(key)
  }
  // Trigger reactivity
  expandedItems.value = new Set(expandedItems.value)
}

function getFilteredOpponents(matchup) {
  if (!matchup.opponents) return []
  const searchTerm = opponentSearch.value.toLowerCase().trim()

  if (!searchTerm) {
    return matchup.opponents.slice(0, 10) // Show top 10 by default
  }

  return matchup.opponents.filter(o =>
    o.opponentChampionName.toLowerCase().includes(searchTerm)
  )
}

function getChampionIcon(championName) {
  if (!championName) return ''
  // Normalize champion name for Data Dragon (remove spaces, special chars)
  const normalized = championName.replace(/[^a-zA-Z]/g, '')
  return `https://ddragon.leagueoflegends.com/cdn/14.24.1/img/champion/${normalized}.png`
}

function getWinrateClass(winRate) {
  if (winRate >= 55) return 'winrate-high'
  if (winRate >= 50) return 'winrate-good'
  return 'winrate-low'
}

function formatPercent(value) {
  if (value == null) return '0%'
  return `${Math.round(value)}%`
}

function formatRole(role) {
  if (!role) return ''
  const roleMap = {
    'TOP': 'Top',
    'JUNGLE': 'Jungle',
    'MIDDLE': 'Mid',
    'BOTTOM': 'Bot',
    'UTILITY': 'Support'
  }
  return roleMap[role.toUpperCase()] || role
}
</script>

<style scoped>
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
  flex-wrap: wrap;
  gap: var(--spacing-sm);
}

.card-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.header-controls {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.search-box {
  position: relative;
}

.search-input {
  background: var(--color-surface-hover);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text);
  width: 160px;
  transition: all 0.2s;
}

.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
}

.search-input::placeholder {
  color: var(--color-text-secondary);
}

.matchup-count {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.matchups-header, .matchup-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr 40px;
  gap: var(--spacing-sm);
  align-items: center;
  padding: var(--spacing-sm) var(--spacing-xs);
}

.matchups-header {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
  border-bottom: 1px solid var(--color-border);
}

.matchup-group {
  border-bottom: 1px solid var(--color-border);
}

.matchup-group:last-of-type {
  border-bottom: none;
}

.matchup-row {
  font-size: var(--font-size-sm);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: background 0.15s;
}

.matchup-row:hover {
  background: var(--color-surface-hover);
}

.matchup-row.expanded {
  background: var(--color-surface-hover);
}

.col-champion {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-icon {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.champion-name {
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.col-role {
  color: var(--color-text-secondary);
  text-align: center;
  font-size: var(--font-size-xs);
}

.col-games {
  color: var(--color-text-secondary);
  text-align: center;
}

.col-winrate {
  text-align: center;
  font-weight: var(--font-weight-medium);
}

.col-expand {
  display: flex;
  justify-content: center;
  align-items: center;
}

.expand-icon {
  width: 16px;
  height: 16px;
  color: var(--color-text-secondary);
  transition: transform 0.2s;
}

.expand-icon.rotated {
  transform: rotate(180deg);
}

.winrate-high { color: #22c55e; }
.winrate-good { color: #3b82f6; }
.winrate-low { color: #ef4444; }

/* Opponents Panel */
.opponents-panel {
  background: rgba(0, 0, 0, 0.2);
  border-radius: var(--radius-sm);
  margin: var(--spacing-xs) var(--spacing-sm) var(--spacing-sm);
  padding: var(--spacing-sm);
}

.opponents-header, .opponent-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr;
  gap: var(--spacing-sm);
  align-items: center;
  padding: var(--spacing-xs) 0;
}

.opponents-header {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  border-bottom: 1px solid var(--color-border);
  padding-bottom: var(--spacing-xs);
  margin-bottom: var(--spacing-xs);
}

.opponent-row {
  font-size: var(--font-size-sm);
}

.opponent-row:hover {
  background: rgba(255, 255, 255, 0.05);
  border-radius: var(--radius-sm);
}

.opp-col-champ {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.opp-icon {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-xs);
  object-fit: cover;
}

.opp-col-games, .opp-col-record, .opp-col-winrate {
  text-align: center;
  color: var(--color-text-secondary);
}

.opp-col-winrate {
  font-weight: var(--font-weight-medium);
}

.btn-show-more {
  width: 100%;
  padding: var(--spacing-sm);
  margin-top: var(--spacing-md);
  background: var(--color-surface-hover);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-show-more:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

/* Loading skeleton */
.loading-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.skeleton-row {
  height: 48px;
  background: linear-gradient(90deg, var(--color-surface-hover) 25%, var(--color-surface) 50%, var(--color-surface-hover) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty-state {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-secondary);
}

@media (max-width: 600px) {
  .matchups-header, .matchup-row {
    grid-template-columns: 2fr 1fr 1fr 40px;
  }

  .col-role {
    display: none;
  }

  .header-controls {
    width: 100%;
    justify-content: space-between;
  }

  .search-input {
    width: 120px;
  }
}
</style>

