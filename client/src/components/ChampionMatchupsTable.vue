<template>
  <section class="bg-background-surface border border-border rounded-lg p-lg">
    <header class="flex justify-between items-start gap-md mb-md flex-wrap">
      <div class="flex-1 min-w-[150px]">
        <h2 class="text-lg font-semibold text-text m-0">Champion Matchups</h2>
        <p class="text-sm text-text-secondary mt-xs mb-0">{{ isSearching ? 'Your champions vs ' + searchQuery : 'Top champions with opponent performance' }}</p>
      </div>
      <div class="flex flex-col items-end gap-sm flex-shrink-0">
        <div v-if="roles.length > 0" class="flex flex-wrap gap-xs" role="tablist">
          <button
            type="button"
            class="py-1 px-2.5 rounded-full border border-border bg-background-elevated text-text-secondary text-xs font-medium cursor-pointer transition-all duration-150 hover:border-primary hover:text-text"
            :class="{ 'bg-primary border-primary text-white': selectedRole === null }"
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
            class="py-1 px-2.5 rounded-full border border-border bg-background-elevated text-text-secondary text-xs font-medium cursor-pointer transition-all duration-150 hover:border-primary hover:text-text"
            :class="{ 'bg-primary border-primary text-white': role === selectedRole }"
            @click="selectRole(role)"
            role="tab"
            :aria-selected="role === selectedRole"
          >
            {{ roleLabel(role) }}
          </button>
        </div>
        <div class="relative flex items-center">
          <input
            v-model="searchQuery"
            type="text"
            class="w-40 py-xs px-sm pr-7 text-sm border border-border rounded-sm bg-background-elevated text-text transition-all duration-150 placeholder:text-text-secondary focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft"
            placeholder="Search opponent..."
            @input="onSearchInput"
          />
          <button
            v-if="searchQuery"
            type="button"
            class="absolute right-1.5 bg-transparent border-none text-text-secondary cursor-pointer py-0.5 px-1 text-xs leading-none transition-colors duration-150 hover:text-text"
            @click="clearSearch"
            aria-label="Clear search"
          >
            ✕
          </button>
        </div>
      </div>
    </header>

    <!-- Inverse view: searching for opponent -->
    <div v-if="isSearching && inverseMatchups.length > 0" class="overflow-x-auto">
      <table class="matchups-table w-full border-collapse text-sm">
        <thead>
          <tr>
            <th class="th-champion text-left font-medium text-text-secondary py-sm px-xs border-b border-border w-[40%]">Your Champion</th>
            <th class="th-opponent text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[15%]">vs {{ searchQuery }}</th>
            <th class="th-record text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[20%]">Record</th>
            <th class="th-winrate text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[15%]">Winrate</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in inverseMatchups" :key="item.championId" class="transition-colors duration-150 hover:bg-background-elevated">
            <td class="py-sm px-xs border-b border-border align-middle">
              <div class="flex items-center gap-sm">
                <img
                  class="champion-icon w-9 h-9 rounded-sm object-cover"
                  :src="getChampionIconUrl(item.championName)"
                  :alt="item.championName"
                  loading="lazy"
                />
                <div class="flex flex-col gap-0.5">
                  <span class="font-medium text-text">{{ item.championName }}</span>
                  <span class="text-xs text-text-secondary">{{ roleLabel(item.role) }}</span>
                </div>
              </div>
            </td>
            <td class="text-center align-middle py-sm px-xs border-b border-border">
              <div class="inline-flex items-center justify-center">
                <img
                  class="w-7 h-7 rounded-sm object-cover"
                  :src="getChampionIconUrl(item.opponentName)"
                  :alt="item.opponentName"
                  loading="lazy"
                />
              </div>
            </td>
            <td class="text-center align-middle text-text py-sm px-xs border-b border-border">{{ item.wins }}-{{ item.losses }}</td>
            <td :class="['text-center align-middle font-semibold py-sm px-xs border-b border-border', getWinRateColorClass(item.winRate)]">
              {{ formatWinRate(item.winRate) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- No results for search -->
    <div v-else-if="isSearching && inverseMatchups.length === 0" class="p-lg text-center text-text-secondary text-sm">
      <p class="m-0">No matchups found against "{{ searchQuery }}"</p>
    </div>

    <!-- Normal view: top champions with expandable opponents -->
    <div v-else-if="hasData && filteredMatchups.length > 0" class="overflow-x-auto">
      <table class="matchups-table w-full border-collapse text-sm">
        <thead>
          <tr>
            <th class="text-left font-medium text-text-secondary py-sm px-xs border-b border-border w-[40%]">Champion</th>
            <th class="text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[20%]">Record</th>
            <th class="text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[15%]">Winrate</th>
            <th class="text-center font-medium text-text-secondary py-sm px-xs border-b border-border w-[10%]"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="matchup in filteredMatchups" :key="matchup.championId">
            <!-- Champion row -->
            <tr class="cursor-pointer transition-colors duration-150 hover:bg-background-elevated" @click="toggleExpanded(matchup.championId)">
              <td class="py-sm px-xs border-b border-border align-middle">
                <div class="flex items-center gap-sm">
                  <img
                    class="champion-icon w-9 h-9 rounded-sm object-cover"
                    :src="getChampionIconUrl(matchup.championName)"
                    :alt="matchup.championName"
                    loading="lazy"
                  />
                  <div class="flex flex-col gap-0.5">
                    <span class="font-medium text-text">{{ matchup.championName }}</span>
                    <span class="text-xs text-text-secondary">{{ roleLabel(matchup.role) }}</span>
                  </div>
                </div>
              </td>
              <td class="text-center align-middle text-text py-sm px-xs border-b border-border">{{ matchup.wins }}-{{ matchup.totalGames - matchup.wins }}</td>
              <td :class="['text-center align-middle font-semibold py-sm px-xs border-b border-border', getWinRateColorClass(matchup.winRate)]">
                {{ formatWinRate(matchup.winRate) }}
              </td>
              <td class="text-center align-middle py-sm px-xs border-b border-border">
                <button
                  type="button"
                  class="bg-transparent border-none cursor-pointer p-xs text-text-secondary transition-colors duration-150 hover:text-text"
                  :aria-expanded="isExpanded(matchup.championId)"
                  :aria-label="isExpanded(matchup.championId) ? 'Collapse opponents' : 'Expand opponents'"
                >
                  <span class="inline-block text-[10px] transition-transform duration-200" :class="{ 'rotate-180': isExpanded(matchup.championId) }">▼</span>
                </button>
              </td>
            </tr>
            <!-- Opponent rows (expandable) -->
            <tr v-if="isExpanded(matchup.championId) && visibleOpponents(matchup).length > 0">
              <td colspan="4" class="p-0 border-b border-border bg-background-elevated">
                <div class="py-sm px-md flex flex-col gap-xs">
                  <div
                    v-for="opp in visibleOpponents(matchup)"
                    :key="opp.opponentChampionId"
                    class="flex items-center gap-sm py-xs"
                  >
                    <img
                      class="w-6 h-6 rounded-sm object-cover"
                      :src="getChampionIconUrl(opp.opponentChampionName)"
                      :alt="opp.opponentChampionName"
                      loading="lazy"
                    />
                    <span class="flex-1 text-text text-sm">{{ opp.opponentChampionName }}</span>
                    <span class="text-text-secondary text-sm min-w-[40px] text-center">{{ opp.wins }}-{{ opp.losses }}</span>
                    <span :class="['font-semibold text-sm min-w-[50px] text-right', getWinRateColorClass(opp.winRate)]">
                      {{ formatWinRate(opp.winRate) }}
                    </span>
                  </div>
                  <button
                    v-if="matchup.opponents.length > 3 && !showAllOpponents[matchup.championId]"
                    type="button"
                    class="bg-transparent border border-border rounded-sm py-xs px-sm text-xs text-primary cursor-pointer mt-xs transition-all duration-150 hover:bg-primary-soft hover:border-primary"
                    @click.stop="showAllOpponents[matchup.championId] = true"
                  >
                    Show All ({{ matchup.opponents.length - 3 }} more)
                  </button>
                  <button
                    v-else-if="matchup.opponents.length > 3 && showAllOpponents[matchup.championId]"
                    type="button"
                    class="bg-transparent border border-border rounded-sm py-xs px-sm text-xs text-primary cursor-pointer mt-xs transition-all duration-150 hover:bg-primary-soft hover:border-primary"
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
    <div v-else-if="hasData && filteredMatchups.length === 0" class="p-lg text-center text-text-secondary text-sm">
      <p class="m-0">No matchups found for this role.</p>
    </div>

    <div v-else class="p-lg text-center text-text-secondary text-sm">
      <p class="m-0">No matchup data available for this filter.</p>
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
/* Win rate color classes (dynamic classes can't be done with Tailwind) */
.winrate-red { color: #ef4444; }
.winrate-redorange { color: #f97316; }
.winrate-orange { color: #fdba74; }
.winrate-yellow { color: #eab308; }
.winrate-yellowgreen { color: #84cc16; }
.winrate-green { color: #22c55e; }
.winrate-neutral { color: var(--color-text); }

/* Responsive styles for table */
@media (max-width: 640px) {
  .matchups-table {
    font-size: var(--font-size-xs);
  }

  .champion-icon {
    width: 28px;
    height: 28px;
  }
}
</style>

