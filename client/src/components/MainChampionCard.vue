<template>
  <section class="bg-background-surface border border-border rounded-lg p-lg h-full flex flex-col backdrop-blur-sm">
    <TabGroup v-if="hasData" :selected-index="selectedTabIndex" @change="handleTabChange" as="div" class="flex-1 flex flex-col">
      <!-- Header with role tabs -->
      <header class="flex items-center justify-between gap-md mb-lg">
        <div>
          <h2 class="m-0 text-lg font-semibold text-text">Your Champions</h2>
          <p class="mt-1 mb-0 text-xs text-text-secondary">Top picks based on your performance</p>
        </div>
        <TabList class="flex gap-1 p-1 bg-background-elevated rounded-lg border border-border">
          <Tab
            v-for="role in roles"
            :key="role"
            v-slot="{ selected }"
            as="template"
          >
            <button
              type="button"
              :class="[
                'py-1.5 px-3 rounded-md text-xs font-medium cursor-pointer transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                selected
                  ? 'bg-primary text-white shadow-sm'
                  : 'text-text-secondary hover:text-text hover:bg-[rgba(255,255,255,0.05)]'
              ]"
            >
              {{ roleLabel(role) }}
            </button>
          </Tab>
        </TabList>
      </header>

      <TabPanels class="flex-1">
        <TabPanel
          v-for="role in roles"
          :key="role"
          class="focus:outline-none h-full"
        >
          <!-- 3 Champion Cards in a row -->
          <div class="grid grid-cols-3 gap-md h-full">
            <article
              v-for="(champion, index) in championsForRole(role)"
              :key="champion.championId"
              class="player-card relative flex flex-col rounded-lg overflow-hidden border transition-all duration-200"
              :class="[
                index === 0
                  ? 'bg-gradient-to-b from-primary/15 via-background-elevated to-background-elevated border-primary/40 shadow-md'
                  : 'bg-background-elevated border-border hover:border-primary/30 hover:shadow-sm'
              ]"
            >
              <!-- Recommended badge for first card -->
              <div
                v-if="index === 0"
                class="absolute top-0 left-0 right-0 py-1 bg-primary text-center text-2xs font-semibold text-white uppercase tracking-wider"
              >
                Recommended
              </div>

              <!-- Champion portrait header -->
              <div class="relative pt-8 pb-4 px-4 flex flex-col items-center" :class="{ 'pt-10': index === 0 }">
                <img
                  class="w-20 h-20 rounded-lg object-cover shadow-lg"
                  :class="index === 0 ? 'ring-2 ring-primary/60' : 'ring-1 ring-border'"
                  :src="getChampionIconUrl(champion.championName)"
                  :alt="`${champion.championName} icon`"
                />
                <h3 class="mt-3 mb-0 text-base font-bold text-text text-center">{{ champion.championName }}</h3>
                <span class="text-2xs text-text-secondary uppercase tracking-wide">{{ roleLabel(role) }}</span>
              </div>

              <!-- Stats section -->
              <div class="px-4 py-3 bg-[rgba(0,0,0,0.2)]">
                <div class="grid grid-cols-3 gap-2 text-center">
                  <div class="flex flex-col">
                    <span :class="['text-lg font-bold', getWinRateColorClass(champion.winRate)]">
                      {{ formatWinRate(champion.winRate) }}
                    </span>
                    <span class="text-2xs text-text-secondary">WR</span>
                  </div>
                  <div class="flex flex-col">
                    <span class="text-lg font-bold text-text">{{ formatLpPerGame(champion.lpPerGame) }}</span>
                    <span class="text-2xs text-text-secondary">LP/G</span>
                  </div>
                  <div class="flex flex-col">
                    <span class="text-lg font-bold text-text">
                      <span class="text-success">{{ champion.wins }}</span><span class="text-text-secondary">-</span><span class="text-error">{{ champion.losses }}</span>
                    </span>
                    <span class="text-2xs text-text-secondary">{{ champion.gamesPlayed }}G</span>
                  </div>
                </div>
              </div>

              <!-- Matchups section -->
              <div class="flex-1 px-4 py-3 flex flex-col gap-3">
                <!-- Strong vs -->
                <div>
                  <div class="flex items-center gap-1.5 mb-1.5">
                    <span class="w-1.5 h-1.5 rounded-full bg-success"></span>
                    <span class="text-2xs text-text-secondary uppercase tracking-wide font-medium">Strong vs</span>
                  </div>
                  <div v-if="getMatchupsForChampion(champion.championId, role).good.length > 0" class="flex gap-1.5">
                    <div
                      v-for="opponent in getMatchupsForChampion(champion.championId, role).good"
                      :key="opponent.opponentChampionId"
                      class="matchup-item relative flex flex-col items-center"
                    >
                      <img
                        class="w-8 h-8 rounded object-cover ring-1 ring-success/40"
                        :src="getChampionIconUrl(opponent.opponentChampionName)"
                        :alt="opponent.opponentChampionName"
                      />
                      <span class="text-2xs font-medium text-success mt-0.5">{{ Math.round(opponent.winRate) }}%</span>
                      <span class="text-2xs text-text-secondary">{{ opponent.wins }}-{{ opponent.losses }}</span>
                      <div class="matchup-tooltip">
                        <span class="font-medium">{{ opponent.opponentChampionName }}</span>
                        <span class="text-success">{{ Math.round(opponent.winRate) }}%</span>
                        <span class="text-text-secondary">({{ opponent.wins }}W-{{ opponent.losses }}L)</span>
                      </div>
                    </div>
                  </div>
                  <span v-else class="text-2xs text-text-secondary italic">No data yet</span>
                </div>

                <!-- Weak vs -->
                <div>
                  <div class="flex items-center gap-1.5 mb-1.5">
                    <span class="w-1.5 h-1.5 rounded-full bg-error"></span>
                    <span class="text-2xs text-text-secondary uppercase tracking-wide font-medium">Weak vs</span>
                  </div>
                  <div v-if="getMatchupsForChampion(champion.championId, role).bad.length > 0" class="flex gap-1.5">
                    <div
                      v-for="opponent in getMatchupsForChampion(champion.championId, role).bad"
                      :key="opponent.opponentChampionId"
                      class="matchup-item relative flex flex-col items-center"
                    >
                      <img
                        class="w-8 h-8 rounded object-cover ring-1 ring-error/40"
                        :src="getChampionIconUrl(opponent.opponentChampionName)"
                        :alt="opponent.opponentChampionName"
                      />
                      <span class="text-2xs font-medium text-error mt-0.5">{{ Math.round(opponent.winRate) }}%</span>
                      <span class="text-2xs text-text-secondary">{{ opponent.wins }}-{{ opponent.losses }}</span>
                      <div class="matchup-tooltip">
                        <span class="font-medium">{{ opponent.opponentChampionName }}</span>
                        <span class="text-error">{{ Math.round(opponent.winRate) }}%</span>
                        <span class="text-text-secondary">({{ opponent.wins }}W-{{ opponent.losses }}L)</span>
                      </div>
                    </div>
                  </div>
                  <span v-else class="text-2xs text-text-secondary italic">No data yet</span>
                </div>
              </div>
            </article>
          </div>
        </TabPanel>
      </TabPanels>
    </TabGroup>

    <!-- Empty state when no data -->
    <template v-else>
      <header class="flex justify-between items-center gap-md mb-lg">
        <div>
          <h2 class="m-0 text-lg font-semibold text-text">Your Champions</h2>
          <p class="mt-1 mb-0 text-xs text-text-secondary">Top picks based on your performance</p>
        </div>
      </header>
      <div class="flex-1 flex items-center justify-center">
        <p class="m-0 text-sm text-text-secondary">No champion data yet. Play some games to see your best picks.</p>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { TabGroup, TabList, Tab, TabPanels, TabPanel } from '@headlessui/vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'
