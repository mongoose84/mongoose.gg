<template>
  <div class="match-header" :class="{ 'win': match.win, 'loss': !match.win }">
    <!-- Champion Icon -->
    <div class="champion-icon-wrapper">
      <img
        v-if="match.championIconUrl"
        :src="match.championIconUrl"
        :alt="`${match.championName} icon`"
        class="champion-icon"
      />
    </div>

    <!-- Match Info -->
    <div class="match-info">
      <div class="primary-row">
        <span class="champion-name">{{ match.championName }}</span>
        <span class="result-badge" :class="{ 'win': match.win, 'loss': !match.win }">
          {{ match.win ? 'Victory' : 'Defeat' }}
        </span>
      </div>
      <div class="kda-row">
        <span class="kda-value">{{ match.kills }}</span>
        <span class="kda-separator">/</span>
        <span class="kda-value">{{ match.deaths }}</span>
        <span class="kda-separator">/</span>
        <span class="kda-value">{{ match.assists }}</span>
        <span class="kda-ratio">{{ kdaRatio }} KDA</span>
      </div>
      <div class="secondary-row">
        <span class="role">{{ formatRole(match.role) }}</span>
        <span class="separator">·</span>
        <span class="queue">{{ match.queueType }}</span>
        <span class="separator">·</span>
        <span class="duration">{{ formatDuration(match.gameDurationSec) }}</span>
        <span class="separator">·</span>
        <span class="timestamp">{{ relativeTime }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

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

function formatDuration(seconds) {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

const kdaRatio = computed(() => {
  const m = props.match
  if (m.deaths === 0) return 'Perfect'
  const ratio = (m.kills + m.assists) / m.deaths
  return ratio.toFixed(2)
})

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

  if (diffMin < 1) return 'Just now'
  if (diffMin < 60) return `${diffMin} min ago`
  if (diffHour < 24) return `${diffHour} hour${diffHour > 1 ? 's' : ''} ago`
  if (diffDay < 7) return `${diffDay} day${diffDay > 1 ? 's' : ''} ago`
  return `${diffWeek} week${diffWeek > 1 ? 's' : ''} ago`
})
</script>

<style scoped>
.match-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-elevated);
  border-radius: var(--radius-md);
}

.match-header.win {
  border-left: 4px solid #22c55e;
}

.match-header.loss {
  border-left: 4px solid #ef4444;
}

.champion-icon-wrapper {
  width: 64px;
  height: 64px;
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

.match-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.primary-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-name {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.result-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 3px 10px;
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

.kda-row {
  display: flex;
  align-items: baseline;
  gap: 2px;
}

.kda-value {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.kda-separator {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
  margin: 0 2px;
}

.kda-ratio {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-left: var(--spacing-sm);
}

.secondary-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.separator {
  opacity: 0.5;
}
</style>

