<template>
  <section class="rank-snapshot">
    <!-- Rank Emblem -->
    <div class="rank-emblem-wrapper">
      <img
        v-if="rankEmblemUrl"
        :src="rankEmblemUrl"
        :alt="`${tierDisplay} emblem`"
        class="rank-emblem"
        @error="handleEmblemError"
      />
      <div v-else class="rank-emblem-placeholder">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="unranked-icon">
          <path fill-rule="evenodd" d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25zM12.75 9a.75.75 0 00-1.5 0v2.25H9a.75.75 0 000 1.5h2.25V15a.75.75 0 001.5 0v-2.25H15a.75.75 0 000-1.5h-2.25V9z" clip-rule="evenodd" />
        </svg>
      </div>
    </div>

    <!-- Rank Info -->
    <div class="rank-info">
      <div class="queue-label">{{ primaryQueueLabel }}</div>
      <div class="rank-details">
        <span class="rank-text">{{ rankDisplay }}</span>
        <span v-if="lp !== null && lp !== undefined" class="lp-text">{{ lp }} LP</span>
      </div>
      <div class="stats-row">
        <span class="lp-delta" :class="lpDeltaClass">{{ formattedLpDelta }}</span>
        <span class="separator">•</span>
        <span class="winrate-text">{{ winrateDisplay }}</span>
      </div>
      <!-- W/L Strip -->
      <div v-if="wlLast20 && wlLast20.length > 0" class="wl-strip">
        <div class="strip-indicators">
          <div
            v-for="(isWin, index) in wlLast20"
            :key="`wl-${index}`"
            class="wl-indicator"
            :class="isWin ? 'win' : 'loss'"
          ></div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  primaryQueueLabel: { type: String, required: true },
  rank: { type: String, default: null },
  lp: { type: Number, default: null },
  lpDeltaLast20: { type: Number, default: 0 },
  last20Wins: { type: Number, default: 0 },
  last20Losses: { type: Number, default: 0 },
  wlLast20: { type: Array, default: () => [] }
})

const emblemError = ref(false)

// Extract tier from rank string (e.g., "SILVER IV" -> "silver")
const tierDisplay = computed(() => {
  if (!props.rank) return null
  const tier = props.rank.split(' ')[0]
  return tier ? tier.toLowerCase() : null
})

// Get rank emblem URL from local cropped assets
const rankEmblemUrl = computed(() => {
  if (!tierDisplay.value || emblemError.value) return null
  // Use locally cropped emblems for consistent sizing (72x72px to match profile icon)
  return `/assets/ranked/emblem-${tierDisplay.value}.png`
})

// Display rank nicely formatted
const rankDisplay = computed(() => {
  if (!props.rank) return 'Unranked'
  // Capitalize first letter of tier, e.g., "SILVER IV" -> "Silver IV"
  const parts = props.rank.split(' ')
  if (parts.length === 0) return props.rank
  const tier = parts[0].charAt(0).toUpperCase() + parts[0].slice(1).toLowerCase()
  return parts.length > 1 ? `${tier} ${parts[1]}` : tier
})

// LP delta styling
const lpDeltaClass = computed(() => {
  if (props.lpDeltaLast20 > 0) return 'positive'
  if (props.lpDeltaLast20 < 0) return 'negative'
  return 'neutral'
})

// Format LP delta with sign
const formattedLpDelta = computed(() => {
  const delta = props.lpDeltaLast20
  if (delta > 0) return `+${delta} LP (Last 20)`
  if (delta < 0) return `${delta} LP (Last 20)`
  return '±0 LP (Last 20)'
})

// Calculate winrate
const winrateDisplay = computed(() => {
  const wins = props.last20Wins
  const losses = props.last20Losses
  const total = wins + losses
  if (total === 0) return 'No games'
  const winrate = Math.round((wins / total) * 100)
  return `${winrate}% (${wins}W–${losses}L)`
})

function handleEmblemError() {
  emblemError.value = true
}
</script>

<style scoped>
.rank-snapshot {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
}

/* Rank Emblem - matches profile icon size from OverviewPlayerHeader */
.rank-emblem-wrapper {
  position: relative;
  width: 72px;
  height: 72px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.rank-emblem {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.rank-emblem-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(148, 163, 184, 0.1);
  border-radius: 50%;
}

.unranked-icon {
  width: 40px;
  height: 40px;
  color: var(--color-text-secondary);
}

/* Rank Info */
.rank-info {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  min-width: 0;
}

.queue-label {
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-secondary);
}

.rank-details {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-sm);
}

.rank-text {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.lp-text {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}

.stats-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  flex-wrap: wrap;
}

.lp-delta {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.lp-delta.positive { color: #22c55e; }
.lp-delta.negative { color: #ef4444; }
.lp-delta.neutral { color: var(--color-text-secondary); }

.separator {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.winrate-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* W/L Strip */
.wl-strip {
  margin-top: var(--spacing-xs);
}

.strip-indicators {
  display: flex;
  gap: 3px;
}

.wl-indicator {
  width: 10px;
  height: 10px;
  border-radius: 2px;
}

.wl-indicator.win { background: #22c55e; }
.wl-indicator.loss { background: #ef4444; }

/* Mobile Responsive */
@media (max-width: 480px) {
  .rank-snapshot {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
    padding: var(--spacing-md);
  }

  .rank-emblem-wrapper {
    width: 56px;
    height: 56px;
  }

  .rank-text {
    font-size: var(--font-size-lg);
  }

  .rank-details {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }
}
</style>

