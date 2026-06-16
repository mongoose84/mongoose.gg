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
            <ExclamationCircleIcon v-else-if="hasFailed" key="failed" class="w-5 h-5 text-error" />
            <!-- Default idle state -->
            <div v-else key="idle" class="status-dot status-dot--idle" />
          </Transition>
        </div>
        
        <!-- Status text -->
        <div class="min-w-0" aria-live="polite" aria-atomic="true">
          <Transition name="status-text" mode="out-in">
            <p :key="statusText" class="text-sm font-medium text-white truncate">
              {{ statusText }}
            </p>
          </Transition>
          <Transition name="status-text" mode="out-in">
            <p v-if="subtitleText" :key="subtitleText" class="text-xs text-text-secondary truncate">
              {{ subtitleText }}
            </p>
          </Transition>
        </div>
      </div>
      
      <!-- Progress bar: indeterminate while pending/connecting, determinate once running -->
      <div v-if="(isRunning && progress.total > 0) || (isPending && !isRunning)" class="flex-1 max-w-[120px]">
        <div class="progress-bar">
          <div 
            class="progress-bar__fill"
            :class="{
              'progress-bar__fill--rate-limited': isRateLimited,
              'progress-bar__fill--indeterminate': isPending && !isRunning
            }"
            :style="!(isPending && !isRunning) ? { width: `${progressPercent}%` } : undefined"
          />
        </div>
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
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
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

// Safety timeout handle: clears isPending if the WS never delivers a status
// update (e.g. connection failure, instant completion with no progress events).
let pendingTimeoutId = null

function clearPending() {
  if (pendingTimeoutId !== null) {
    clearTimeout(pendingTimeoutId)
    pendingTimeoutId = null
  }
  isPending.value = false
}

// Clear the optimistic flag whenever the job reaches any settled state:
// running (confirmed by WS), completed, failed.
//
// NOTE: this watcher only fires on *changes*. The safety timeout in
// handleAction covers the edge case where the sync completes so fast that
// isUpToDate never transitions away from true (no value change → no watcher fire).
watch([isRunning, isUpToDate, hasFailed], ([running, upToDate, failed]) => {
  if (running || upToDate || failed) {
    clearPending()
  }
})

// Load status on mount for persisted state
onMounted(() => {
  loadStatus()
})

// Cancel the safety-timeout if the component is destroyed before it fires.
onUnmounted(() => {
  clearPending()
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
  // Hide only while actively running (progress bar takes over).
  // Keep visible (but disabled) during the pending gap so the user
  // sees the button revert naturally if the WS never fires.
  return hasFailed.value || !isRunning.value
})

const actionButtonText = computed(() => {
  // While optimistically pending (clicked but no WS confirmation yet) the button
  // stays visible but disabled — surface "Analyzing..." so the click clearly
  // registers even before the backend/WebSocket responds.
  if (isActiveOrPending.value) {
    return 'Analyzing...'
  }
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
    return
  }

  // Immediately clear if WS has already confirmed a settled state during the
  // HTTP await (e.g. the sync started and completed before we got here, or
  // isUpToDate was already true and the WS delivered no status change).
  if (isRunning.value || hasFailed.value || isUpToDate.value) {
    clearPending()
    return
  }

  // Dead-WS fallback only. The server opens the aggregate run and broadcasts a
  // 'syncing' state almost immediately, so in the normal case the watcher above
  // clears isPending within ~1 s. This longer timeout just re-enables the button
  // if the WebSocket is down and no aggregate message ever arrives.
  pendingTimeoutId = setTimeout(clearPending, 15_000)
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
  border: 2px solid var(--color-info-soft);
  border-radius: 50%;
  border-top-color: var(--color-info);
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
  background-color: var(--color-muted);
}

/* Progress bar */
.progress-bar {
  height: 4px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 2px;
  overflow: hidden;
}

.progress-bar__fill {
  height: 100%;
  background: var(--color-info);
  border-radius: 2px;
  transition: width 0.3s ease;
}

.progress-bar__fill--rate-limited {
  background: var(--color-warning);
}

.progress-bar__fill--indeterminate {
  width: 40%;
  transition: none;
  animation: progress-slide 1.4s ease-in-out infinite;
}

@keyframes progress-slide {
  0% { transform: translateX(-150%); }
  100% { transform: translateX(350%); }
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

