import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { getHost, isDevelopment } from '../services/apiConfig'
import { useAuthStore } from '../stores/authStore'

/**
 * Singleton state for WebSocket connection.
 * Shared across all components using this composable so that
 * sync progress (including rate-limited state) persists across navigation.
 */
const isConnected = ref(false)
const isConnecting = ref(false)
const connectionError = ref(null)

// Map of puuid -> sync progress data (persists across navigation)
const syncProgress = reactive(new Map())

// Single user-scoped aggregate view for the "Analyze all" flow (combined across all
// linked accounts). Driven by the server's sync_aggregate_* messages; no subscription
// needed since the server pushes to the user's own authenticated connection.
const aggregateProgress = reactive({
  status: null, // null | 'syncing' | 'completed' | 'failed'
  progress: null,
  total: null,
  accountsTotal: null,
  accountsDone: null,
  matchId: null,
  totalSynced: null,
  error: null
})

function resetAggregateProgress() {
  aggregateProgress.status = null
  aggregateProgress.progress = null
  aggregateProgress.total = null
  aggregateProgress.accountsTotal = null
  aggregateProgress.accountsDone = null
  aggregateProgress.matchId = null
  aggregateProgress.totalSynced = null
  aggregateProgress.error = null
}

let socket = null
let reconnectAttempts = 0
let reconnectTimeout = null
const maxReconnectAttempts = 10
const baseReconnectDelay = 1000 // 1 second

// Track number of active component instances using this composable
// Using ref for proper reactivity and to avoid race conditions during rapid navigation
const activeInstances = ref(0)

/**
 * Composable for managing WebSocket connection to sync progress endpoint.
 * Provides real-time updates for match sync progress.
 *
 * Uses singleton state so sync progress persists across navigation.
 * The WebSocket connection is maintained as long as at least one component
 * is using this composable.
 */
