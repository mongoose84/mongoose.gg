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
        v-if="match.championIconUrl && !iconError"
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
        <template v-if="match.role && match.role !== 'UNKNOWN'">
          <span class="context-separator">·</span>
          <span class="role-badge">{{ formatRole(match.role) }}</span>
        </template>
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
      <TrendBadge :badge="displayBadge" />
    </div>

    <!-- Account Icon (Overall mode only — outermost right) -->
    <div
      v-if="match.accountGameName"
      class="account-icon-wrapper"
      :title="accountBadgeTitle"
      :aria-label="accountBadgeLabel"
      role="img"
      data-testid="account-tag"
    >
      <img
        v-if="accountIconUrl && !accountIconError"
        :src="accountIconUrl"
        alt=""
        class="account-icon"
        @error="accountIconError = true"
      />
      <div v-else class="account-icon-fallback">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
        </svg>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import TrendBadge from './TrendBadge.vue'
import { formatRole, formatKda, formatDuration, formatRelativeTime } from '@/utils/formatters'
import { getProfileIconUrl } from '@/utils/leagueAssets'
import { useAuthStore } from '@/stores/authStore'

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

const authStore = useAuthStore()

const iconError = ref(false)
const accountIconError = ref(false)

function handleIconError() {
  iconError.value = true
}

const relativeTime = computed(() => formatRelativeTime(props.match.gameStartTime, { short: true }))

const displayBadge = computed(() => {
  if (props.match.gameDurationSec < 300) {
    return { text: 'Remake', type: 'neutral', stat: null }
  }
  return props.match.trendBadge
})

const accountIconUrl = computed(() => {
  if (!props.match.accountGameName) return null
  const account = authStore.riotAccounts.find(a =>
    a.gameName?.toLowerCase() === props.match.accountGameName?.toLowerCase() &&
    a.tagLine?.toLowerCase() === props.match.accountTagLine?.toLowerCase() &&
    a.region?.toLowerCase() === props.match.accountRegion?.toLowerCase()
  )
  if (!account?.profileIconId) return null
  return getProfileIconUrl(account.profileIconId)
})

const accountBadgeTitle = computed(() => {
  const name = props.match.accountGameName
  const region = props.match.accountRegion ? ' · ' + props.match.accountRegion.toUpperCase() : ''
  return `${name}${region}`
})

const accountBadgeLabel = computed(() => {
  const name = props.match.accountGameName
  const region = props.match.accountRegion ? ' · ' + props.match.accountRegion.toUpperCase() : ''
  return `Account: ${name}${region}`
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
  border-left: 3px solid var(--color-success);
}

.match-row.loss {
  border-left: 3px solid var(--color-error);
}

.match-row.selected.win {
  border-left: 4px solid var(--color-success);
}

.match-row.selected.loss {
  border-left: 4px solid var(--color-error);
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

/* Account Icon */
.account-icon-wrapper {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  overflow: hidden;
  background: var(--color-elevated);
  border: 1px solid var(--color-border);
}

.account-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.account-icon-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.account-icon-fallback svg {
  width: 14px;
  height: 14px;
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

  .account-icon-wrapper {
    display: none;
  }

  .trend-badge-wrapper {
    display: none;
  }
}
</style>

