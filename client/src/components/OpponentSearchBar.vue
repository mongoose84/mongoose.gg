<template>
  <Combobox
    as="div"
    class="opponent-search-bar relative w-[40%]"
    :model-value="null"
    @update:model-value="selectResult"
    nullable
  >
    <div class="relative flex items-center">
      <ComboboxInput
        class="w-full py-sm px-md pr-8 text-sm border border-border rounded-md bg-background-elevated text-text transition-all duration-150 placeholder:text-text-secondary focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft"
        placeholder="Search opponent champion..."
        :display-value="() => searchQuery"
        @change="searchQuery = $event.target.value"
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
    <transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0 -translate-y-2"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 -translate-y-2"
    >
      <ComboboxOptions
        v-if="isSearching"
        class="absolute top-full left-0 mt-1 w-full max-h-80 overflow-y-auto bg-background-surface border border-border rounded-md shadow-lg z-50 focus:outline-none"
      >
        <div v-if="searchResults.length > 0" class="py-1">
          <ComboboxOption
            v-for="result in searchResults"
            :key="`${result.championId}-${result.opponentId}`"
            :value="result"
            v-slot="{ active }"
          >
            <div
              :class="[
                'flex items-center gap-sm px-md py-sm cursor-pointer transition-colors duration-150',
                active ? 'bg-background-elevated' : ''
              ]"
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

              <!-- Stats: In-lane and Out-of-lane -->
              <div class="flex items-center gap-md ml-auto">
                <!-- In-lane stats -->
                <div v-if="result.inLaneGames > 0" class="flex flex-col items-center min-w-[60px]">
                  <span :class="['text-sm font-semibold', getWinRateColorClass(result.inLaneWinRate)]">
                    {{ formatWinRate(result.inLaneWinRate) }}
                  </span>
                  <span class="text-2xs text-text-secondary">{{ result.inLaneWins }}-{{ result.inLaneLosses }} lane</span>
                </div>
                <!-- Out-of-lane stats -->
                <div v-if="result.outOfLaneGames > 0" class="flex flex-col items-center min-w-[60px]">
                  <span :class="['text-sm font-semibold', getWinRateColorClass(result.outOfLaneWinRate)]">
                    {{ formatWinRate(result.outOfLaneWinRate) }}
                  </span>
                  <span class="text-2xs text-text-secondary">{{ result.outOfLaneWins }}-{{ result.outOfLaneLosses }} other</span>
                </div>
                <!-- Overall stats (always shown) -->
                <div class="flex flex-col items-end min-w-[60px] pl-sm border-l border-border">
                  <span :class="['text-sm font-bold', getWinRateColorClass(result.totalWinRate)]">
                    {{ formatWinRate(result.totalWinRate) }}
                  </span>
                  <span class="text-2xs text-text-secondary">{{ result.totalWins }}-{{ result.totalLosses }} total</span>
                </div>
              </div>
            </div>
          </ComboboxOption>
        </div>
        <div v-else class="px-md py-lg text-center text-text-secondary text-sm">
          No matchups found for "{{ searchQuery }}"
        </div>
      </ComboboxOptions>
    </transition>
  </Combobox>
</template>

<script setup>
import { ref, computed } from 'vue'
import { Combobox, ComboboxInput, ComboboxOptions, ComboboxOption } from '@headlessui/vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'
import { formatRoleWithAdc as roleLabel, formatWinRate } from '@/utils/formatters'

const props = defineProps({
  matchups: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['select'])

const searchQuery = ref('')

const isSearching = computed(() => searchQuery.value.trim().length >= 2)

// Helper to calculate win rate
function calcWinRate(wins, games) {
  return games > 0 ? (wins / games) * 100 : 0
}

// Search through all matchups and find opponents matching the query
const searchResults = computed(() => {
  if (!isSearching.value || !props.matchups) return []

  const query = searchQuery.value.trim().toLowerCase()
  const results = []

  for (const matchup of props.matchups) {
    for (const opp of matchup.opponents) {
      if (opp.opponentChampionName.toLowerCase().includes(query)) {
        // Calculate derived values from raw data
        const inLaneGames = opp.inLaneWins + opp.inLaneLosses
        const outOfLaneGames = opp.outOfLaneWins + opp.outOfLaneLosses
        const totalGames = inLaneGames + outOfLaneGames
        const totalWins = opp.inLaneWins + opp.outOfLaneWins
        const totalLosses = opp.inLaneLosses + opp.outOfLaneLosses

        results.push({
          championId: matchup.championId,
          championName: matchup.championName,
          role: matchup.role,
          opponentId: opp.opponentChampionId,
          opponentName: opp.opponentChampionName,
          // In-lane stats
          inLaneWins: opp.inLaneWins,
          inLaneLosses: opp.inLaneLosses,
          inLaneGames,
          inLaneWinRate: calcWinRate(opp.inLaneWins, inLaneGames),
          // Out-of-lane stats
          outOfLaneWins: opp.outOfLaneWins,
          outOfLaneLosses: opp.outOfLaneLosses,
          outOfLaneGames,
          outOfLaneWinRate: calcWinRate(opp.outOfLaneWins, outOfLaneGames),
          // Total stats
          totalWins,
          totalLosses,
          totalGames,
          totalWinRate: calcWinRate(totalWins, totalGames)
        })
      }
    }
  }

  // Sort by total games played (most relevant matchups first)
  return results.sort((a, b) => b.totalGames - a.totalGames)
})

function clearSearch() {
  searchQuery.value = ''
}

function selectResult(result) {
  if (result) {
    emit('select', result)
  }
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
</script>

<style>
/* Note: winrate-* classes are defined globally in style.css */
</style>

