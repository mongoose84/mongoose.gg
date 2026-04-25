<template>
  <section
    class="today-session-card"
    :class="accentClass"
    data-testid="today-session-card"
    aria-label="Today's session summary"
  >
    <!-- Loading skeleton -->
    <div v-if="loading" class="session-skeleton" data-testid="session-skeleton">
      <div class="skeleton-label"></div>
      <div class="skeleton-wr"></div>
      <div class="skeleton-strip"></div>
    </div>

    <!-- Content -->
    <template v-else>
      <div class="session-foreground">
        <!-- Top row: [◂ LABEL ▸] [dots] [champion badge] -->
        <div class="session-top-row">
          <div class="session-nav-group">
            <button
              class="session-nav-btn"
              @click="prev"
              @keydown.left="prev"
              @keydown.right="next"
              aria-label="Previous time period"
              data-testid="session-prev-btn"
            >
              <ChevronLeftIcon class="nav-icon" aria-hidden="true" />
            </button>
            <span class="session-label" aria-live="polite" aria-atomic="true">{{ pageLabel }}</span>
            <button
              class="session-nav-btn"
              @click="next"
              @keydown.left="prev"
              @keydown.right="next"
              aria-label="Next time period"
              data-testid="session-next-btn"
            >
              <ChevronRightIcon class="nav-icon" aria-hidden="true" />
            </button>
          </div>

          <div class="session-dots" aria-hidden="true">
            <span class="session-dot" :class="{ active: currentPage === 0 }"></span>
            <span class="session-dot" :class="{ active: currentPage === 1 }"></span>
            <span class="session-dot" :class="{ active: currentPage === 2 }"></span>
          </div>

          <div v-if="bestChampion" class="session-champion" data-testid="champion-badge">
            <img
              :src="championIconUrl"
              :alt="bestChampion.championName"
              class="champion-icon"
            />
            <span class="champion-name">{{ bestChampion.championName }}</span>
          </div>
        </div>

        <!-- Stats body — crossfade on page change -->
        <Transition name="session-fade" mode="out-in">
          <div :key="currentPage" class="session-body">
            <div class="session-stats-row">
              <span class="session-wr" :class="winRateClass" data-testid="win-rate">{{ winRateDisplay }}%</span>
              <span class="session-detail">{{ detailDisplay }}</span>
            </div>
            <div v-if="wlDots.length > 0" class="session-strip" data-testid="wl-strip">
              <div
                v-for="(isWin, index) in wlDots"
                :key="index"
                class="wl-indicator"
                :class="isWin ? 'win' : 'loss'"
                :aria-label="isWin ? 'Win' : 'Loss'"
              ></div>
            </div>
          </div>
        </Transition>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/vue/24/solid'
import { getWinRateColorClass } from '@/composables/useWinRateColor'
import { getChampionIconUrl } from '@/utils/leagueAssets'

const PAGE_COUNT = 3

const props = defineProps({
  sessionStats: { type: Object, default: null },
  combinedStats: { type: Object, default: null },
  loading: { type: Boolean, default: false }
})

// Waterfall: determine the most relevant starting page
const defaultPage = computed(() => {
  if (props.sessionStats?.gamesToday > 0) return 0
  if (props.sessionStats?.gamesThisWeek > 0) return 1
  return 2
})

const currentPage = ref(defaultPage.value)

// Reset to the most relevant page when data loads/changes
watch(defaultPage, (val) => {
  currentPage.value = val
})

function prev() {
  currentPage.value = (currentPage.value + PAGE_COUNT - 1) % PAGE_COUNT
}

function next() {
  currentPage.value = (currentPage.value + 1) % PAGE_COUNT
}

const pageLabel = computed(() => {
  if (currentPage.value === 0) return "TODAY'S SESSION"
  if (currentPage.value === 1) return 'THIS WEEK'
  return 'THIS SEASON'
})

const bestChampion = computed(() => {
  if (currentPage.value !== 0) return null
  return props.sessionStats?.bestChampionToday ?? null
})

const championIconUrl = computed(() => {
  if (!bestChampion.value) return null
  return getChampionIconUrl(bestChampion.value.championName)
})

const winRate = computed(() => {
  if (currentPage.value === 0) {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    const total = wins + losses
    return total > 0 ? Math.round((wins / total) * 100) : null
  }
  if (currentPage.value === 1) {
    const wins = props.sessionStats?.winsThisWeek ?? 0
    const losses = props.sessionStats?.lossesThisWeek ?? 0
    const total = wins + losses
    return total > 0 ? Math.round((wins / total) * 100) : null
  }
  const wr = props.combinedStats?.winRate
  return wr != null ? Math.round(wr) : null
})

const winRateDisplay = computed(() => winRate.value ?? '--')

