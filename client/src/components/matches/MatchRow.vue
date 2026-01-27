<template>
  <div
    class="match-row"
    :class="{ 'selected': selected, 'win': match.win, 'loss': !match.win }"
    @click="$emit('select', match.matchId)"
  >
    <!-- Result indicator strip (left border) -->
    
    <!-- Champion Icon -->
    <div class="champion-icon-wrapper">
      <img
        v-if="match.championIconUrl"
        :src="match.championIconUrl"
        :alt="`${match.championName} icon`"
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
      <!-- Row 1: Champion + Context -->
      <div class="match-header">
        <span class="champion-name">{{ match.championName }}</span>
        <span class="context-separator">·</span>
        <span class="role-badge">{{ formatRole(match.role) }}</span>
        <span class="context-separator">·</span>
        <span class="queue-type">{{ match.queueType }}</span>
      </div>
      
      <!-- Row 2: KDA + Duration + Timestamp -->
      <div class="match-details">
        <span class="kda">{{ formatKda(match.kills, match.deaths, match.assists) }}</span>
        <span class="separator">•</span>
        <span class="duration">{{ formatDuration(match.gameDurationSec) }}</span>
        <span class="separator">•</span>
        <span class="timestamp">{{ relativeTime }}</span>
      </div>
    </div>

    <!-- Trend Badge -->
    <div class="trend-badge-wrapper">
      <TrendBadge :badge="match.trendBadge" />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import TrendBadge from './TrendBadge.vue'

const props = defineProps({
  match: {
    type: Object,
    required: true
  },
  selected: {
    type: Boolean,
    default: false
  }
})

defineEmits(['select'])

const iconError = ref(false)

function handleIconError() {
  iconError.value = true
}

function formatRole(role) {
  if (!role) return ''
  const roleMap = {
    'TOP': 'Top',
    'JUNGLE': 'Jungle', 
    'MIDDLE': 'Mid',
    'MID': 'Mid',
    'BOTTOM': 'Bot',
    'ADC': 'Bot',
    'UTILITY': 'Support',
    'SUPPORT': 'Support',
    'NONE': '',
    'UNKNOWN': ''
  }
  return roleMap[role.toUpperCase()] || role
}

function formatKda(kills, deaths, assists) {
  return `${kills}/${deaths}/${assists}`
}

function formatDuration(seconds) {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

const relativeTime = computed(() => {
  if (!props.match.gameStartTime) return ''
  
  const now = Date.now()
  const matchTime = props.match.gameStartTime
  const diffMs = now - matchTime
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)
  const diffWeek = Math.floor(diffDay / 7)
  const diffMonth = Math.floor(diffDay / 30)

  if (diffMin < 1) return 'Just now'
  if (diffMin < 60) return `${diffMin}m ago`
  if (diffHour < 24) return `${diffHour}h ago`
  if (diffDay < 7) return `${diffDay}d ago`
  if (diffWeek < 4) return `${diffWeek}w ago`
  return `${diffMonth}mo ago`
})
</script>

<style scoped>
.match-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md) var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.15s ease;
  min-height: 68px;
  position: relative;
}

.match-row:hover:not(.selected) {
  border-color: var(--color-border);
  background: var(--color-elevated);
}

.match-row.selected {
  background: var(--color-elevated);
  border-color: var(--color-primary);
  border-width: 2px;
  box-shadow: 0 0 0 1px var(--color-primary), 0 4px 12px rgba(147, 51, 234, 0.15);
  z-index: 1;
}

/* Win/Loss left border indicator */
.match-row.win {
  border-left: 3px solid #22c55e;
}

.match-row.loss {
  border-left: 3px solid #ef4444;
}

.match-row.selected.win {
  border-left: 4px solid #22c55e;
}

.match-row.selected.loss {
  border-left: 4px solid #ef4444;
}

/* Champion Icon */
.champion-icon-wrapper {
  position: relative;
  width: 44px;
  height: 44px;
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
}

.placeholder-icon {
  width: 24px;
  height: 24px;
  color: var(--color-text-secondary);
}

/* Match Info */
.match-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.match-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.champion-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.context-separator {
  color: var(--color-text-secondary);
  opacity: 0.5;
}

.role-badge {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.queue-type {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.match-details {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.kda {
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.separator {
  opacity: 0.5;
}

.duration,
.timestamp {
  color: var(--color-text-secondary);
}

/* Trend Badge */
.trend-badge-wrapper {
  flex-shrink: 0;
  min-width: 80px;
  display: flex;
  justify-content: flex-end;
}

/* Mobile responsive */
@media (max-width: 640px) {
  .match-row {
    gap: var(--spacing-sm);
    padding: var(--spacing-sm) var(--spacing-md);
  }

  .champion-icon-wrapper {
    width: 36px;
    height: 36px;
  }

  .trend-badge-wrapper {
    display: none;
  }
}
</style>

