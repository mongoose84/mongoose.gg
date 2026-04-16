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

    <!-- TODAY'S SESSION (mural + champion badge) -->
    <template v-else-if="state === 'today'">
      <div v-if="hasMural" class="session-mural-layer" aria-hidden="true">
        <img :src="muralUrl" alt="" class="session-mural-image" @error="handleMuralError" />
      </div>
      <div v-if="hasMural" class="session-overlay-layer" aria-hidden="true"></div>
      <div class="session-foreground">
        <div class="session-top-row">
          <span class="session-label">TODAY'S SESSION</span>
          <div v-if="bestChampion" class="session-champion" data-testid="champion-badge">
            <img
              :src="championIconUrl"
              :alt="bestChampion.championName"
              class="champion-icon"
            />
            <span class="champion-name">{{ bestChampion.championName }}</span>
          </div>
        </div>
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
    </template>

    <!-- THIS WEEK (no mural, no champion) -->
    <template v-else-if="state === 'week'">
      <div class="session-foreground">
        <div class="session-top-row">
          <span class="session-label">THIS WEEK</span>
        </div>
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
    </template>

    <!-- THIS SEASON (combinedStats, no strip) -->
    <template v-else>
      <div class="session-foreground">
        <div class="session-top-row">
          <span class="session-label">THIS SEASON</span>
        </div>
        <div class="session-stats-row">
          <span class="session-wr" :class="winRateClass" data-testid="win-rate">{{ winRateDisplay }}%</span>
          <span class="session-detail">{{ detailDisplay }}</span>
        </div>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { getWinRateColorClass } from '@/composables/useWinRateColor'
import { getChampionIconUrl, getChampionSplashUrl } from '@/utils/leagueAssets'

const props = defineProps({
  sessionStats: { type: Object, default: null },
  combinedStats: { type: Object, default: null },
  loading: { type: Boolean, default: false }
})

const muralErrored = ref(false)

const state = computed(() => {
  if (props.sessionStats?.gamesToday > 0) return 'today'
  if (props.sessionStats?.gamesThisWeek > 0) return 'week'
  return 'season'
})

const bestChampion = computed(() => {
  if (state.value !== 'today') return null
  return props.sessionStats?.bestChampionToday ?? null
})

const muralUrl = computed(() => {
  if (!bestChampion.value) return null
  return getChampionSplashUrl(bestChampion.value.championName)
})

const hasMural = computed(() => Boolean(muralUrl.value) && !muralErrored.value)

const championIconUrl = computed(() => {
  if (!bestChampion.value) return null
  return getChampionIconUrl(bestChampion.value.championName)
})

const winRate = computed(() => {
  if (state.value === 'today') {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    const total = wins + losses
    return total > 0 ? Math.round((wins / total) * 100) : null
  }
  if (state.value === 'week') {
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
  if (state.value === 'today') {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    const kda = props.sessionStats?.avgKdaToday
    const kdaPart = kda != null ? ` · ${kda.toFixed(1)} KDA` : ''
    return `${wins}W ${losses}L${kdaPart}`
  }
  if (state.value === 'week') {
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
  if (state.value === 'season') return []
  if (state.value === 'today') {
    const wins = props.sessionStats?.winsToday ?? 0
    const losses = props.sessionStats?.lossesToday ?? 0
    return [...Array(losses).fill(false), ...Array(wins).fill(true)]
  }
  if (state.value === 'week') {
    const wins = props.sessionStats?.winsThisWeek ?? 0
    const losses = props.sessionStats?.lossesThisWeek ?? 0
    return [...Array(losses).fill(false), ...Array(wins).fill(true)]
  }
  return []
})

function handleMuralError() {
  muralErrored.value = true
}

watch(() => props.sessionStats?.bestChampionToday?.championName, () => {
  muralErrored.value = false
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

/* Mural layers (same pattern as ChampionSelectCTA) */
.session-mural-layer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 1;
}

.session-mural-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  opacity: 0.5;
}

.session-overlay-layer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 2;
  background: linear-gradient(
    120deg,
    color-mix(in srgb, var(--color-surface) 98%, transparent) 0%,
    color-mix(in srgb, var(--color-surface) 92%, transparent) 45%,
    color-mix(in srgb, var(--color-surface) 78%, transparent) 100%
  );
}

/* Foreground content */
.session-foreground {
  position: relative;
  z-index: 3;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  height: 100%;
}

/* Top row: label + champion badge */
.session-top-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.session-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-secondary);
}

.session-champion {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
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
