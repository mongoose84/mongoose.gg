import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { getHost, isDevelopment } from '../services/apiConfig'

/**
 * Composable for managing WebSocket connection to sync progress endpoint.
 * Provides real-time updates for match sync progress.
 */
export function useSyncWebSocket() {
  const isConnected = ref(false)
  const isConnecting = ref(false)
  const connectionError = ref(null)
  
  // Map of puuid -> sync progress data
  const syncProgress = reactive(new Map())
  
  let socket = null
  let reconnectAttempts = 0
  let reconnectTimeout = null
  const maxReconnectAttempts = 10
  const baseReconnectDelay = 1000 // 1 second
  
  /**
   * Get WebSocket URL based on current host
   */
  function getWebSocketUrl() {
    const host = getHost()
    // Convert http(s) to ws(s)
    const wsProtocol = host.startsWith('https') ? 'wss' : 'ws'
    const wsHost = host.replace(/^https?:\/\//, '')
    return `${wsProtocol}://${wsHost}/ws/sync`
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
        
        // Re-subscribe to any puuids we were tracking
        for (const puuid of syncProgress.keys()) {
          sendSubscribe(puuid)
        }
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
    if (!syncProgress.has(puuid)) {
      syncProgress.set(puuid, {
        status: 'idle',
        progress: 0,
        total: 0,
        matchId: null,
        error: null,
        totalSynced: null
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
    const { type, puuid } = message

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
        totalSynced: null
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
        break

      case 'sync_complete':
        progress.status = 'completed'
        progress.totalSynced = message.totalSynced ?? progress.progress
        progress.progress = progress.total // Fill the bar
        progress.error = null
        break

      case 'sync_error':
        progress.status = 'failed'
        progress.error = message.error || 'Sync failed'
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
   * Reset progress for a puuid (e.g., before retry)
   */
  function resetProgress(puuid) {
    if (syncProgress.has(puuid)) {
      const progress = syncProgress.get(puuid)
      progress.status = 'idle'
      progress.progress = 0
      progress.total = 0
      progress.matchId = null
      progress.error = null
      progress.totalSynced = null
    }
  }

  // Lifecycle hooks for auto-connect/disconnect
  onMounted(() => {
    connect()
  })

  onUnmounted(() => {
    disconnect()
  })

  return {
    // State
    isConnected,
    isConnecting,
    connectionError,
    syncProgress,

    // Methods
    connect,
    disconnect,
    subscribe,
    unsubscribe,
    getProgress,
    isSyncing,
    resetProgress
  }
}

