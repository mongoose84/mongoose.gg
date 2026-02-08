<template>
  <BaseCard class="summary-stats-card" data-testid="summary-stats-card">
    <div class="card-content">
      <!-- Loading state -->
      <div v-if="loading" class="stats-grid" data-testid="loading-state">
        <div v-for="i in 3" :key="i" class="stat-item">
          <div class="skeleton skeleton-label" />
          <div class="skeleton skeleton-value" />
        </div>
      </div>

      <!-- Empty state -->
      <div v-else-if="isEmpty" class="empty-state" data-testid="empty-state">
        <p class="empty-text">No games found for this filter</p>
      </div>

      <!-- Stats display -->
      <div v-else class="stats-grid" data-testid="stats-display">
        <!-- Games Played -->
        <div class="stat-item">
          <span class="stat-label">Games</span>
          <span class="stat-value">{{ gamesPlayed }}</span>
        </div>

        <!-- Winrate -->
        <div class="stat-item">
          <span class="stat-label">Winrate</span>
          <span class="stat-value" :class="winrateColorClass">
            {{ formattedWinrate }}
          </span>
        </div>

        <!-- Average KDA -->
        <div class="stat-item">
          <span class="stat-label">Avg KDA</span>
          <span class="stat-value">{{ formattedKda }}</span>
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup>
import { computed } from 'vue'
import BaseCard from '../base/BaseCard.vue'
import { getWinRateColorClass } from '../../composables/useWinRateColor'

const props = defineProps({
  /** Number of games played */
  gamesPlayed: {
    type: Number,
    default: 0
  },
  /** Win rate percentage (0-100) */
  winRate: {
    type: Number,
    default: null
  },
  /** Average KDA ratio */
  avgKda: {
    type: Number,
    default: null
  },
  /** Loading state */
  loading: {
    type: Boolean,
    default: false
  }
})

// Computed: Check if data is empty
const isEmpty = computed(() => {
  return !props.loading && props.gamesPlayed === 0
})

// Computed: Formatted winrate
const formattedWinrate = computed(() => {
  if (props.winRate === null || props.winRate === undefined) return '--'
  return `${props.winRate.toFixed(1)}%`
})

// Computed: Winrate color class
const winrateColorClass = computed(() => {
  return getWinRateColorClass(props.winRate)
})

// Computed: Formatted KDA
const formattedKda = computed(() => {
  if (props.avgKda === null || props.avgKda === undefined) return '--'
  return props.avgKda.toFixed(2)
})
</script>

<style scoped>
.summary-stats-card {
  width: 100%;
}

.card-content {
  padding: var(--spacing-md) var(--spacing-lg);
}

.stats-grid {
  display: flex;
  justify-content: space-around;
  align-items: center;
  gap: var(--spacing-lg);
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 500;
}

.stat-value {
  font-size: var(--font-size-xl);
  font-weight: 700;
  color: var(--color-text);
}

/* Empty state */
.empty-state {
  text-align: center;
  padding: var(--spacing-md) 0;
}

.empty-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Skeleton loading */
.skeleton {
  background: linear-gradient(90deg, var(--color-surface) 25%, var(--color-elevated) 50%, var(--color-surface) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

.skeleton-label {
  width: 48px;
  height: 12px;
}

.skeleton-value {
  width: 64px;
  height: 28px;
  margin-top: var(--spacing-xs);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* Responsive: stack on mobile */
@media (max-width: 480px) {
  .stats-grid {
    flex-direction: column;
    gap: var(--spacing-md);
  }
}
</style>

