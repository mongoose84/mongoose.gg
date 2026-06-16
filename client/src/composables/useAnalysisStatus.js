import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from './useSyncWebSocket'
import { triggerAnalysisAll, getRiotAccountSyncStatus } from '../services/authApi'

/**
 * Composable for managing analysis/sync status across the application.
 * Provides a unified interface for the AnalysisStatusCard and other components.
 * Uses "analysis" language in the UI instead of "sync".
 */
export function useAnalysisStatus() {
  const authStore = useAuthStore()
  const {
    aggregateProgress,
    subscribe,
    unsubscribe,
    resetProgress,
    resetAggregateProgress,
    isConnected
  } = useSyncWebSocket()
  
  const isLoading = ref(false)
  const error = ref(null)
  const lastLoadedStatus = ref(null)
  
  // Get the primary account's puuid
  const primaryPuuid = computed(() => authStore.primaryRiotAccount?.puuid || null)
  
  // Get stored sync status from the account data
  const storedStatus = computed(() => authStore.primaryRiotAccount?.syncStatus || null)
  const lastSyncAt = computed(() => authStore.primaryRiotAccount?.lastSyncAt || null)
  
  /**
   * Whether the server-aggregated "Analyze all" run is currently active (drives the
   * real-time view). null status means no active run → fall back to stored status.
   */
  const hasAggregate = computed(() => aggregateProgress.status !== null)

  /**
   * Current analysis status - combines the real-time aggregate run with stored status.
   * Possible values: 'idle', 'pending', 'syncing', 'completed', 'failed', 'waiting_rate_limit'
   */
  const status = computed(() => {
    // The aggregate run (all linked accounts) has priority for real-time updates.
    if (hasAggregate.value) {
      return aggregateProgress.status
    }
    // Fall back to stored status from account data
    return storedStatus.value || 'idle'
  })

  /**
   * Whether analysis is currently running (pending or syncing)
   */
  const isRunning = computed(() => {
    return status.value === 'pending' || status.value === 'syncing'
  })

  /**
   * Whether waiting on Riot API rate limit.
   * The aggregate run surfaces a rate-limited account as 'syncing' (v1), so this is only
   * meaningful for the at-rest stored status.
   */
  const isRateLimited = computed(() => {
    return !hasAggregate.value && storedStatus.value === 'waiting_rate_limit'
  })

  /**
   * Whether analysis has failed
   */
  const hasFailed = computed(() => {
    return status.value === 'failed'
  })

  /**
   * Whether analysis is up to date (completed or idle with lastSyncAt)
   */
  const isUpToDate = computed(() => {
    return (status.value === 'completed' || status.value === 'idle') && lastSyncAt.value
  })

  /**
   * Combined progress across all linked accounts (current/total matches processed).
   */
  const progress = computed(() => ({
    current: aggregateProgress.progress || 0,
    total: aggregateProgress.total || 0,
    matchId: aggregateProgress.matchId || null,
    totalSynced: aggregateProgress.totalSynced || null
  }))

  /**
   * Account-level progress for the aggregate run. The combined match total keeps growing as
   * each account is enumerated from Riot, so the bar is only safe to show as determinate once
   * every account has settled (accountsDone === accountsTotal). 0 means no aggregate info.
   */
  const accountsTotal = computed(() => aggregateProgress.accountsTotal || 0)
  const accountsDone = computed(() => aggregateProgress.accountsDone || 0)

  /**
   * Error message if analysis failed
   */
  const errorMessage = computed(() => {
    return aggregateProgress.error || error.value || null
  })
  
  /**
   * Load the current analysis status from the backend.
   * This should be called on component mount to get persisted status.
   */
  async function loadStatus() {
    if (!primaryPuuid.value) return
    
    isLoading.value = true
    error.value = null
    
    try {
      const statusData = await getRiotAccountSyncStatus(primaryPuuid.value)
      lastLoadedStatus.value = statusData
      
      // Subscribe to WebSocket updates for this account
      subscribe(primaryPuuid.value)
    } catch (e) {
      error.value = e.message
      console.error('[useAnalysisStatus] Failed to load status:', e)
    } finally {
      isLoading.value = false
    }
  }
  
  /**
   * Trigger a new analysis/sync for ALL of the user's linked accounts.
   * Progress arrives as a single combined stream over the aggregate WebSocket channel.
   * Returns true if successful, false otherwise.
   */
  async function triggerAnalysis() {
    if (!primaryPuuid.value) {
      error.value = 'No linked Riot account'
      return false
    }

    isLoading.value = true
    error.value = null

    try {
      // Clear any previous aggregate run state so the new run starts clean.
      resetAggregateProgress()

      await triggerAnalysisAll()

      return true
    } catch (e) {
      error.value = e.message
      console.error('[useAnalysisStatus] Failed to trigger analysis:', e)
      return false
    } finally {
      isLoading.value = false
    }
  }
  
  /**
   * Clear the error state
   */
  function clearError() {
    error.value = null
    resetAggregateProgress()
    if (primaryPuuid.value) {
      resetProgress(primaryPuuid.value)
    }
  }
  
  // Subscribe to WebSocket when primary account changes
  // Unsubscribe from old puuid to prevent memory leaks
  watch(primaryPuuid, (newPuuid, oldPuuid) => {
    if (oldPuuid && oldPuuid !== newPuuid) {
      unsubscribe(oldPuuid)
    }
    if (newPuuid) {
      subscribe(newPuuid)
    }
  }, { immediate: true })
  
  return {
    // State
    status,
    isRunning,
    isRateLimited,
    hasFailed,
    isUpToDate,
    isLoading,
    progress,
    accountsTotal,
    accountsDone,
    errorMessage,
    lastSyncAt,
    isConnected,
    primaryPuuid,

    // Methods
    loadStatus,
    triggerAnalysis,
    clearError
  }
}