import { getChampionMatchups } from '../services/authApi'
import { formatRoleWithAdc as roleLabel, formatWinRate, formatLpPerGame } from '@/utils/formatters'

const props = defineProps({
  mainChampions: {
    type: Array,
    default: () => []
  },
  userId: {
    type: [Number, String],
    default: null
  },
  queueType: {
    type: String,
    default: 'all'
  },
  timeRange: {
    type: String,
    default: null
  }
})

// Matchups data fetched from API
const matchupsData = ref(null)
const matchupsLoading = ref(false)

// Selected tab index for HeadlessUI TabGroup
const selectedTabIndex = ref(0)

const roles = computed(() => props.mainChampions.map((role) => role.role))

const hasData = computed(() => props.mainChampions && props.mainChampions.length > 0)

// Handle tab change from HeadlessUI
function handleTabChange(index) {
  selectedTabIndex.value = index
}

// Get champions for a specific role
function championsForRole(role) {
  const entry = props.mainChampions.find((r) => r.role === role)
  return entry ? entry.champions || [] : []
}

watch(
  () => props.mainChampions,
  (newVal) => {
    if (!newVal || newVal.length === 0) {
      selectedTabIndex.value = 0
      return
    }

    // Keep the current selection if it still exists within bounds
    if (selectedTabIndex.value < newVal.length) {
      return
    }

    // Default to the most-played role (by total games across its top champs)
    let bestIndex = 0
    let bestGames = -1

    for (let i = 0; i < newVal.length; i++) {
      const roleEntry = newVal[i]
      const totalGames = (roleEntry.champions || []).reduce(
        (sum, c) => sum + (c.gamesPlayed || 0),
        0
      )
      if (totalGames > bestGames) {
        bestGames = totalGames
        bestIndex = i
      }
    }

    selectedTabIndex.value = bestIndex
  },
  { immediate: true, deep: true }
)

