<template>
  <div class="impact-stats" data-testid="impact-stats">
    <h3 class="section-title">Impact Stats</h3>

    <div v-if="loading" class="loading-state" data-testid="loading-state">
      <div v-for="n in 3" :key="n" class="skeleton skeleton-stat" />
    </div>

    <div v-else-if="stats" class="stats-grid">
      <div
        v-for="stat in displayedStats"
        :key="stat.key"
        class="stat-cell"
        :class="stat.sentiment ? `stat-cell--${stat.sentiment}` : ''"
        :data-testid="`impact-stat-${stat.key}`"
      >
        <p class="stat-label">{{ stat.label }}</p>
        <p class="stat-value">{{ stat.value }}</p>
        <p v-if="stat.context" class="stat-context">{{ stat.context }}</p>
      </div>
    </div>

    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No impact data available</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  /** Participant role: 'support' or any other role */
  role: {
    type: String,
    default: null
  },
  /** Kill participation percentage (0–100) */
  killParticipation: {
    type: Number,
    default: null
  },
  /** Gold at 15 minutes */
  goldAt15: {
    type: Number,
    default: null
  },
  /** Vision score per minute (support role) */
  visionPerMin: {
    type: Number,
    default: null
  },
  /** Damage / gold efficiency (non-support) */
  damageGoldEfficiency: {
    type: Number,
    default: null
  },
  /** Loading state */
  loading: {
    type: Boolean,
    default: false
  }
})

const isSupport = computed(() => props.role?.toLowerCase() === 'support')

const stats = computed(() => {
  const hasData =
    props.killParticipation !== null ||
    props.goldAt15 !== null ||
    props.visionPerMin !== null ||
    props.damageGoldEfficiency !== null

  return hasData ? true : null
})

const displayedStats = computed(() => {
  const result = [
    {
      key: 'kp',
      label: 'Kill Participation',
      value: props.killParticipation !== null ? `${Math.round(props.killParticipation)}%` : '—',
      sentiment: getSentiment(props.killParticipation, 50, 35)
    },
    {
      key: 'gold15',
      label: 'Gold @15',
      value: props.goldAt15 !== null ? formatGold(props.goldAt15) : '—',
      sentiment: getSentiment(props.goldAt15, 6000, 4500)
    }
  ]

  if (isSupport.value) {
    result.push({
      key: 'vision',
      label: 'Vision/min',
      value: props.visionPerMin !== null ? props.visionPerMin.toFixed(1) : '—',
      sentiment: getSentiment(props.visionPerMin, 1.5, 0.8)
    })
  } else {
    result.push({
      key: 'dmg-gold',
      label: 'Dmg/Gold',
      value: props.damageGoldEfficiency !== null ? props.damageGoldEfficiency.toFixed(2) : '—',
      sentiment: getSentiment(props.damageGoldEfficiency, 1.2, 0.7)
    })
  }

  return result
})

function getSentiment(value, goodThreshold, badThreshold) {
  if (value === null || value === undefined) return null
  if (value >= goodThreshold) return 'positive'
  if (value <= badThreshold) return 'negative'
  return null
}

function formatGold(gold) {
  if (gold >= 1000) return `${(gold / 1000).toFixed(1)}k`
  return String(gold)
}
</script>

<style scoped>
.impact-stats {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.section-title {
  margin: 0 0 var(--spacing-md) 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-md);
}

.stat-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--spacing-sm);
  border-radius: var(--radius-md);
  background: var(--color-elevated);
  border: 1px solid transparent;
}

.stat-cell--positive {
  border-color: var(--color-success-border);
}

.stat-cell--negative {
  border-color: var(--color-error-border);
}

.stat-label {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
}

.stat-value {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  line-height: 1.2;
}

.stat-context {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

/* Loading skeleton */
.loading-state {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-md);
}

.skeleton {
  background: linear-gradient(90deg, var(--color-surface) 25%, var(--color-elevated) 50%, var(--color-surface) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-md);
}

.skeleton-stat {
  height: 70px;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
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

@media (max-width: 640px) {
  .stats-grid,
  .loading-state {
    grid-template-columns: 1fr;
  }
}
</style>