const winRateClass = computed(() => getWinRateColorClass(winRate.value))

const accentClass = computed(() => {
  const wr = winRate.value
  if (wr == null) return 'accent-neutral'
  if (wr >= 55) return 'accent-success'
  if (wr < 45) return 'accent-error'
  return 'accent-neutral'
})

const detailDisplay = computed(() => {
  if (currentPage.value === 0) {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    const kda = props.sessionStats?.avgKdaToday
    const kdaPart = kda != null ? ` · ${kda.toFixed(1)} KDA` : ''
    return `${wins}W ${losses}L${kdaPart}`
  }
  if (currentPage.value === 1) {
    const wins = props.sessionStats?.winsThisWeek ?? 0
    const losses = props.sessionStats?.lossesThisWeek ?? 0
    const kda = props.sessionStats?.avgKdaThisWeek
    const kdaPart = kda != null ? ` · ${kda.toFixed(1)} KDA` : ''
    return `${wins}W ${losses}L${kdaPart}`
  }
  const total = props.combinedStats?.totalGames
  const kda = props.combinedStats?.avgKda
  const kdaPart = kda != null ? ` · ${kda.toFixed(1)} KDA` : ''
  return total != null ? `${total} games${kdaPart}` : '--'
})

const wlDots = computed(() => {
  if (currentPage.value === 2) return []
  if (currentPage.value === 0) {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    return [...Array(losses).fill(false), ...Array(wins).fill(true)]
  }
  if (currentPage.value === 1) {
    const wins = props.sessionStats?.winsThisWeek ?? 0
    const losses = props.sessionStats?.lossesThisWeek ?? 0
    return [...Array(losses).fill(false), ...Array(wins).fill(true)]
  }
  return []
})


</script>

<style scoped>
.today-session-card {
  position: relative;
  overflow: hidden;
  isolation: isolate;
  display: flex;
  flex-direction: column;
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border-top: 1px solid var(--color-border);
  border-right: 1px solid var(--color-border);
  border-bottom: 1px solid var(--color-border);
  border-left: 3px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  transition: all 0.2s ease;
  min-height: 140px;
}

.today-session-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

/* Left border accent colors */
.today-session-card.accent-success {
  border-left-color: var(--color-success-border);
}

.today-session-card.accent-error {
  border-left-color: var(--color-error-border);
}

/* Foreground content */
.session-foreground {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  height: 100%;
}

/* Top row: nav group + dots + optional champion badge */
.session-top-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

/* Nav group: prev button + label + next button */
.session-nav-group {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  flex-shrink: 0;
}

.session-nav-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  padding: 2px;
  background: none;
  border: none;
  border-radius: var(--radius-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: color 0.15s ease, background-color 0.15s ease;
  flex-shrink: 0;
}

.session-nav-btn:hover {
  color: var(--color-text);
  background-color: var(--color-elevated);
}

.session-nav-btn:active {
  transform: scale(0.9);
}

.session-nav-btn:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 1px;
}

.nav-icon {
  width: 12px;
  height: 12px;
}

.session-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-secondary);
  white-space: nowrap;
}

/* Dot indicators */
.session-dots {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.session-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--color-elevated);
  transition: background-color 0.2s ease;
  flex-shrink: 0;
}

.session-dot.active {
  background: var(--color-primary);
}

/* Champion badge — pushed to the right */
.session-champion {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  flex-shrink: 0;
  margin-left: auto;
}

.champion-icon {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: cover;
}

.champion-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Stats body */
.session-body {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

/* Stats row */
.session-stats-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.session-wr {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  line-height: 1;
}

.session-detail {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* W/L strip */
.session-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 3px;
  margin-top: var(--spacing-xs);
}

.wl-indicator {
  width: 12px;
  height: 12px;
  border-radius: 2px;
  flex-shrink: 0;
}

.wl-indicator.win {
  background: var(--color-success);
  opacity: 0.8;
}

.wl-indicator.loss {
  background: var(--color-error);
  opacity: 0.8;
}

/* Crossfade transition */
.session-fade-enter-active,
.session-fade-leave-active {
  transition: opacity 0.15s ease;
}

.session-fade-enter-from,
.session-fade-leave-to {
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .session-fade-enter-active,
  .session-fade-leave-active {
    transition: none;
  }
}

/* Skeleton */
.session-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  animation: skeleton-pulse 1.5s ease-in-out infinite;
}

.skeleton-label {
  width: 80px;
  height: 12px;
  background: var(--color-elevated);
  border-radius: 4px;
}

.skeleton-wr {
  width: 60px;
  height: 40px;
  background: var(--color-elevated);
  border-radius: 4px;
}

.skeleton-strip {
  width: 100%;
  height: 12px;
  background: var(--color-elevated);
  border-radius: 4px;
}

@keyframes skeleton-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
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
