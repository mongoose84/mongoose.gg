<template>
  <router-link
    v-if="matchId"
    :to="{ path: '/app/matches', query: { matchId } }"
    class="last-match-card"
    :class="resultClass"
  >
    <!-- Champion Icon -->
    <div class="champion-icon-wrapper">
      <img
        v-if="championIconUrl"
        :src="championIconUrl"
        :alt="`${championName} icon`"
        class="champion-icon"
        @error="handleIconError"
      />
      <div v-else class="champion-icon-placeholder">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="placeholder-icon">
          <path fill-rule="evenodd" d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25zm0 8.625a1.125 1.125 0 100 2.25 1.125 1.125 0 000-2.25zM15.375 12a1.125 1.125 0 112.25 0 1.125 1.125 0 01-2.25 0zM7.5 10.875a1.125 1.125 0 100 2.25 1.125 1.125 0 000-2.25z" clip-rule="evenodd" />
        </svg>
      </div>
    </div>

    <!-- Match Info -->
    <div class="match-info">
      <div class="match-header">
        <span class="champion-name">{{ championName }}</span>
        <span class="result-badge" :class="resultClass">{{ result }}</span>
      </div>
      <div class="match-details">
        <span class="kda">{{ kda }}</span>
        <span class="separator">•</span>
        <span v-if="queueType" class="queue-type">{{ queueType }}</span>
        <span v-if="queueType" class="separator">•</span>
        <span class="timestamp">{{ relativeTime }}</span>
      </div>
    </div>

    <!-- Arrow indicator -->
    <div class="arrow-indicator">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="arrow-icon">
        <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
      </svg>
    </div>
  </router-link>

  <!-- Empty state -->
  <div v-else class="last-match-card empty">
    <div class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="empty-icon">
        <path fill-rule="evenodd" d="M4.5 5.653c0-1.426 1.529-2.33 2.779-1.643l11.54 6.348c1.295.712 1.295 2.573 0 3.285L7.28 19.991c-1.25.687-2.779-.217-2.779-1.643V5.653z" clip-rule="evenodd" />
      </svg>
      <span class="empty-text">No recent matches</span>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  matchId: { type: String, default: null },
  championIconUrl: { type: String, default: null },
  championName: { type: String, default: 'Unknown' },
  result: { type: String, default: null },
  kda: { type: String, default: null },
  timestamp: { type: Number, default: null },
  queueType: { type: String, default: null }
})

const iconError = ref(false)

function handleIconError() {
  iconError.value = true
}

// CSS class based on result
const resultClass = computed(() => {
  if (!props.result) return ''
  const r = props.result.toLowerCase()
  if (r === 'victory' || r === 'win') return 'win'
  if (r === 'defeat' || r === 'loss') return 'loss'
  return ''
})

// Calculate relative time from timestamp
const relativeTime = computed(() => {
  if (!props.timestamp) return ''
  
  const now = Date.now()
  const matchTime = props.timestamp
  const diffMs = now - matchTime
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)
  const diffWeek = Math.floor(diffDay / 7)
  const diffMonth = Math.floor(diffDay / 30)

  if (diffMin < 1) return 'Just now'
  if (diffMin < 60) return `${diffMin} min ago`
  if (diffHour < 24) return `${diffHour} hour${diffHour > 1 ? 's' : ''} ago`
  if (diffDay < 7) return `${diffDay} day${diffDay > 1 ? 's' : ''} ago`
  if (diffWeek < 4) return `${diffWeek} week${diffWeek > 1 ? 's' : ''} ago`
  return `${diffMonth} month${diffMonth > 1 ? 's' : ''} ago`
})
</script>

<style scoped>
.last-match-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  text-decoration: none;
  transition: border-color 0.2s ease, background-color 0.2s ease;
  cursor: pointer;
}

.last-match-card:hover {
  border-color: var(--color-primary);
  background: var(--color-elevated);
}

.last-match-card.win {
  border-left: 3px solid #22c55e;
}

.last-match-card.loss {
  border-left: 3px solid #ef4444;
}

.last-match-card.empty {
  cursor: default;
  justify-content: center;
}

.last-match-card.empty:hover {
  border-color: var(--color-border);
  background: var(--color-surface);
}

/* Champion Icon */
.champion-icon-wrapper {
  position: relative;
  width: 56px;
  height: 56px;
  flex-shrink: 0;
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--color-elevated);
}

.champion-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.champion-icon-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-elevated);
}

.placeholder-icon {
  width: 28px;
  height: 28px;
  color: var(--color-text-secondary);
}

/* Match Info */
.match-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.match-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-name {
  font-size: var(--text-base);
  font-weight: 600;
  color: var(--color-text);
}

.result-badge {
  font-size: var(--text-xs);
  font-weight: 600;
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.result-badge.win {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
}

.result-badge.loss {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
}

.match-details {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

.kda {
  font-weight: 500;
}

.separator {
  color: var(--color-text-tertiary);
}

.queue-type {
  color: var(--color-text-secondary);
}

.timestamp {
  color: var(--color-text-tertiary);
}

/* Arrow indicator */
.arrow-indicator {
  flex-shrink: 0;
  color: var(--color-text-tertiary);
  transition: color 0.2s ease, transform 0.2s ease;
}

.last-match-card:hover .arrow-indicator {
  color: var(--color-primary);
  transform: translateX(2px);
}

.arrow-icon {
  width: 20px;
  height: 20px;
}

/* Empty state */
.empty-state {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--color-text-secondary);
}

.empty-icon {
  width: 20px;
  height: 20px;
  opacity: 0.5;
}

.empty-text {
  font-size: var(--text-sm);
}

/* Mobile responsive */
@media (max-width: 640px) {
  .last-match-card {
    gap: var(--spacing-md);
    padding: var(--spacing-md);
  }

  .champion-icon-wrapper {
    width: 48px;
    height: 48px;
  }

  .champion-name {
    font-size: var(--text-sm);
  }

  .match-details {
    font-size: var(--text-xs);
  }
}
</style>

