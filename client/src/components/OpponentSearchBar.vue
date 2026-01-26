<template>
  <div class="opponent-search-bar relative w-[40%]" ref="containerRef">
    <div class="relative flex items-center">
      <input
        v-model="searchQuery"
        type="text"
        class="w-full py-sm px-md pr-8 text-sm border border-border rounded-md bg-background-elevated text-text transition-all duration-150 placeholder:text-text-secondary focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft"
        placeholder="Search opponent champion..."
        @focus="showDropdown = true"
        @input="onSearchInput"
      />
      <button
        v-if="searchQuery"
        type="button"
        class="absolute right-2 bg-transparent border-none text-text-secondary cursor-pointer py-0.5 px-1 text-xs leading-none transition-colors duration-150 hover:text-text"
        @click="clearSearch"
        aria-label="Clear search"
      >
        ✕
      </button>
    </div>

    <!-- Dropdown Results -->
    <Transition name="dropdown">
      <div
        v-if="showDropdown && isSearching"
        class="absolute top-full left-0 mt-1 w-full max-h-80 overflow-y-auto bg-background-surface border border-border rounded-md shadow-lg z-50"
      >
        <div v-if="searchResults.length > 0" class="py-1">
          <div
            v-for="result in searchResults"
            :key="`${result.championId}-${result.opponentId}`"
            class="flex items-center gap-sm px-md py-sm cursor-pointer transition-colors duration-150 hover:bg-background-elevated"
            @click="selectResult(result)"
          >
            <!-- Your Champion -->
            <img
              class="w-7 h-7 rounded-sm object-cover"
              :src="getChampionIconUrl(result.championName)"
              :alt="result.championName"
              loading="lazy"
            />
            <div class="flex flex-col min-w-[80px]">
              <span class="text-sm font-medium text-text">{{ result.championName }}</span>
              <span class="text-2xs text-text-secondary">{{ roleLabel(result.role) }}</span>
            </div>

            <!-- vs indicator -->
            <span class="text-xs text-text-secondary px-1">vs</span>

            <!-- Opponent Champion -->
            <img
              class="w-7 h-7 rounded-sm object-cover"
              :src="getChampionIconUrl(result.opponentName)"
              :alt="result.opponentName"
              loading="lazy"
            />
            <span class="text-sm text-text flex-1">{{ result.opponentName }}</span>

            <!-- Win Rate -->
            <div class="flex flex-col items-end">
              <span :class="['text-sm font-semibold', getWinRateColorClass(result.winRate)]">
                {{ formatWinRate(result.winRate) }}
              </span>
              <span class="text-2xs text-text-secondary">{{ result.wins }}-{{ result.losses }}</span>
            </div>
          </div>
        </div>
        <div v-else class="px-md py-lg text-center text-text-secondary text-sm">
          No matchups found for "{{ searchQuery }}"
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'

const props = defineProps({
  matchups: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['select'])

const containerRef = ref(null)
const searchQuery = ref('')
const showDropdown = ref(false)

const isSearching = computed(() => searchQuery.value.trim().length >= 2)

// Search through all matchups and find opponents matching the query
const searchResults = computed(() => {
  if (!isSearching.value || !props.matchups) return []

  const query = searchQuery.value.trim().toLowerCase()
  const results = []

  for (const matchup of props.matchups) {
    for (const opp of matchup.opponents) {
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

  return results.sort((a, b) => b.winRate - a.winRate)
})

function onSearchInput() {
  showDropdown.value = true
}

function clearSearch() {
  searchQuery.value = ''
  showDropdown.value = false
}

function selectResult(result) {
  emit('select', result)
  showDropdown.value = false
}

// Click outside handler
function handleClickOutside(event) {
  if (containerRef.value && !containerRef.value.contains(event.target)) {
    showDropdown.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})

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
  const map = { TOP: 'Top', JUNGLE: 'Jungle', MIDDLE: 'Mid', BOTTOM: 'ADC', UTILITY: 'Support', UNKNOWN: 'Fill' }
  return map[role] || role
}

function formatWinRate(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(1)}%`
}
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

/* Win rate color classes */
.winrate-red { color: #ef4444; }
.winrate-redorange { color: #f97316; }
.winrate-orange { color: #fdba74; }
.winrate-yellow { color: #eab308; }
.winrate-yellowgreen { color: #84cc16; }
.winrate-green { color: #22c55e; }
.winrate-neutral { color: var(--color-text); }
</style>

