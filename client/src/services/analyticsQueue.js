/**
 * Client-side Analytics Queue Service
 * Manages event batching, retry logic, and flush strategy
 * 
 * Goals:
 * - Buffer events during burst traffic
 * - Batch for efficient transmission
 * - Retry with exponential backoff on failures
 * - Multiple flush triggers (interval, visibility, route, unload)
 * - Fire-and-forget user experience (no blocking on network)
 * - Hard caps to protect UX (memory, queue size, event size)
 */

const DEFAULT_CONFIG = {
  // Queue settings
  maxQueueSize: 500,           // Max events to buffer before force-flush
  maxEventSizeBytes: 8192,     // Max single event payload (2x network limit as safety margin)
  
  // Flush timing
  flushIntervalMs: 20000,      // Flush every 20 seconds
  minEventsToFlush: 5,         // Flush immediately if batch reaches this size
  
  // Retry settings
  maxRetries: 4,               // Max attempts per batch
  initialBackoffMs: 1000,      // Start at 1 second
  maxBackoffMs: 16000,         // Cap at 16 seconds (2^4 * 1000)
  backoffMultiplier: 2,        // Exponential: 1s, 2s, 4s, 8s, 16s
  
  // Hard limits (UX protection)
  maxPendingFlushes: 3,        // Don't queue more than 3 concurrent flush attempts
  flushTimeoutMs: 30000,       // Abort flush if no response after 30s
  
  // Endpoint
  endpoint: '/api/v2/analytics/async',
  
  // Monitoring
  enableMetrics: true,
}

/**
 * AnalyticsQueue - Manages event buffering and transmission
 */
class AnalyticsQueue {
  constructor(config = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config }
    
    // Queue state
    this.queue = []               // Events waiting to be flushed
    this.pendingFlushes = 0       // Currently in-flight batch sends
    this.lastFlushTime = Date.now()
    
    // Retry state per batch
    this.retryMap = new Map()    // batchId -> { retries, nextRetryTime }
    
    // Metrics
    this.metrics = {
      totalEventsQueued: 0,
      totalEventsFlushed: 0,
      totalEventsRejected: 0,
      flushCount: 0,
      flushSuccessCount: 0,
      flushFailureCount: 0,
      retryCount: 0,
      avgQueueSize: 0,
      maxQueueSize: 0,
    }
    
    // Monitoring callbacks
    this.onMetrics = null
    this.onFlushStart = null
    this.onFlushEnd = null
    this.onError = null
    
    // Flush timer
    this.flushTimer = null
    
    // Page visibility listener
    this.visibilityListener = this._handleVisibilityChange.bind(this)
    
    // Route change listener (for Vue Router)
    this.routeChangeListener = null
    
