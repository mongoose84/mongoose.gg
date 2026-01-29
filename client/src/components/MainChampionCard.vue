<template>
  <section class="bg-background-surface border border-border rounded-lg p-lg h-full flex flex-col">
    <TabGroup v-if="hasData" :selected-index="selectedTabIndex" @change="handleTabChange" as="div" class="flex-1 flex flex-col">
      <header class="flex justify-between items-start gap-md mb-md">
        <div class="flex-1">
          <h2 class="m-0 text-lg font-semibold text-text">Main Champions by Role</h2>
          <p class="mt-1 mb-0 text-xs text-text-secondary">Top picks based on your performance in the selected queue and time range.</p>
        </div>
        <TabList class="flex flex-wrap gap-sm flex-shrink-0">
          <Tab
            v-for="role in roles"
            :key="role"
            v-slot="{ selected }"
            as="template"
          >
            <button
              type="button"
              :class="[
                'py-1.5 px-3 rounded-full border text-xs font-medium cursor-pointer transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background-surface',
                selected
                  ? 'bg-primary border-primary text-white'
                  : 'border-border bg-background-elevated text-text-secondary hover:border-primary hover:text-text'
              ]"
            >
              {{ roleLabel(role) }}
            </button>
          </Tab>
        </TabList>
      </header>

      <TabPanels class="flex-1 flex flex-col">
        <TabPanel
          v-for="role in roles"
          :key="role"
          class="flex-1 content-center flex flex-wrap gap-md focus:outline-none"
        >
          <article
            v-for="(champion, index) in championsForRole(role)"
            :key="champion.championId"
            class="champion-card flex-[0_0_calc((100%-2*var(--spacing-md))/3)] p-xl rounded-md bg-background-elevated border border-border flex flex-col justify-between gap-lg transition-all duration-150"
            :class="{ 'border-primary shadow-sm -translate-y-px hover:shadow-md hover:-translate-y-0.5': index === 0 }"
          >
            <div class="flex justify-between items-center">
              <div class="flex items-center gap-sm">
                <img
                  class="w-14 h-14 rounded-md object-cover"
                  :src="getChampionIconUrl(champion.championName)"
                  :alt="`${champion.championName} icon`"
                />
                <div class="flex flex-col gap-1">
                  <div class="flex items-center gap-1.5">
                    <span class="text-lg font-semibold text-text">{{ champion.championName }}</span>
                    <span class="text-xs uppercase py-0.5 px-1.5 rounded-sm bg-[rgba(148,163,184,0.2)] text-text-secondary">{{ roleLabel(role) }}</span>
                  </div>
                  <span v-if="index === 0" class="text-2xs font-semibold text-primary uppercase tracking-wide">Recommended</span>
                </div>
              </div>
            </div>
            <div class="flex justify-between gap-md">
              <div class="flex flex-col gap-1">
                <span :class="['stat-value text-lg font-bold text-text', getWinRateColorClass(champion.winRate)]">{{ formatWinRate(champion.winRate) }}</span>
                <span class="text-2xs text-text-secondary">Win Rate</span>
              </div>
              <div class="flex flex-col gap-1">
                <span class="text-lg font-bold text-text">{{ formatLpPerGame(champion.lpPerGame) }}</span>
                <span class="text-2xs text-text-secondary">LP / game</span>
              </div>
              <div class="flex flex-col gap-1">
                <span class="text-lg font-bold text-text">{{ champion.wins }}-{{ champion.losses }}</span>
                <span class="text-2xs text-text-secondary">{{ champion.gamesPlayed }} games</span>
              </div>
            </div>

            <!-- Matchups Section (always shown) -->
            <div class="flex gap-lg pt-md border-t border-border">
              <!-- Good Matchups -->
              <div class="flex-1">
                <span class="text-2xs text-text-secondary uppercase tracking-wide mb-xs block">Strong vs</span>
                <div v-if="getMatchupsForChampion(champion.championId, role).good.length > 0" class="flex gap-xs">
                  <div
                    v-for="opponent in getMatchupsForChampion(champion.championId, role).good"
                    :key="opponent.opponentChampionId"
                    class="matchup-item flex flex-col items-center gap-0.5 relative"
                  >
                    <img
                      class="w-8 h-8 rounded-md object-cover border border-success"
                      :src="getChampionIconUrl(opponent.opponentChampionName)"
                      :alt="opponent.opponentChampionName"
                    />
                    <span class="text-2xs font-medium text-success">{{ Math.round(opponent.winRate) }}%</span>
                    <div class="matchup-tooltip">
                      <span class="font-medium">{{ opponent.opponentChampionName }}</span>
                      <span class="text-success">{{ opponent.wins }}W</span>/<span class="text-error">{{ opponent.losses }}L</span>
                    </div>
                  </div>
                </div>
                <span v-else class="text-2xs text-text-secondary italic h-[52px] flex items-center">Not enough data</span>
              </div>

              <!-- Bad Matchups -->
              <div class="flex-1">
                <span class="text-2xs text-text-secondary uppercase tracking-wide mb-xs block">Weak vs</span>
                <div v-if="getMatchupsForChampion(champion.championId, role).bad.length > 0" class="flex gap-xs">
                  <div
                    v-for="opponent in getMatchupsForChampion(champion.championId, role).bad"
                    :key="opponent.opponentChampionId"
                    class="matchup-item flex flex-col items-center gap-0.5 relative"
                  >
                    <img
                      class="w-8 h-8 rounded-md object-cover border border-error"
                      :src="getChampionIconUrl(opponent.opponentChampionName)"
                      :alt="opponent.opponentChampionName"
                    />
                    <span class="text-2xs font-medium text-error">{{ Math.round(opponent.winRate) }}%</span>
                    <div class="matchup-tooltip">
                      <span class="font-medium">{{ opponent.opponentChampionName }}</span>
                      <span class="text-success">{{ opponent.wins }}W</span>/<span class="text-error">{{ opponent.losses }}L</span>
                    </div>
                  </div>
                </div>
                <span v-else class="text-2xs text-text-secondary italic h-[52px] flex items-center">Not enough data</span>
              </div>
            </div>
          </article>
        </TabPanel>
      </TabPanels>
    </TabGroup>

    <!-- Empty state when no data -->
    <template v-else>
      <header class="flex justify-between items-start gap-md mb-md">
        <div class="flex-1">
          <h2 class="m-0 text-lg font-semibold text-text">Main Champions by Role</h2>
          <p class="mt-1 mb-0 text-xs text-text-secondary">Top picks based on your performance in the selected queue and time range.</p>
        </div>
      </header>
      <div class="pt-sm text-sm text-text-secondary">
        <p class="m-0">No champion data yet for this filter. Play some games to see your best picks.</p>
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
    .slice(0, 3)

  // Weak matchups: win rate < 50%, sorted by lowest first
  const weakMatchups = validOpponents
    .filter(o => o.winRate < 50)
    .sort((a, b) => a.winRate - b.winRate)
    .slice(0, 3)

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
/* Responsive layout for champion cards */
@media (max-width: 768px) {
  .champion-card {
    flex: 0 0 100%;
    max-width: 280px;
  }
}

/* Matchup tooltip styles */
.matchup-item {
  cursor: pointer;
}

.matchup-tooltip {
  position: absolute;
  bottom: 100%;
  left: 50%;
  transform: translateX(-50%);
  margin-bottom: 6px;
  padding: 6px 10px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-md);
  white-space: nowrap;
  font-size: 0.75rem;
  color: var(--color-text);
  opacity: 0;
  visibility: hidden;
  transition: opacity 0.15s ease, visibility 0.15s ease;
  z-index: 10;
  display: flex;
  gap: 4px;
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
</style>
