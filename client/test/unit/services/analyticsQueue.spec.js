/**
 * Analytics Queue Tests (Unit + Integration)
 * Testing:
 * - Queue buffering and FIFO ordering
 * - Flush triggers (interval, batch size, visibility, route change)
 * - Retry logic with exponential backoff
 * - Hard caps (queue size, event size, payload limits)
 * - Fire-and-forget semantics
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { createAnalyticsQueue, getAnalyticsQueue } from '@/services/analyticsQueue'
import { useAnalyticsQueue } from '@/composables/useAnalyticsQueue'

// Run onMounted synchronously and no-op onUnmounted so the composable can be
// exercised outside of a real component instance.
vi.mock('vue', async () => {
  const actual = await vi.importActual('vue')
  return { ...actual, onMounted: (cb) => cb(), onUnmounted: vi.fn() }
})

// The composable calls useRouter(); provide a minimal stub.
vi.mock('vue-router', () => ({
  useRouter: () => ({ afterEach: vi.fn(() => vi.fn()) }),
}))

describe('AnalyticsQueue', () => {
  let queue

  beforeEach(() => {
    queue = createAnalyticsQueue({
      flushIntervalMs: 100, // Short for tests
      minEventsToFlush: 2,
      maxQueueSize: 10,
      initialBackoffMs: 10,
    })
    
    // Mock fetch
    global.fetch = vi.fn()
  })

  afterEach(() => {
    queue?.stop()
    vi.clearAllMocks()
  })

  describe('Event queueing', () => {
    it('should queue events', () => {
      const result = queue.addEvent('test:event', { count: 1 })
      expect(result).toBe(true)
      expect(queue.queue).toHaveLength(1)
    })

    it('should reject oversized events', () => {
      const largePayload = 'x'.repeat(10000)
      const result = queue.addEvent('test:event', { data: largePayload })
      expect(result).toBe(false)
    })

    it('should reject when queue is full', () => {
      // Prevent the batch-size auto-flush from draining the queue so it can
      // actually reach the hard cap we're trying to exercise here.
      vi.spyOn(queue, 'flush').mockImplementation(() => {})

      for (let i = 0; i < queue.config.maxQueueSize; i++) {
        queue.addEvent('test:event', { index: i })
      }

      const result = queue.addEvent('test:event', { overflow: true })
      expect(result).toBe(false)
      expect(queue.metrics.totalEventsRejected).toBeGreaterThan(0)
    })

    it('should track metrics', () => {
      queue.addEvent('event1', {})
      queue.addEvent('event2', {})
      
      expect(queue.metrics.totalEventsQueued).toBe(2)
      expect(queue.metrics.maxQueueSize).toBeGreaterThanOrEqual(2)
    })
  })

  describe('Flush triggers', () => {
    it('should flush when batch size threshold reached', async () => {
      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: true }),
      })
      
      queue.addEvent('test:event', { i: 1 })
      queue.addEvent('test:event', { i: 2 })
      
      await new Promise(resolve => setTimeout(resolve, 50))
      
      expect(global.fetch).toHaveBeenCalled()
      expect(queue.queue).toHaveLength(0)
    })

    it('should flush on interval', async () => {
      global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: true }),
      })
      
      queue.addEvent('test:event', { i: 1 })
      
      await new Promise(resolve => setTimeout(resolve, 150))
      
      expect(global.fetch).toHaveBeenCalled()
    })

    it('should handle visibility change', () => {
      // Visibility listener is only attached once the queue is started.
      queue.start()
      const flushSpy = vi.spyOn(queue, 'flush')

      // jsdom's document.hidden is a read-only getter; override it.
      Object.defineProperty(document, 'hidden', { configurable: true, get: () => true })
      document.dispatchEvent(new Event('visibilitychange'))

      expect(flushSpy).toHaveBeenCalled()
    })
  })

  describe('Retry logic', () => {
    it('should retry on network error', async () => {
      const calls = []
      global.fetch.mockImplementation(async () => {
        calls.push(true)
        if (calls.length < 2) {
          throw new Error('Network error')
        }
        return {
          ok: true,
          json: async () => ({ success: true }),
        }
      })
      
      queue.addEvent('test:event', {})
      queue.addEvent('test:event', {})
      
      await queue.flushAndWait()
      
      expect(calls.length).toBeGreaterThan(1)
    })

    it('should respect max retries', async () => {
      const calls = []
      global.fetch.mockImplementation(async () => {
        calls.push(true)
        throw new Error('Network error')
      })
      
      queue.addEvent('test:event', {})
      
      await queue.flushAndWait()
      
      expect(calls.length).toBeLessThanOrEqual(queue.config.maxRetries + 1)
    })

    it('should use exponential backoff', async () => {
      const times = []
      const originalSleep = queue._sleep
      queue._sleep = async (ms) => {
        times.push(ms)
      }
      
      global.fetch.mockRejectedValue(new Error('Network error'))
      
      queue.addEvent('test:event', {})
      
      try {
        await queue.flushAndWait()
      } catch {
        // Expected to fail
      }
      
      // Check backoff progression (10, 20, 40...)
      for (let i = 1; i < times.length; i++) {
        expect(times[i]).toBeGreaterThanOrEqual(times[i - 1] * 1.5)
      }
      
      queue._sleep = originalSleep
    })
  })

  describe('Fire-and-forget semantics', () => {
    it('should not block on network I/O', async () => {
      global.fetch.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          ok: true,
          json: async () => ({}),
        }), 200))
      )
      
      const start = performance.now()
      queue.addEvent('test:event', {})
      queue.addEvent('test:event', {})
      queue.flush()
      const elapsed = performance.now() - start
      
      expect(elapsed).toBeLessThan(50) // Should return immediately
    })

    it('should handle concurrent flushes', async () => {
      global.fetch.mockResolvedValue({
        ok: true,
        json: async () => ({ success: true }),
      })
      
      queue.addEvent('test:event', { i: 1 })
      queue.addEvent('test:event', { i: 2 })
      queue.flush()
      queue.flush() // Should not queue another
      
      await new Promise(resolve => setTimeout(resolve, 100))
      
      expect(queue.pendingFlushes).toBe(0)
    })
  })

  describe('Hard caps', () => {
    it('should enforce max pending flushes', async () => {
      global.fetch.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          ok: true,
          json: async () => ({}),
        }), 500))
      )
      
      for (let i = 0; i < 5; i++) {
        queue.addEvent('test:event', {})
      }
      
      for (let i = 0; i < 10; i++) {
        queue.flush()
      }
      
      expect(queue.pendingFlushes).toBeLessThanOrEqual(queue.config.maxPendingFlushes)
    })

    it('should timeout hanging flushes', async () => {
      // Short timeout and no retries so the aborted flush settles quickly.
      const localQueue = createAnalyticsQueue({
        minEventsToFlush: 10,
        flushTimeoutMs: 50,
        maxRetries: 0,
      })

      // Never resolves on its own; rejects only when the queue's
      // AbortController fires, mirroring how real fetch honors the signal.
      global.fetch.mockImplementation((_url, opts) =>
        new Promise((_resolve, reject) => {
          opts.signal.addEventListener('abort', () =>
            reject(new DOMException('The operation was aborted', 'AbortError'))
          )
        })
      )

      localQueue.addEvent('test:event', {})
      localQueue.flush()
      expect(localQueue.pendingFlushes).toBe(1)

      await new Promise(resolve => setTimeout(resolve, localQueue.config.flushTimeoutMs + 100))

      expect(localQueue.pendingFlushes).toBe(0) // Should have timed out
      localQueue.stop()
    })
  })

  describe('Error handling', () => {
    it('should handle HTTP 4xx gracefully (no retry)', async () => {
      global.fetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        text: async () => 'Invalid event',
      })
      
      queue.addEvent('test:event', {})
      await queue.flushAndWait()
      
      expect(queue.metrics.totalEventsRejected).toBeGreaterThan(0)
      expect(global.fetch).toHaveBeenCalledTimes(1) // No retry
    })

    it('should retry on HTTP 5xx', async () => {
      const calls = []
      global.fetch.mockImplementation(async () => {
        calls.push(true)
        if (calls.length < 2) {
          return {
            ok: false,
            status: 500,
            statusText: 'Server Error',
            text: async () => '',
          }
        }
        return {
          ok: true,
          json: async () => ({}),
        }
      })
      
      queue.addEvent('test:event', {})
      await queue.flushAndWait()
      
      expect(calls.length).toBeGreaterThan(1)
    })
  })
})

describe('useAnalyticsQueue (Composable)', () => {
  afterEach(() => {
    // The composable starts the singleton queue's interval timer on mount;
    // stop it so the timer doesn't leak between tests.
    getAnalyticsQueue().stop()
    vi.clearAllMocks()
  })

  it('should integrate with Vue', () => {
    global.fetch = vi.fn()
    const { track, getMetrics, isReady } = useAnalyticsQueue()

    expect(isReady.value).toBe(true)
    const metrics = getMetrics()
    expect(metrics).toHaveProperty('currentQueueSize')
    expect(typeof track).toBe('function')
  })
})
