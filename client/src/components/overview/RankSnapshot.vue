<template>
  <div class="rank-snapshot" data-testid="rank-snapshot">
    <!-- Queue label -->
    <p v-if="primaryQueueLabel" class="queue-label">{{ primaryQueueLabel }}</p>

    <!-- Rank and LP -->
    <div class="rank-row">
      <BaseRankBadge
        v-if="rank"
        :tier="rankTier"
        :division="rankDivision"
        :lp="lp"
        :has-rank="!!rank"
        size="lg"
      />
      <span v-else class="unranked-label">Unranked</span>

      <span v-if="lpDeltaLast20 !== null && lpDeltaLast20 !== undefined" class="lp-delta" :class="lpDeltaClass">
        {{ lpDeltaLast20 >= 0 ? '+' : '' }}{{ lpDeltaLast20 }} LP
      </span>
    </div>

    <!-- Last 20 W/L strip -->
    <div v-if="wlLast20 && wlLast20.length" class="wl-strip" data-testid="wl-strip" role="img" :aria-label="`Last ${wlLast20.length} games: ${last20Wins} wins, ${last20Losses} losses`">
      <span
        v-for="(result, idx) in wlLast20"
        :key="idx"
        class="wl-cell"
        :class="result === 'W' || result === true ? 'wl-win' : 'wl-loss'"
      />
    </div>

    <!-- W/L summary -->
    <p v-if="last20Wins !== null || last20Losses !== null" class="wl-summary">
      <span class="win-count">{{ last20Wins }}W</span>
      <span class="separator"> / </span>
      <span class="loss-count">{{ last20Losses }}L</span>
      <span class="wl-label"> last 20</span>
    </p>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { BaseRankBadge } from '@/components/base'

const props = defineProps({
  /** Queue label, e.g. "Ranked Solo/Duo" */
  primaryQueueLabel: {
    type: String,
    default: null
  },
  /** Rank string, e.g. "Gold II" */
  rank: {
    type: String,
    default: null
  },
  /** Current LP */
  lp: {
    type: Number,
    default: null
  },
  /** LP change over last 20 games */
  lpDeltaLast20: {
    type: Number,
    default: null
  },
  /** Wins in last 20 games */
  last20Wins: {
    type: Number,
    default: null
  },
  /** Losses in last 20 games */
  last20Losses: {
    type: Number,
    default: null
  },
  /** Per-game W/L array for strip visualization (e.g. ['W','L','W',...]) */
  wlLast20: {
    type: Array,
    default: () => []
  }
})

/** Parse tier from rank string (e.g. "Gold II" → "GOLD") */
const rankTier = computed(() => {
  if (!props.rank) return null
  return props.rank.split(' ')[0]?.toUpperCase() || null
})

/** Parse division from rank string (e.g. "Gold II" → "II") */
const rankDivision = computed(() => {
  if (!props.rank) return null
  const parts = props.rank.split(' ')
  return parts[1] || null
})

const lpDeltaClass = computed(() => {
  if (props.lpDeltaLast20 === null || props.lpDeltaLast20 === undefined) return ''
  return props.lpDeltaLast20 >= 0 ? 'lp-delta--positive' : 'lp-delta--negative'
})
</script>

<style scoped>
.rank-snapshot {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.queue-label {
  margin: 0;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.rank-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.unranked-label {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  font-style: italic;
}

.lp-delta {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.lp-delta--positive {
  color: var(--color-success);
}

.lp-delta--negative {
  color: var(--color-error);
}

/* W/L strip */
.wl-strip {
  display: flex;
  gap: 2px;
  flex-wrap: wrap;
}

.wl-cell {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 2px;
}

.wl-win {
  background: var(--color-success);
}

.wl-loss {
  background: var(--color-error);
}

.wl-summary {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.win-count {
  color: var(--color-success);
  font-weight: var(--font-weight-semibold);
}

.loss-count {
  color: var(--color-error);
  font-weight: var(--font-weight-semibold);
}
</style>
