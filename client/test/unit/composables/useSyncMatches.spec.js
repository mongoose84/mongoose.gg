import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, ref, computed, nextTick } from 'vue'
import { useSyncMatches } from '@/composables/useSyncMatches'

const status = ref('idle')
const lastSyncAt = ref('2026-09-27T10:00:00Z')
const progressState = ref({ current: 0, total: 0, totalSynced: null })
const accountsTotal = ref(0)
const accountsDone = ref(0)
const mockLoadStatus = vi.fn()
const mockTriggerAnalysis = vi.fn()
const mockClearError = vi.fn()

vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({
    isRunning: computed(() => status.value === 'pending' || status.value === 'syncing'),
    isRateLimited: computed(() => status.value === 'waiting_rate_limit'),
    hasFailed: computed(() => status.value === 'failed'),
    isUpToDate: computed(() => (status.value === 'completed' || status.value === 'idle') && Boolean(lastSyncAt.value)),
    progress: computed(() => progressState.value),
    accountsTotal,
    accountsDone,
    lastSyncAt,
    loadStatus: mockLoadStatus,
    triggerAnalysis: mockTriggerAnalysis,
    clearError: mockClearError
  })
}))

function mountComposable() {
  let api
  const wrapper = mount(defineComponent({
    setup() {
      api = useSyncMatches()
      return () => null
    }
  }))
  return { wrapper, api }
}

describe('useSyncMatches', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    status.value = 'idle'
    lastSyncAt.value = null
    progressState.value = { current: 0, total: 0, totalSynced: null }
    accountsTotal.value = 0
    accountsDone.value = 0
    mockLoadStatus.mockReset()
    mockTriggerAnalysis.mockReset().mockResolvedValue(true)
    mockClearError.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads the stored status on mount and shows nothing when idle', () => {
    const { api } = mountComposable()
    expect(mockLoadStatus).toHaveBeenCalledTimes(1)
    expect(api.syncState.value).toBeNull()
    expect(api.isSyncing.value).toBe(false)
  })

  it('shows running optimistically from the click until the sync settles', async () => {
    const { api } = mountComposable()

    const started = api.startSync()
    expect(api.syncState.value).toBe('running')
    await started
    expect(mockTriggerAnalysis).toHaveBeenCalledTimes(1)
    expect(api.isSyncing.value).toBe(true)

    // Dead-WebSocket fallback re-enables the button
    vi.advanceTimersByTime(15_000)
    expect(api.isSyncing.value).toBe(false)
  })

  it('does not start a second sync while one is running', async () => {
    status.value = 'syncing'
    const { api } = mountComposable()
    await api.startSync()
    expect(mockTriggerAnalysis).not.toHaveBeenCalled()
  })

  it('drops the optimistic state when the request fails', async () => {
    mockTriggerAnalysis.mockResolvedValue(false)
    const { api } = mountComposable()
    await api.startSync()
    expect(api.syncState.value).toBeNull()
  })

  it('reports progress and hides the total while a multi-account run is still counting', async () => {
    status.value = 'syncing'
    progressState.value = { current: 4, total: 20, totalSynced: null }
    accountsTotal.value = 2
    accountsDone.value = 1
    const { api } = mountComposable()

    expect(api.progressCurrent.value).toBe(4)
    expect(api.progressTotal.value).toBe(0)

    accountsDone.value = 2
    expect(api.progressTotal.value).toBe(20)
  })

  it('shows the synced count for a few seconds after a run finishes', async () => {
    status.value = 'syncing'
    const { api } = mountComposable()

    progressState.value = { current: 40, total: 40, totalSynced: 38 }
    status.value = 'completed'
    await nextTick()

    expect(api.syncState.value).toBe('done')
    expect(api.syncedCount.value).toBe(38)

    vi.advanceTimersByTime(5000)
    expect(api.syncState.value).toBeNull()
  })

  it('maps failed and rate-limited states, and clears a failure before retrying', async () => {
    status.value = 'waiting_rate_limit'
    const { api } = mountComposable()
    expect(api.syncState.value).toBe('waiting')

    status.value = 'failed'
    expect(api.syncState.value).toBe('failed')

    await api.startSync()
    await flushPromises()
    expect(mockClearError).toHaveBeenCalledTimes(1)
    expect(mockTriggerAnalysis).toHaveBeenCalledTimes(1)
  })
})
