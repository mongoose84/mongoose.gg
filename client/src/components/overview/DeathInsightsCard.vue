<template>
  <section
    class="survival-check-card"
    :class="borderClass"
    data-testid="death-insights-card"
    aria-label="Death insight: win rate correlation with deaths"
  >
    <!-- Loading skeleton -->
    <div v-if="loading" class="death-skeleton" data-testid="death-insight-skeleton">
      <div class="skeleton-label"></div>
      <div class="skeleton-wr"></div>
      <div class="skeleton-strip"></div>
    </div>

    <!-- Content states -->
    <template v-else>
      <div class="death-foreground">
        <span class="death-label">DEATH INSIGHT</span>

        <!-- Motivational state -->
        <template v-if="headlineState === 'motivational'">
          <div class="death-hero-row">
            <span class="death-hero-wr" :class="getWinRateColorClass(Math.round(survivalStats.winRateLowDeaths * 100))" data-testid="hero-wr">{{ Math.round(survivalStats.winRateLowDeaths * 100) }}%</span>
            <span class="death-hero-text">win rate when you die ≤{{ survivalStats.lowDeathThreshold }} times</span>
          </div>
          <div class="death-contrast-row" data-testid="contrast-row">
            <span class="death-contrast-prefix">vs</span>
            <span class="death-contrast-wr" :class="getWinRateColorClass(Math.round(survivalStats.winRateHighDeaths * 100))">{{ Math.round(survivalStats.winRateHighDeaths * 100) }}%</span>
            <span class="death-contrast-text">when you die {{ survivalStats.highDeathThreshold }}+ times</span>
          </div>
        </template>

        <!-- Warning state -->
        <template v-else-if="headlineState === 'warning'">
          <div class="death-hero-row">
            <span class="death-hero-wr" :class="getWinRateColorClass(Math.round(survivalStats.winRateHighDeaths * 100))" data-testid="hero-wr">{{ Math.round(survivalStats.winRateHighDeaths * 100) }}%</span>
            <span class="death-hero-text">win rate when {{ survivalStats.highDeathThreshold }}+ deaths</span>
          </div>
          <div class="death-contrast-row" data-testid="contrast-row">
            <span class="death-contrast-prefix">vs</span>
            <span class="death-contrast-wr" :class="getWinRateColorClass(Math.round(survivalStats.winRateLowDeaths * 100))">{{ Math.round(survivalStats.winRateLowDeaths * 100) }}%</span>
            <span class="death-contrast-text">under that — a {{ Math.round((survivalStats.winRateLowDeaths - survivalStats.winRateHighDeaths) * 100) }}pt gap</span>
          </div>
        </template>

        <!-- Neutral state -->
        <template v-else-if="headlineState === 'neutral'">
          <div class="death-hero-row">
            <span class="death-hero-wr winrate-neutral" data-testid="hero-wr">{{ survivalStats.avgDeathsPerGame.toFixed(1) }}</span>
            <span class="death-hero-text">avg deaths/game</span>
          </div>
          <p class="death-neutral-context" :class="neutralContext.colorClass" data-testid="neutral-context">{{ neutralContext.text }}</p>
        </template>

        <!-- Empty state -->
        <template v-else>
          <p class="death-empty-text" data-testid="death-insight-empty">Play a few games to unlock death insights</p>
        </template>

        <!-- Divider + footer (only for motivational/warning — neutral state already shows avg deaths in hero) -->
        <template v-if="headlineState === 'motivational' || headlineState === 'warning'">
          <div class="death-divider" aria-hidden="true"></div>
          <p class="death-footer" data-testid="death-insight-footer">
            Your avg: {{ survivalStats.avgDeathsPerGame.toFixed(1) }} deaths/game
          </p>
        </template>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed } from 'vue'
import { getWinRateColorClass } from '@/composables/useWinRateColor'

const props = defineProps({
  survivalStats: { type: Object, default: null },
  loading: { type: Boolean, default: false }
})

const neutralContext = computed(() => {
  const s = props.survivalStats
  if (!s) return { text: '', colorClass: 'context-muted' }

  const avg = s.avgDeathsPerGame
  const low = s.lowDeathThreshold
  const high = s.highDeathThreshold

  if (avg <= low)
    return { text: `Within target range (≤ ${low})`, colorClass: 'context-success' }
  if (avg >= high)
    return { text: `Above danger zone (${high}+)`, colorClass: 'context-error' }

  const midpoint = (low + high) / 2
  if (avg > midpoint)
    return { text: `Approaching danger zone (${high}+)`, colorClass: 'context-warning' }

  return { text: `Room to improve — target ≤ ${low}`, colorClass: 'context-muted' }
})

const headlineState = computed(() => {
  const s = props.survivalStats
  if (!s || s.totalGames === 0) return 'empty'

  const low = s.winRateLowDeaths
  const high = s.winRateHighDeaths

  if (low != null && high != null) {
    const gap = low - high
    if (low >= 0.55 && gap >= 0.15) return 'motivational'
    if (high <= 0.45 && gap >= 0.15) return 'warning'
  }

  return 'neutral'
})

const borderClass = computed(() => {
  const s = props.survivalStats
  if (!s) return 'border-default'
  if (s.avgDeathsPerGame <= s.lowDeathThreshold) return 'border-success'
  if (s.avgDeathsPerGame >= s.highDeathThreshold) return 'border-error'
  return 'border-default'
})
</script>

<style scoped>
.survival-check-card {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
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

.survival-check-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

/* Left border accent colors */
.survival-check-card.border-success {
  border-left-color: var(--color-success-border);
}

.survival-check-card.border-error {
  border-left-color: var(--color-error-border);
}

/* Foreground content */
.death-foreground {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  height: 100%;
}

/* Section label */
.death-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-secondary);
}

/* Hero row */
.death-hero-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.death-hero-wr {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  line-height: 1;
}

.death-hero-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Contrast row */
.death-contrast-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-xs);
  flex-wrap: wrap;
}

.death-contrast-prefix {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.death-contrast-wr {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  line-height: 1;
}

.death-contrast-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Divider */
.death-divider {
  border: none;
  border-top: 1px solid var(--color-border);
  margin-top: auto;
}

/* Footer */
.death-footer {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

/* Neutral state context qualifier */
.death-neutral-context {
  margin: 0;
  font-size: var(--font-size-xs);
}

.death-neutral-context.context-success { color: var(--color-success); }
.death-neutral-context.context-error   { color: var(--color-error); }
.death-neutral-context.context-warning { color: var(--color-warning); }
.death-neutral-context.context-muted   { color: var(--color-text-secondary); }

/* Empty state */
.death-empty-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Skeleton */
.death-skeleton {
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

.winrate-neutral { color: var(--color-text); }
</style>