    // Unload listener
    this.unloadListener = this._handlePageUnload.bind(this)
  }
  
  /**
   * Initialize: Start interval timer and attach listeners
   */
  start() {
    // Start interval flush timer
    this._resetFlushTimer()
    
    // Listen for page visibility changes (tab hidden/shown)
    document.addEventListener('visibilitychange', this.visibilityListener)
    
    // Listen for page unload (send beacon)
    window.addEventListener('beforeunload', this.unloadListener, { capture: true })
    
    console.log('[AnalyticsQueue] Started with config:', this.config)
  }
  
  /**
   * Shutdown: Stop timers and detach listeners
   */
  stop() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer)
      this.flushTimer = null
    }
    
    document.removeEventListener('visibilitychange', this.visibilityListener)
    window.removeEventListener('beforeunload', this.unloadListener, { capture: true })
    
    if (this.routeChangeListener) {
      this.routeChangeListener()
    }
    
    console.log('[AnalyticsQueue] Stopped')
  }
  
  /**
   * Add event to queue
   * @param {string} eventName - Event identifier
   * @param {object} payload - Event payload
   * @returns {boolean} - Whether event was queued (false if dropped due to limits)
   */
  addEvent(eventName, payload = {}) {
    try {
      // Check event size
      const eventJson = JSON.stringify({ eventName, payload })
      const eventSize = new Blob([eventJson]).size
      
      if (eventSize > this.config.maxEventSizeBytes) {
        console.warn(`[AnalyticsQueue] Event too large (${eventSize}B > ${this.config.maxEventSizeBytes}B):`, eventName)
        this.metrics.totalEventsRejected++
        return false
      }
      
      // Check queue size
      if (this.queue.length >= this.config.maxQueueSize) {
        console.warn(`[AnalyticsQueue] Queue full (${this.queue.length}). Force flushing.`)
        this.flush() // Non-blocking flush
        
        // If still full after flush, drop event
        if (this.queue.length >= this.config.maxQueueSize) {
          this.metrics.totalEventsRejected++
          return false
        }
      }
      
      // Add to queue
      this.queue.push({
        eventName,
        payload,
        enqueuedAt: Date.now(),
      })
      
      this.metrics.totalEventsQueued++
      this._updateMetrics()
      
      // Flush if batch size threshold reached
      if (this.queue.length >= this.config.minEventsToFlush) {
        this.flush() // Non-blocking
      }
      
      return true
    } catch (err) {
      console.error('[AnalyticsQueue] Error adding event:', err)
      this._reportError('addEvent', err)
      return false
    }
  }
  
  /**
   * Register Vue Router for route-change flush trigger
   * @param {Router} router - Vue Router instance
   */
  registerRouter(router) {
    if (!router || !router.afterEach) {
      console.warn('[AnalyticsQueue] Router not available for registration')
      return
    }
    
    this.routeChangeListener = router.afterEach(async (to, from) => {
      // Flush before navigation (but don't block navigation)
      this.flush()
    })
    
    console.log('[AnalyticsQueue] Router registered for route-change flush')
  }
  
  /**
   * Flush queue: Send buffered events to backend
   * Non-blocking; returns immediately (fire-and-forget semantics)
   */
  async flush() {
    // Guard against concurrent flushes
    if (this.pendingFlushes >= this.config.maxPendingFlushes) {
      console.debug(`[AnalyticsQueue] Flush skipped (${this.pendingFlushes} pending)`)
      return
    }
    
    if (this.queue.length === 0) {
      return
    }
    
    // Take snapshot of queue
    const batch = this.queue.splice(0, this.queue.length)
    const batchId = this._generateBatchId()
    
    this.pendingFlushes++
    this.metrics.flushCount++
    this._updateMetrics()
    
    if (this.onFlushStart) {
      this.onFlushStart({ batchId, eventCount: batch.length })
    }
    
    // Send batch in background (fire-and-forget)
    this._sendBatchAsync(batch, batchId, 0)
      .then((result) => {
        this.pendingFlushes--
        this.metrics.flushSuccessCount++
        this.lastFlushTime = Date.now()
        
        if (this.onFlushEnd) {
          this.onFlushEnd({ batchId, status: 'success', result })
        }
      })
      .catch((err) => {
        this.pendingFlushes--
        this.metrics.flushFailureCount++
        
        if (this.onFlushEnd) {
          this.onFlushEnd({ batchId, status: 'failed', error: err.message })
        }
      })
  }
  
  /**
   * Send batch with retry logic (internal)
   */
  async _sendBatchAsync(batch, batchId, attemptNum) {
    const retryState = this.retryMap.get(batchId) || { retries: 0, nextRetryTime: 0 }
    
    // Check retry limit
    if (attemptNum > this.config.maxRetries) {
      console.error(`[AnalyticsQueue] Batch ${batchId} failed after ${attemptNum} attempts`)
      this.retryMap.delete(batchId)
      throw new Error(`Max retries exceeded for batch ${batchId}`)
    }
    
    // Exponential backoff
    if (attemptNum > 0) {
      const backoffMs = Math.min(
        this.config.initialBackoffMs * Math.pow(this.config.backoffMultiplier, attemptNum - 1),
        this.config.maxBackoffMs
      )
      await this._sleep(backoffMs)
      this.metrics.retryCount++
      console.debug(`[AnalyticsQueue] Batch ${batchId} retry #${attemptNum} (backoff ${backoffMs}ms)`)
    }
    
    try {
      // Build request
      const request = {
        events: batch.map(evt => ({
          eventName: evt.eventName,
          eventVersion: 1,
          clientTimestamp: evt.enqueuedAt,
          timestamp: Date.now(),
          sessionId: this._getSessionId(),
          payload: evt.payload,
          metadata: {
            queuedTime: evt.enqueuedAt,
            flushTime: Date.now(),
          },
        })),
      }
      
      // Send with timeout
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), this.config.flushTimeoutMs)
      
      const response = await fetch(this.config.endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
        signal: controller.signal,
      })
      
      clearTimeout(timeoutId)
      
      // Handle response
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408 || response.status === 429) {
          // Retryable errors
          throw new Error(`HTTP ${response.status}: ${response.statusText}`)
        } else {
          // Non-retryable errors (4xx)
          const text = await response.text()
          console.warn(`[AnalyticsQueue] Batch ${batchId} rejected:`, text)
          this.metrics.totalEventsRejected += batch.length
          return { success: false, statusCode: response.status, details: text }
        }
      }
      
      const data = await response.json()
      this.metrics.totalEventsFlushed += batch.length
      this.retryMap.delete(batchId)
      
      console.debug(`[AnalyticsQueue] Batch ${batchId} sent successfully:`, data)
      return { success: true, data }
      
    } catch (err) {
      // Retryable on network errors
      if (attemptNum < this.config.maxRetries) {
        console.debug(`[AnalyticsQueue] Batch ${batchId} attempt ${attemptNum} failed, will retry:`, err.message)
        return this._sendBatchAsync(batch, batchId, attemptNum + 1)
      } else {
        console.error(`[AnalyticsQueue] Batch ${batchId} failed permanently:`, err)
        this.metrics.totalEventsRejected += batch.length
        this.retryMap.delete(batchId)
        throw err
      }
    }
  }
  
  /**
   * Handle page visibility change (tab hidden/shown)
   */
  _handleVisibilityChange() {
    if (document.hidden) {
      console.debug('[AnalyticsQueue] Page hidden, flushing queue')
      this.flush()
    }
  }
  
  /**
   * Handle page unload (beforeunload event)
   * Uses sendBeacon for reliable delivery before navigation
   */
  _handlePageUnload(event) {
    if (this.queue.length === 0) {
      return
    }
    
    try {
      const batch = this.queue
      const beacon = {
        events: batch.map(evt => ({
          eventName: evt.eventName,
          sessionId: this._getSessionId(),
          payload: evt.payload,
        })),
      }
      
      // sendBeacon can only send POST to same origin
      const success = navigator.sendBeacon(
        this.config.endpoint,
        JSON.stringify(beacon)
      )
      
      if (success) {
        console.debug('[AnalyticsQueue] Sent beacon on unload:', batch.length, 'events')
      } else {
        console.warn('[AnalyticsQueue] Beacon send failed')
      }
    } catch (err) {
      console.error('[AnalyticsQueue] Error in unload handler:', err)
    }
  }
  
  /**
   * Reset flush interval timer
   */
  _resetFlushTimer() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer)
    }
    
    this.flushTimer = setInterval(() => {
      if (this.queue.length > 0) {
        this.flush()
      }
    }, this.config.flushIntervalMs)
  }
  
  /**
   * Update metrics
   */
  _updateMetrics() {
    this.metrics.avgQueueSize = (this.metrics.avgQueueSize + this.queue.length) / 2
    this.metrics.maxQueueSize = Math.max(this.metrics.maxQueueSize, this.queue.length)
    
    if (this.config.enableMetrics && this.onMetrics) {
      this.onMetrics({
        ...this.metrics,
        currentQueueSize: this.queue.length,
        pendingFlushes: this.pendingFlushes,
        timestamp: Date.now(),
      })
    }
  }
  
  /**
   * Get session ID (from session storage or generate)
   */
  _getSessionId() {
    if (!window.__analyticsSessionId) {
      window.__analyticsSessionId = this._generateUuid()
      sessionStorage.setItem('analyticsSessionId', window.__analyticsSessionId)
    }
    return window.__analyticsSessionId
  }
  
  /**
   * Generate batch ID
   */
  _generateBatchId() {
    return `batch_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }
  
  /**
   * Generate UUID v4
   */
  _generateUuid() {
    const bytes = new Uint8Array(16)
    window.crypto.getRandomValues(bytes)
    bytes[6] = (bytes[6] & 0x0f) | 0x40
    bytes[8] = (bytes[8] & 0x3f) | 0x80
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0'))
    return `${hex[0]}${hex[1]}${hex[2]}${hex[3]}-${hex[4]}${hex[5]}-${hex[6]}${hex[7]}-${hex[8]}${hex[9]}-${hex[10]}${hex[11]}${hex[12]}${hex[13]}${hex[14]}${hex[15]}`
  }
  
  /**
   * Sleep helper
   */
  async _sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms))
  }
  
  /**
   * Report error
   */
  _reportError(context, error) {
    if (this.onError) {
      this.onError({ context, error })
    }
  }
  
  /**
   * Get metrics snapshot
   */
  getMetrics() {
    return {
      ...this.metrics,
      currentQueueSize: this.queue.length,
      pendingFlushes: this.pendingFlushes,
      timestamp: Date.now(),
    }
  }
  
  /**
   * Force immediate flush and wait for completion
   * Used for testing or critical scenarios
   */
  async flushAndWait() {
    return new Promise((resolve) => {
      const originalOnFlushEnd = this.onFlushEnd
      
      this.onFlushEnd = (result) => {
        if (originalOnFlushEnd) {
          originalOnFlushEnd(result)
        }
        resolve(result)
      }
      
      this.flush()
    })
  }
}

// Singleton instance
let instance = null

/**
 * Get or create singleton queue instance
 */
export function getAnalyticsQueue(config = {}) {
  if (!instance) {
    instance = new AnalyticsQueue(config)
  }
  return instance
}

/**
 * Create new queue instance (for testing)
 */
export function createAnalyticsQueue(config = {}) {
  return new AnalyticsQueue(config)
}

export default AnalyticsQueue