export function useSyncWebSocket() {
  
  /**
   * Get WebSocket URL based on current host.
   * In development getHost() returns '' (relative), so fall back to
   * window.location so the Vite dev-server proxy is used correctly.
   */
  function getWebSocketUrl() {
    const host = getHost()
    if (host) {
      // Production: absolute origin like https://api.mongoose.gg
      const wsProtocol = host.startsWith('https') ? 'wss' : 'ws'
      const wsHost = host.replace(/^https?:\/\//, '')
      return `${wsProtocol}://${wsHost}/ws/sync`
    }
    // Development: derive from the current page's origin
    const wsProtocol = window.location.protocol === 'https:' ? 'wss' : 'ws'
    return `${wsProtocol}://${window.location.host}/ws/sync`
  }
  
  /**
   * Connect to the WebSocket server
   */
  function connect() {
    if (socket?.readyState === WebSocket.OPEN || isConnecting.value) {
      return
    }
    
    isConnecting.value = true
    connectionError.value = null
    
    try {
      const url = getWebSocketUrl()
      if (isDevelopment) {
        console.log('[SyncWebSocket] Connecting to:', url)
      }
      
      socket = new WebSocket(url)
      
      socket.onopen = () => {
        isConnected.value = true
        isConnecting.value = false
        reconnectAttempts = 0
        connectionError.value = null
        
        if (isDevelopment) {
          console.log('[SyncWebSocket] Connected')
        }
        
        // Clear transient WS state so that if we missed messages while
        // disconnected (e.g. sync completed during navigation), the UI
        // falls back to the HTTP-loaded storedStatus rather than staying
        // stuck in a stale isRateLimited / syncing state.
        for (const [puuid, progress] of syncProgress.entries()) {
          progress.status = null
          progress.isRateLimited = false
          sendSubscribe(puuid)
        }

        // Aggregate runs are in-memory on the server and can't be replayed after a
        // disconnect, so clear any stale aggregate state and let the UI fall back to
        // the HTTP-loaded per-account status.
        resetAggregateProgress()
      }
      
      socket.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data)
          handleMessage(message)
        } catch (e) {
          console.error('[SyncWebSocket] Failed to parse message:', e)
        }
      }
      
      socket.onclose = (event) => {
        isConnected.value = false
        isConnecting.value = false
        socket = null
        
        if (isDevelopment) {
          console.log('[SyncWebSocket] Disconnected:', event.code, event.reason)
        }
        
        // Attempt reconnection with exponential backoff
        scheduleReconnect()
      }
      
      socket.onerror = (error) => {
        connectionError.value = 'WebSocket connection error'
        console.error('[SyncWebSocket] Error:', error)
      }
    } catch (e) {
      isConnecting.value = false
      connectionError.value = e.message
      console.error('[SyncWebSocket] Failed to connect:', e)
    }
  }
  
  /**
   * Schedule a reconnection attempt with exponential backoff
   */
  function scheduleReconnect() {
    if (reconnectAttempts >= maxReconnectAttempts) {
      connectionError.value = 'Max reconnection attempts reached'
      return
    }
    
    const delay = Math.min(
      baseReconnectDelay * Math.pow(2, reconnectAttempts),
      30000 // Max 30 seconds
    )
    
    reconnectAttempts++
    
    if (isDevelopment) {
      console.log(`[SyncWebSocket] Reconnecting in ${delay}ms (attempt ${reconnectAttempts})`)
    }
    
    reconnectTimeout = setTimeout(() => {
      connect()
    }, delay)
  }
  
  /**
   * Disconnect from the WebSocket server
   */
  function disconnect() {
    if (reconnectTimeout) {
      clearTimeout(reconnectTimeout)
      reconnectTimeout = null
    }
    
    if (socket) {
      socket.close(1000, 'Client disconnect')
      socket = null
    }
    
    isConnected.value = false
    isConnecting.value = false
    reconnectAttempts = maxReconnectAttempts // Prevent auto-reconnect
  }
  
  /**
   * Send a subscribe message for a puuid
   */
  function sendSubscribe(puuid) {
    if (socket?.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'subscribe', puuid }))
    }
  }
  
  /**
   * Send an unsubscribe message for a puuid
   */
  function sendUnsubscribe(puuid) {
    if (socket?.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'unsubscribe', puuid }))
    }
  }
  
  /**
   * Subscribe to sync progress updates for a puuid
   */
  function subscribe(puuid) {
    if (!puuid) return

    // Initialize progress entry if not exists
    // Use null for progress/total so the UI can fall back to API data
    // until we receive actual WebSocket updates
    if (!syncProgress.has(puuid)) {
      syncProgress.set(puuid, {
        status: null,
        progress: null,
        total: null,
        matchId: null,
        error: null,
        totalSynced: null,
        // TEMPORARY: Flag for rate limiting status
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        isRateLimited: false
      })
    }

    sendSubscribe(puuid)
  }

  /**
   * Unsubscribe from sync progress updates for a puuid
   */
  function unsubscribe(puuid) {
    if (!puuid) return

    syncProgress.delete(puuid)
    sendUnsubscribe(puuid)
  }

  /**
   * Handle incoming WebSocket messages
   */
  function handleMessage(message) {
    const { type } = message

    // User-scoped aggregate messages ("Analyze all"): a single combined stream, not
    // keyed by puuid. Handle these before the per-account branch.
    switch (type) {
      case 'sync_aggregate_progress':
        aggregateProgress.status = message.status || 'syncing'
        aggregateProgress.progress = message.progress ?? 0
        aggregateProgress.total = message.total ?? 0
        aggregateProgress.accountsTotal = message.accountsTotal ?? null
        aggregateProgress.accountsDone = message.accountsDone ?? null
        aggregateProgress.matchId = message.matchId ?? null
        aggregateProgress.error = null
        return

      case 'sync_aggregate_complete':
        aggregateProgress.status = 'completed'
        aggregateProgress.totalSynced = message.totalSynced ?? aggregateProgress.progress
        aggregateProgress.progress = aggregateProgress.total // fill the bar
        aggregateProgress.accountsDone = aggregateProgress.accountsTotal
        aggregateProgress.error = null
        // Refresh user data once so lastSyncAt updates in the store.
        useAuthStore().refreshUser()
        return

      case 'sync_aggregate_error':
        aggregateProgress.status = 'failed'
        aggregateProgress.error = message.error || 'Analysis failed'
        return
    }

    const { puuid } = message

    if (!puuid) return

    // Get or create progress entry
    let progress = syncProgress.get(puuid)
    if (!progress) {
      progress = {
        status: 'idle',
        progress: 0,
        total: 0,
        matchId: null,
        error: null,
        totalSynced: null,
        // TEMPORARY: Flag for rate limiting status
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        isRateLimited: false
      }
      syncProgress.set(puuid, progress)
    }

    switch (type) {
      case 'sync_progress':
        progress.status = message.status || 'syncing'
        progress.progress = message.progress ?? progress.progress
        progress.total = message.total ?? progress.total
        progress.matchId = message.matchId ?? progress.matchId
        progress.error = null
        // TEMPORARY: Clear rate limited flag when progress resumes
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        progress.isRateLimited = false
        break

      case 'sync_complete':
        progress.status = 'completed'
        progress.totalSynced = message.totalSynced ?? progress.progress
        progress.progress = progress.total // Fill the bar
        progress.error = null
        // TEMPORARY: Clear rate limited flag on completion
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        progress.isRateLimited = false
        // Refresh user data once globally so lastSyncAt updates in the store.
        // Doing this here (singleton message handler) ensures it fires exactly
        // once per completion event regardless of how many components are mounted.
        useAuthStore().refreshUser()
        break

      case 'sync_error':
        progress.status = 'failed'
        progress.error = message.error || 'Sync failed'
        // TEMPORARY: Clear rate limited flag on error
        // TODO: Remove this once we have a more sophisticated rate limiting UX.
        progress.isRateLimited = false
        break

      // TEMPORARY: Handle rate limited status from Riot API
      // TODO: Remove this once we have a more sophisticated rate limiting UX.
      case 'sync_rate_limited':
        // Ensure status is 'syncing' so the UI renders the rate limit message
        // (rate limit can only occur during an active sync)
        progress.status = 'syncing'
        progress.isRateLimited = true
        break

      default:
        if (isDevelopment) {
          console.log('[SyncWebSocket] Unknown message type:', type)
        }
    }
  }

  /**
   * Get sync progress for a specific puuid
   */
  function getProgress(puuid) {
    return syncProgress.get(puuid) || null
  }

  /**
   * Check if a puuid is currently syncing
   */
  function isSyncing(puuid) {
    const progress = syncProgress.get(puuid)
    return progress?.status === 'syncing'
  }

  /**
   * Reset progress for a puuid (e.g., after sync completes)
   * Uses null values so the UI falls back to API data
   */
  function resetProgress(puuid) {
    if (syncProgress.has(puuid)) {
      const progress = syncProgress.get(puuid)
      progress.status = null
      progress.progress = null
      progress.total = null
      progress.matchId = null
      progress.error = null
      progress.totalSynced = null
      // TEMPORARY: Reset rate limited flag
      // TODO: Remove this once we have a more sophisticated rate limiting UX.
      progress.isRateLimited = false
    }
  }

  // Lifecycle hooks for auto-connect/disconnect
  // Uses instance counting so WebSocket stays connected as long as
  // at least one component is using this composable
  onMounted(() => {
    activeInstances.value++
    if (activeInstances.value === 1) {
      // First instance - connect
      connect()
    }
  })

  onUnmounted(() => {
    activeInstances.value--
    if (activeInstances.value === 0) {
      // Last instance unmounted - disconnect
      disconnect()
    }
  })

  return {
    // State
    isConnected,
    isConnecting,
    connectionError,
    syncProgress,
    aggregateProgress,

    // Methods
    connect,
    disconnect,
    subscribe,
    unsubscribe,
    getProgress,
    isSyncing,
    resetProgress,
    resetAggregateProgress
  }
}

