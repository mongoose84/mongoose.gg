<template>
  <BaseCard class="analysis-status-card">
    <div class="analysis-status-content flex items-center justify-between gap-4">
      <!-- Status indicator and text -->
      <div class="flex items-center gap-3 min-w-0">
        <!-- Status dot/icon -->
        <div class="flex-shrink-0 status-icon-container">
          <Transition name="status-icon" mode="out-in">
            <!-- Loading spinner for running state (including optimistic pending) -->
            <div v-if="isActiveOrPending && !isRateLimited" key="spinner" class="status-spinner" />
            <!-- Clock icon for rate limited -->
            <ClockIcon v-else-if="isRateLimited" key="rate-limited" class="w-5 h-5 text-warning" />
            <!-- Check icon for up to date -->
            <CheckCircleIcon v-else-if="isUpToDate" key="up-to-date" class="w-5 h-5 text-success" />
            <!-- Exclamation for error -->
            <ExclamationCircleIcon v-else-if="hasFailed" key="failed" class="w-5 h-5 text-muted" />
            <!-- Default idle state -->
            <div v-else key="idle" class="status-dot status-dot--idle" />
          </Transition>
        </div>
        
        <!-- Status text -->
        <div class="min-w-0">
          <Transition name="status-text" mode="out-in">
            <p :key="statusText" class="text-sm font-medium text-white truncate">
              {{ statusText }}
            </p>
          </Transition>
          <Transition name="status-text" mode="out-in">
            <p v-if="subtitleText" :key="subtitleText" class="text-xs text-secondary truncate">
              {{ subtitleText }}
            </p>
          </Transition>
        </div>
      </div>
      
      <!-- Progress bar (only when running and has progress data) -->
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
        :loading="isLoading || isPending"
        :disabled="isActiveOrPending"
        @click="handleAction"
      >
        {{ actionButtonText }}
      </BaseButton>
    </div>
  </BaseCard>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
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

// Optimistic pending flag: true from the moment the button is clicked until
// the WebSocket confirms the job is actually running (or the request fails).
const isPending = ref(false)

// Once the WebSocket reports running or a terminal state, clear the optimistic flag.
watch(isRunning, (running) => {
  if (running) isPending.value = false
})
watch(hasFailed, (failed) => {
  if (failed) isPending.value = false
})

// Load status on mount for persisted state
onMounted(() => {
  loadStatus()
})

// True when the card should show the "active" spinner — either optimistically
// pending (clicked but no WS update yet) or genuinely running.
const isActiveOrPending = computed(() => isPending.value || isRunning.value)

// Computed properties for display
const statusText = computed(() => {
  if (isRateLimited.value) {
    return 'Waiting on Riot API...'
  }
  if (isPending.value && !isRunning.value) {
    return 'Starting analysis...'
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
  if (isPending.value && !isRunning.value) {
    return null
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
  return hasFailed.value || !isActiveOrPending.value
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
  isPending.value = true
  const success = await triggerAnalysis()
  if (!success) {
    isPending.value = false
  }
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

/* Fixed-size container so icon transitions don't shift layout */
.status-icon-container {
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
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

/* Icon crossfade transition */
.status-icon-enter-active,
.status-icon-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
  position: absolute;
}

.status-icon-enter-from {
  opacity: 0;
  transform: scale(0.7);
}

.status-icon-leave-to {
  opacity: 0;
  transform: scale(0.7);
}

/* Status text fade transition */
.status-text-enter-active,
.status-text-leave-active {
  transition: opacity 0.15s ease;
}

.status-text-enter-from,
.status-text-leave-to {
  opacity: 0;
}
</style>

