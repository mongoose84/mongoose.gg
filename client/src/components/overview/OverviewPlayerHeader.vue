<template>
  <section class="overview-player-header">
    <!-- Profile Icon with Level Badge -->
    <div class="profile-icon-wrapper">
      <img
        v-if="profileIconUrl"
        :src="profileIconUrl"
        :alt="`${summonerName} profile icon`"
        class="profile-icon"
        @error="handleIconError"
      />
      <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="profile-icon-fallback">
        <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
      </svg>
      <span v-if="level" class="level-badge">{{ level }}</span>
    </div>

    <!-- Player Info -->
    <div class="player-info">
      <div class="name-row">
        <h2 class="summoner-name">{{ summonerName }}</h2>
        <span class="region-tag">{{ regionDisplay }}</span>
      </div>

      <!-- Context Badges -->
      <div v-if="activeContexts && activeContexts.length > 0" class="context-badges">
        <span
          v-for="context in activeContexts"
          :key="context"
          :class="['context-badge', `context-${context.toLowerCase()}`]"
        >
          {{ contextLabel(context) }}
        </span>
      </div>
    </div>

    <!-- Sync Status (always visible) -->
    <div class="sync-section">
      <!-- Syncing state with progress bar -->
      <div v-if="syncStatus === 'syncing'" class="sync-status syncing">
        <div class="sync-spinner"></div>
        <div class="sync-info">
          <span class="sync-text">Syncing matches</span>
          <div v-if="syncTotal" class="sync-progress-wrapper">
            <div class="sync-progress-bar">
              <div
                class="sync-progress-fill"
                :style="{ width: syncProgressPercent + '%' }"
              ></div>
            </div>
            <span class="sync-progress-text">{{ syncProgress }}/{{ syncTotal }}</span>
          </div>
        </div>
      </div>

      <!-- Pending state -->
      <div v-else-if="syncStatus === 'pending'" class="sync-status pending">
        <div class="sync-spinner"></div>
        <span class="sync-text">Sync pending</span>
      </div>

      <!-- Failed state -->
      <div v-else-if="syncStatus === 'failed'" class="sync-status failed">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="sync-icon">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        <span class="sync-text">Sync failed</span>
      </div>

      <!-- Completed/Idle state - show last synced time -->
      <div v-else class="sync-status idle">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="sync-icon">
          <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
        </svg>
        <span class="sync-text">{{ lastSyncDisplay }}</span>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  summonerName: {
    type: String,
    required: true
  },
  level: {
    type: Number,
    default: null
  },
  region: {
    type: String,
    required: true
  },
  profileIconUrl: {
    type: String,
    default: null
  },
  activeContexts: {
    type: Array,
    default: () => []
  },
  syncStatus: {
    type: String,
    default: null // 'syncing' | 'pending' | 'completed' | 'failed' | null
  },
  syncProgress: {
    type: Number,
    default: null
  },
  syncTotal: {
    type: Number,
    default: null
  },
  lastSyncAt: {
    type: String, // ISO timestamp
    default: null
  }
})

const iconError = ref(false)

// Calculate sync progress percentage
const syncProgressPercent = computed(() => {
  if (!props.syncProgress || !props.syncTotal) return 0
  return Math.round((props.syncProgress / props.syncTotal) * 100)
})

// Format last sync time for display
const lastSyncDisplay = computed(() => {
  // If sync status is 'completed' but lastSyncAt is missing, show "Synced"
  // (this can happen if the page hasn't refreshed user data yet)
  if (!props.lastSyncAt) {
    if (props.syncStatus === 'completed') return 'Synced'
    return 'Never synced'
  }

  const syncDate = new Date(props.lastSyncAt)
  if (isNaN(syncDate.getTime())) {
    if (props.syncStatus === 'completed') return 'Synced'
    return 'Never synced'
  }

  const now = new Date()
  const diffMs = now - syncDate
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Synced just now'
  if (diffMins < 60) return `Synced ${diffMins}m ago`
  if (diffHours < 24) return `Synced ${diffHours}h ago`
  return `Synced ${diffDays}d ago`
})

// Region labels for display (convert API codes to friendly names)
const regionLabels = {
  euw1: 'EUW',
  eun1: 'EUNE',
  na1: 'NA',
  kr: 'KR',
  jp1: 'JP',
  br1: 'BR',
  la1: 'LAN',
  la2: 'LAS',
  oc1: 'OCE',
  tr1: 'TR',
  ru: 'RU',
  ph2: 'PH',
  sg2: 'SG',
  th2: 'TH',
  tw2: 'TW',
  vn2: 'VN'
}

const regionDisplay = computed(() => {
  const region = props.region?.toLowerCase()
  return regionLabels[region] || props.region?.toUpperCase() || ''
})

function handleIconError() {
  iconError.value = true
}

function contextLabel(context) {
  const labels = {
    'Solo': 'Solo',
    'Duo': 'Duo',
    'Team': 'Team'
  }
  return labels[context] || context
}
</script>

<style scoped>
.overview-player-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
}

.profile-icon-wrapper {
  position: relative;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  overflow: visible;
  flex-shrink: 0;
  border: 2px solid var(--color-primary);
  background: var(--color-elevated);
}

.profile-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.profile-icon-fallback {
  width: 40px;
  height: 40px;
  color: var(--color-text-secondary);
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
}

.level-badge {
  position: absolute;
  bottom: -4px;
  right: -4px;
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
  padding: 2px 6px;
  border-radius: 10px;
  min-width: 28px;
  text-align: center;
  line-height: 1.2;
}

.player-info {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  min-width: 0;
}

.name-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.summoner-name {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.region-tag {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
}

.context-badges {
  display: flex;
  gap: var(--spacing-xs);
  flex-wrap: wrap;
}

.context-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 4px 10px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.context-solo {
  background: rgba(109, 40, 217, 0.2);
  color: var(--color-primary);
}

.context-duo {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
}

.context-team {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
}

/* Sync Section */
.sync-section {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.sync-status {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  padding: 6px 12px;
  border-radius: var(--radius-md);
}

.sync-status.syncing {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.sync-status.completed {
  background: rgba(34, 197, 94, 0.1);
  color: #22c55e;
}

.sync-status.pending {
  background: rgba(251, 191, 36, 0.1);
  color: #fbbf24;
}

.sync-status.failed {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.sync-status.idle {
  background: rgba(255, 255, 255, 0.03);
  color: var(--color-text-secondary);
}

.sync-icon {
  width: 16px;
  height: 16px;
}

.sync-text {
  font-weight: var(--font-weight-medium);
}

.sync-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(59, 130, 246, 0.3);
  border-radius: 50%;
  border-top-color: #3b82f6;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.sync-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.sync-progress-wrapper {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.sync-progress-bar {
  width: 80px;
  height: 4px;
  background: rgba(59, 130, 246, 0.2);
  border-radius: 2px;
  overflow: hidden;
}

.sync-progress-fill {
  height: 100%;
  background: #3b82f6;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.sync-progress-text {
  font-size: 0.625rem;
  color: #3b82f6;
  font-weight: var(--font-weight-medium);
}

/* Mobile responsive */
@media (max-width: 480px) {
  .overview-player-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
    padding: var(--spacing-md);
  }

  .profile-icon-wrapper {
    width: 56px;
    height: 56px;
  }

  .summoner-name {
    font-size: var(--font-size-lg);
  }

  .name-row {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }

  .sync-section {
    margin-left: 0;
    align-self: flex-end;
  }
}
</style>

