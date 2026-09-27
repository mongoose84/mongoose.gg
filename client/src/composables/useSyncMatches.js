import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useAnalysisStatus } from './useAnalysisStatus'

// How long "Synced N matches · just now" stays before SyncProgress disappears
const DONE_DISPLAY_MS = 5000

// Dead-WebSocket fallback: re-enable the sync button if no status update ever arrives
const PENDING_TIMEOUT_MS = 15_000

/**
 * Drives SyncProgress and the "Sync matches" button: starts a sync of all linked accounts,
 * shows it as running optimistically until the WebSocket confirms, and keeps the finished
 * state visible for a few seconds.
 *
 * syncState: null (nothing to show), 'running', 'waiting', 'done' or 'failed'.
 */
export function useSyncMatches() {
  const {
    isRunning,
    isRateLimited,
    hasFailed,
    isUpToDate,
    progress,
    accountsTotal,
    accountsDone,
    lastSyncAt,
    loadStatus,
    triggerAnalysis,
    clearError
  } = useAnalysisStatus()

  // Optimistic flag from the click until the WebSocket confirms the run (or the request fails)
  const isPending = ref(false)
  const syncedCount = ref(null)

  let pendingTimeoutId = null
  let doneTimeoutId = null

  function clearPending() {
    if (pendingTimeoutId !== null) {
      clearTimeout(pendingTimeoutId)
      pendingTimeoutId = null
    }
    isPending.value = false
  }

  function clearDone() {
    if (doneTimeoutId !== null) {
      clearTimeout(doneTimeoutId)
      doneTimeoutId = null
    }
    syncedCount.value = null
  }

  watch([isRunning, isUpToDate, hasFailed], ([running, upToDate, failed]) => {
    if (running || upToDate || failed) clearPending()
  })

  // A run that ends without failing shows "Synced N matches" for a few seconds
  watch(isRunning, (running, wasRunning) => {
    if (running) {
      clearDone()
      return
    }
    if (wasRunning && !hasFailed.value) {
      syncedCount.value = progress.value.totalSynced ?? progress.value.total ?? 0
      doneTimeoutId = setTimeout(clearDone, DONE_DISPLAY_MS)
    }
  })

  const isSyncing = computed(() => isPending.value || isRunning.value)

  const syncState = computed(() => {
    if (hasFailed.value) return 'failed'
    if (isRateLimited.value) return 'waiting'
    if (isSyncing.value) return 'running'
    if (syncedCount.value !== null) return 'done'
    return null
  })

  // A multi-account run keeps growing its total until every account is enumerated,
  // so report it as unknown (indeterminate) until then
  const progressTotal = computed(() => {
    const total = progress.value.total || 0
    if (accountsTotal.value > 1 && accountsDone.value < accountsTotal.value) return 0
    return total
  })

  async function startSync() {
    if (isSyncing.value) return
    if (hasFailed.value) clearError()
    clearDone()

    isPending.value = true
    const started = await triggerAnalysis()
    if (!started) {
      isPending.value = false
      return
    }

    // The WebSocket may already have settled the run while the request was in flight
    if (isRunning.value || hasFailed.value || isUpToDate.value) {
      clearPending()
      return
    }

    pendingTimeoutId = setTimeout(clearPending, PENDING_TIMEOUT_MS)
  }

  onMounted(() => {
    loadStatus()
  })

  onUnmounted(() => {
    clearPending()
    clearDone()
  })

  return {
    syncState,
    isSyncing,
    progressCurrent: computed(() => progress.value.current || 0),
    progressTotal,
    syncedCount: computed(() => syncedCount.value ?? 0),
    lastSyncAt,
    startSync
  }
}
