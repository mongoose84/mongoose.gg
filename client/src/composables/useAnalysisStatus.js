import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from './useSyncWebSocket'
import { triggerRiotAccountSync, getRiotAccountSyncStatus } from '../services/authApi'

/**
 * Composable for managing analysis/sync status across the application.
 * Provides a unified interface for the AnalysisStatusCard and other components.
 * Uses "analysis" language in the UI instead of "sync".
 */
export function useAnalysisStatus() {
  const authStore = useAuthStore()
  const { syncProgress, subscribe, unsubscribe, resetProgress, isConnected } = useSyncWebSocket()
  
  const isLoading = ref(false)
  const error = ref(null)
  const lastLoadedStatus = ref(null)
  
  // Get the primary account's puuid
  const primaryPuuid = computed(() => authStore.primaryRiotAccount?.puuid || null)
  
  // Get stored sync status from the account data
  const storedStatus = computed(() => authStore.primaryRiotAccount?.syncStatus || null)
  const lastSyncAt = computed(() => authStore.primaryRiotAccount?.lastSyncAt || null)
  
  // Get WebSocket progress for the primary account
  const wsProgress = computed(() => {
    if (!primaryPuuid.value) return null
    return syncProgress.get(primaryPuuid.value) || null
  })
  
  /**
   * Current analysis status - combines WebSocket real-time updates with stored status.
   * Possible values: 'idle', 'pending', 'syncing', 'completed', 'failed', 'waiting_rate_limit'
   */
  const status = computed(() => {
    // WebSocket has priority for real-time updates
    if (wsProgress.value?.status) {
      return wsProgress.value.status
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
   * Whether waiting on Riot API rate limit
   */
  const isRateLimited = computed(() => {
    return wsProgress.value?.isRateLimited || false
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
   * Progress information (current/total matches processed)
   */
  const progress = computed(() => ({
    current: wsProgress.value?.progress || 0,
    total: wsProgress.value?.total || 0,
    matchId: wsProgress.value?.matchId || null,
    totalSynced: wsProgress.value?.totalSynced || null
  }))
  
  /**
   * Error message if analysis failed
   */
  const errorMessage = computed(() => {
    return wsProgress.value?.error || error.value || null
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
   * Trigger a new analysis/sync for the primary account.
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
      // Reset any previous error state
      resetProgress(primaryPuuid.value)
      
      await triggerRiotAccountSync(primaryPuuid.value)
      
      // Subscribe to WebSocket for real-time updates
      subscribe(primaryPuuid.value)
      
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
    if (primaryPuuid.value) {
      resetProgress(primaryPuuid.value)
    }
  }
  
  // When sync completes, refresh user data so lastSyncAt updates in the store
  watch(status, (newStatus) => {
    if (newStatus === 'completed') {
      authStore.refreshUser()
    }
  })

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