// Fetch matchups data when userId/filters change
async function fetchMatchups() {
  if (!props.userId) return

  matchupsLoading.value = true
  try {
    const data = await getChampionMatchups(props.userId, props.queueType, props.timeRange)
    matchupsData.value = data
  } catch (err) {
    console.error('Failed to fetch matchups:', err)
    matchupsData.value = null
  } finally {
    matchupsLoading.value = false
  }
}

// Watch for prop changes to refetch matchups
watch(
  () => [props.userId, props.queueType, props.timeRange],
  () => {
    fetchMatchups()
  },
  { immediate: true }
)

// Helper to calculate derived matchup stats from raw in-lane data
function calculateInLaneStats(opponent) {
  const wins = opponent.inLaneWins
  const losses = opponent.inLaneLosses
  const gamesPlayed = wins + losses
  const winRate = gamesPlayed > 0 ? (wins / gamesPlayed) * 100 : 0
  return {
    ...opponent,
    wins,
    losses,
    gamesPlayed,
    winRate
  }
}

// Get matchups for a specific champion in a specific role (min 3 in-lane games filter + win rate thresholds)
function getMatchupsForChampion(championId, role) {
  if (!matchupsData.value?.matchups) return { good: [], bad: [] }

  // Find matchup entry that matches BOTH championId AND role
  // This ensures we get the correct matchups when a champion is played in multiple roles
  const championMatchup = matchupsData.value.matchups.find(
    m => m.championId === championId && m.role === role
  )
  if (!championMatchup?.opponents) return { good: [], bad: [] }

  // Calculate in-lane stats and filter opponents with at least 3 in-lane games
  const validOpponents = championMatchup.opponents
    .map(calculateInLaneStats)
    .filter(o => o.gamesPlayed >= 3)

  // Strong matchups: win rate > 50%, sorted by highest first
  const strongMatchups = validOpponents
    .filter(o => o.winRate > 50)
    .sort((a, b) => b.winRate - a.winRate)
    .slice(0, 4)

  // Weak matchups: win rate < 50%, sorted by lowest first
  const weakMatchups = validOpponents
    .filter(o => o.winRate < 50)
    .sort((a, b) => a.winRate - b.winRate)
    .slice(0, 4)

  return { good: strongMatchups, bad: weakMatchups }
}

// Data Dragon version for champion icons (kept in sync with ProfileHeaderCard)
const ddVersion = '16.1.1'

function normalizeChampionName(name) {
  if (!name) return ''
  // Remove spaces, punctuation, etc. (e.g., "Cho'Gath" -> "ChoGath")
  return name.replace(/[^A-Za-z0-9]/g, '')
}

function getChampionIconUrl(name) {
  const normalized = normalizeChampionName(name)
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/champion/${normalized}.png`
}
</script>

<style>
/* Note: Using non-scoped style block to access global winrate-* classes from style.css */
</style>

<style scoped>
/* Player card styling */
.player-card {
  min-height: 360px;
}

/* Matchup tooltip styles */
.matchup-item {
  cursor: pointer;
  transition: transform 0.15s ease;
}

.matchup-item:hover {
  transform: scale(1.1);
  z-index: 5;
}

.matchup-tooltip {
  position: absolute;
  bottom: 100%;
  left: 50%;
  transform: translateX(-50%);
  margin-bottom: 8px;
  padding: 8px 12px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  white-space: nowrap;
  font-size: 0.75rem;
  color: var(--color-text);
  opacity: 0;
  visibility: hidden;
  transition: opacity 0.15s ease, visibility 0.15s ease;
  z-index: 50;
  display: flex;
  gap: 6px;
  align-items: center;
}

.matchup-tooltip::after {
  content: '';
  position: absolute;
  top: 100%;
  left: 50%;
  transform: translateX(-50%);
  border: 5px solid transparent;
  border-top-color: var(--color-border);
}

.matchup-item:hover .matchup-tooltip {
  opacity: 1;
  visibility: visible;
}

/* Responsive adjustments */
@media (max-width: 1024px) {
  .player-card {
    min-height: 320px;
  }
}

@media (max-width: 768px) {
  .grid-cols-3 {
    grid-template-columns: 1fr;
  }

  .player-card {
    min-height: auto;
  }
}
</style>
