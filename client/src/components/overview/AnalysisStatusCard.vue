<template>
  <BaseCard class="analysis-status-card">
    <div class="analysis-status-content flex items-center justify-between gap-4">
      <!-- Status indicator and text -->
      <div class="flex items-center gap-3 min-w-0">
        <!-- Status dot/icon -->
        <div class="flex-shrink-0">
          <!-- Loading spinner for running state -->
          <div v-if="isRunning && !isRateLimited" class="status-spinner" />
          <!-- Clock icon for rate limited -->
          <ClockIcon v-else-if="isRateLimited" class="w-5 h-5 text-warning" />
          <!-- Check icon for up to date -->
          <CheckCircleIcon v-else-if="isUpToDate" class="w-5 h-5 text-success" />
          <!-- Exclamation for error -->
          <ExclamationCircleIcon v-else-if="hasFailed" class="w-5 h-5 text-muted" />
          <!-- Default idle state -->
          <div v-else class="status-dot status-dot--idle" />
        </div>
        
        <!-- Status text -->
        <div class="min-w-0">
          <p class="text-sm font-medium text-white truncate">
            {{ statusText }}
          </p>
          <p v-if="subtitleText" class="text-xs text-secondary truncate">
            {{ subtitleText }}
          </p>
        </div>
      </div>
      
      <!-- Progress bar (only when running) -->
      <div v-if="isRunning && progress.total > 0" class="flex-1 max-w-[120px]">
        <div class="progress-bar">
          <div 
            class="progress-bar__fill"
            :class="{ 'progress-bar__fill--rate-limited': isRateLimited }"
            :style="{ width: `${progressPercent}%` }"
          />
        </div>
        <p class="text-xs text-secondary text-center mt-1">
          {{ progress.current }} / {{ progress.total }}
        </p>
      </div>
      
      <!-- Action button -->
      <BaseButton
        v-if="showActionButton"
        variant="primary"
        size="sm"
        :loading="isLoading"
        :disabled="isRunning"
        @click="handleAction"
      >
        {{ actionButtonText }}
      </BaseButton>
    </div>
  </BaseCard>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { CheckCircleIcon, ClockIcon, ExclamationCircleIcon } from '@heroicons/vue/24/solid'
import BaseCard from '../base/BaseCard.vue'
import BaseButton from '../base/BaseButton.vue'
import { useAnalysisStatus } from '../../composables/useAnalysisStatus'
import { formatRelativeTime } from '../../utils/formatters'

const {
  status,
  isRunning,
  isRateLimited,
  hasFailed,
  isUpToDate,
  isLoading,
  progress,
  errorMessage,
  lastSyncAt,
  loadStatus,
  triggerAnalysis,
  clearError
} = useAnalysisStatus()

// Load status on mount for persisted state
onMounted(() => {
  loadStatus()
})

// Computed properties for display
const statusText = computed(() => {
  if (isRateLimited.value) {
    return 'Waiting on Riot API...'
  }
  if (isRunning.value) {
    return 'Analyzing games...'
  }
  if (hasFailed.value) {
    return 'Analysis failed'
  }
  if (isUpToDate.value) {
    return 'Analysis up to date'
  }
  return 'Ready to analyze'
})

const subtitleText = computed(() => {
  if (isRateLimited.value) {
    return 'Rate limit reached, resuming shortly'
  }
  if (isRunning.value && progress.value.total > 0) {
    return `Processing match ${progress.value.current} of ${progress.value.total}`
  }
  if (hasFailed.value && errorMessage.value) {
    return errorMessage.value
  }
  if (isUpToDate.value && lastSyncAt.value) {
    // Convert date string to timestamp for formatRelativeTime
    const timestamp = new Date(lastSyncAt.value).getTime()
    return `Last updated ${formatRelativeTime(timestamp)}`
  }
  return null
})

const progressPercent = computed(() => {
  if (!progress.value.total) return 0
  return Math.round((progress.value.current / progress.value.total) * 100)
})

const showActionButton = computed(() => {
  // Show retry for failed, analyze for idle/completed
  return hasFailed.value || !isRunning.value
})

const actionButtonText = computed(() => {
  if (hasFailed.value) {
    return 'Retry'
  }
  return 'Analyze'
})

async function handleAction() {
  if (hasFailed.value) {
    clearError()
  }
  await triggerAnalysis()
}
</script>

<style scoped>
.analysis-status-card {
  padding: var(--spacing-md);
  height: 100%;
}

.analysis-status-card :deep(.card-body) {
  height: 100%;
  display: flex;
  align-items: center;
}

.analysis-status-content {
  width: 100%;
}

/* Status spinner for running state */
.status-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid rgba(59, 130, 246, 0.3);
  border-radius: 50%;
  border-top-color: #3b82f6;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Status dot for idle state */
.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.status-dot--idle {
  background-color: #6b7280;
}

/* Color utility classes */
.text-success { color: #22c55e; }
.text-warning { color: #f59e0b; }
.text-muted { color: #6b7280; }
.text-secondary { color: var(--color-text-secondary); }

/* Progress bar */
.progress-bar {
  height: 4px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 2px;
  overflow: hidden;
}

.progress-bar__fill {
  height: 100%;
  background: #3b82f6;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.progress-bar__fill--rate-limited {
  background: #f59e0b;
}
</style>

