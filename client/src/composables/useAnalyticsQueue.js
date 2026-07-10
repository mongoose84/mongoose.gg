/**
 * useAnalyticsQueue - Vue 3 Composable
 * Provides simple event tracking with automatic queueing and flushing
 * 
 * Usage:
 *   const { track, getMetrics } = useAnalyticsQueue()
 *   track('nav:page_view', { path: '/app' })
 *   const metrics = getMetrics()
 */

import { onMounted, onUnmounted, ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { getAnalyticsQueue } from '@/services/analyticsQueue'

export function useAnalyticsQueue(options = {}) {
  const router = useRouter()
  const queue = ref(null)
  const metrics = reactive({
    queueSize: 0,
    flushCount: 0,
    successCount: 0,
    failureCount: 0,
    totalQueued: 0,
    totalRejected: 0,
  })
  
  const isReady = ref(false)
  const error = ref(null)
  
  /**
   * Initialize analytics queue on mount
   */
  onMounted(() => {
    try {
      // Get or create queue
      queue.value = getAnalyticsQueue(options)
      
      // Set up listeners
      queue.value.onFlushStart = (event) => {
        console.debug('[useAnalyticsQueue] Flush started:', event)
      }
      
      queue.value.onFlushEnd = (event) => {
        if (event.status === 'success') {
          metrics.successCount++
        } else {
          metrics.failureCount++
        }
        console.debug('[useAnalyticsQueue] Flush completed:', event)
      }
      
      queue.value.onMetrics = (snapshot) => {
        metrics.queueSize = snapshot.currentQueueSize
        metrics.flushCount = snapshot.flushCount
        metrics.totalQueued = snapshot.totalEventsQueued
        metrics.totalRejected = snapshot.totalEventsRejected
      }
      
      queue.value.onError = (event) => {
        console.error('[useAnalyticsQueue] Error:', event)
        error.value = event
      }
      
      // Start queue
      queue.value.start()
      
      // Register router for route-change flush
      if (router) {
        queue.value.registerRouter(router)
      }
      
      isReady.value = true
      console.log('[useAnalyticsQueue] Initialized')
      
    } catch (err) {
      console.error('[useAnalyticsQueue] Initialization error:', err)
      error.value = err
    }
  })
  
  /**
   * Clean up on unmount
   */
  onUnmounted(async () => {
    if (queue.value) {
      // Final flush before unload
      await queue.value.flushAndWait()
      queue.value.stop()
      console.log('[useAnalyticsQueue] Cleaned up')
    }
  })
  
  /**
   * Track an event (add to queue)
   * @param {string} eventName - Event identifier (e.g., 'nav:page_view')
   * @param {object} payload - Event payload
   * @returns {boolean} - Whether event was queued
   */
  const track = (eventName, payload = {}) => {
    if (!isReady.value || !queue.value) {
      console.warn('[useAnalyticsQueue] Queue not ready')
      return false
    }
    
    return queue.value.addEvent(eventName, payload)
  }
  
  /**
   * Manually flush queue
   * Fire-and-forget; doesn't block caller
   */
  const flush = () => {
    if (!queue.value) return
    queue.value.flush()
  }
  
  /**
   * Get current metrics snapshot
   */
  const getMetrics = () => {
    if (!queue.value) {
      return metrics
    }
    return queue.value.getMetrics()
  }
  
  /**
   * Get queue state
   */
  const getQueueState = () => ({
    isReady: isReady.value,
    error: error.value,
    metrics: reactive(getMetrics()),
  })
  
  return {
    // Main API
    track,
    flush,
    
    // Queries
    getMetrics,
    getQueueState,
    
    // Refs
    isReady,
    error,
    metrics,
  }
}

export default useAnalyticsQueue
