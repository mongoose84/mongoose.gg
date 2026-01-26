<template>
  <section class="bg-background-surface border border-border rounded-lg p-lg h-full flex flex-col">
    <header class="flex justify-between items-start gap-md mb-md">
      <div class="flex-1">
        <h2 class="m-0 text-lg font-semibold text-text">Main Champions by Role</h2>
        <p class="mt-1 mb-0 text-xs text-text-secondary">Top picks based on your performance in the selected queue and time range.</p>
      </div>
      <div v-if="hasData" class="flex flex-wrap gap-sm flex-shrink-0" role="tablist">
        <button
          v-for="role in roles"
          :key="role"
          type="button"
          class="py-1.5 px-3 rounded-full border border-border bg-background-elevated text-text-secondary text-xs font-medium cursor-pointer transition-all duration-150 hover:border-primary hover:text-text"
          :class="{ 'bg-primary border-primary text-white': role === selectedRole }"
          @click="selectRole(role)"
          role="tab"
          :aria-selected="role === selectedRole"
        >
          {{ roleLabel(role) }}
        </button>
      </div>
    </header>

    <div v-if="hasData" class="flex-1 flex flex-col">
      <Transition name="fade-slide" mode="out-in">
        <div
          v-if="selectedRole"
          :key="selectedRole"
          class="flex-1 content-center flex flex-wrap gap-md"
          role="tabpanel"
        >
          <article
            v-for="(champion, index) in championsForSelectedRole"
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
                    <span class="text-xs uppercase py-0.5 px-1.5 rounded-sm bg-[rgba(148,163,184,0.2)] text-text-secondary">{{ roleLabel(selectedRole) }}</span>
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
          </article>
        </div>
      </Transition>
    </div>

    <div v-else class="pt-sm text-sm text-text-secondary">
      <p class="m-0">No champion data yet for this filter. Play some games to see your best picks.</p>
    </div>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'

const props = defineProps({
  mainChampions: {
    type: Array,
    default: () => []
  }
})

const selectedRole = ref(null)

const roles = computed(() => props.mainChampions.map((role) => role.role))

const hasData = computed(() => props.mainChampions && props.mainChampions.length > 0)

watch(
  () => props.mainChampions,
  (newVal) => {
    if (!newVal || newVal.length === 0) {
      selectedRole.value = null
      return
    }

    // Keep the current selection if it still exists
    if (selectedRole.value && newVal.some((r) => r.role === selectedRole.value)) {
      return
    }

    // Default to the most-played role (by total games across its top champs)
    let bestRole = newVal[0].role
    let bestGames = -1

    for (const roleEntry of newVal) {
      const totalGames = (roleEntry.champions || []).reduce(
        (sum, c) => sum + (c.gamesPlayed || 0),
        0
      )
      if (totalGames > bestGames) {
        bestGames = totalGames
        bestRole = roleEntry.role
      }
    }

    selectedRole.value = bestRole
  },
  { immediate: true, deep: true }
)

function selectRole(role) {
  selectedRole.value = role
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

function roleLabel(role) {
  const map = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support'
  }
  return map[role] || role
}

const championsForSelectedRole = computed(() => {
  if (!selectedRole.value) return []
  const entry = props.mainChampions.find((r) => r.role === selectedRole.value)
  return entry ? entry.champions || [] : []
})

function formatWinRate(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(1)}%`
}

function formatLpPerGame(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  const rounded = value.toFixed(1)
  const sign = value > 0 ? '+' : ''
  return `${sign}${rounded}`
}
</script>

<style scoped>
/* Win rate coloring gradient (dynamic classes can't be done with Tailwind) */
.stat-value.winrate-red {
  color: #ef4444;
}
.stat-value.winrate-redorange {
  color: #f97316;
}
.stat-value.winrate-orange {
  color: #fdba74;
}
.stat-value.winrate-yellow {
  color: #eab308;
}
.stat-value.winrate-yellowgreen {
  color: #84cc16;
}
.stat-value.winrate-green {
  color: #22c55e;
}
.stat-value.winrate-neutral {
  color: var(--color-text);
}

/* Vue Transition classes for tab switching animation */
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(4px);
}

/* Responsive layout for champion cards */
@media (max-width: 768px) {
  .champion-card {
    flex: 0 0 100%;
    max-width: 280px;
  }
}
</style>
