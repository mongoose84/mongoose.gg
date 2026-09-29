import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { effectScope, reactive, nextTick } from 'vue'
import { flushPromises } from '@vue/test-utils'
import { climbResponse, statTrendsResponse, winFactorsResponse } from '@test/helpers/soloFixtures'

const mockGetSoloClimb = vi.fn()
const mockGetSoloStatTrends = vi.fn()
const mockGetSoloWinFactors = vi.fn()
const mockGetDeathPositions = vi.fn()
const mockTrackFilterChange = vi.fn()

const authStore = reactive({
  userId: 1,
  isInitialized: true,
  hasLinkedAccount: true,
  activeAccountPuuid: 'acc-1'
})

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => authStore
}))

vi.mock('@/services/analyticsApi', () => ({
  trackFilterChange: (...args) => mockTrackFilterChange(...args)
}))

vi.mock('@/services/soloApi', () => ({
  getSoloClimb: (...args) => mockGetSoloClimb(...args),
  getSoloStatTrends: (...args) => mockGetSoloStatTrends(...args),
  getSoloWinFactors: (...args) => mockGetSoloWinFactors(...args),
  getDeathPositions: (...args) => mockGetDeathPositions(...args)
}))

const { useSoloDashboardData: createSoloDashboardData } = await import('@/composables/useSoloDashboardData')

// Each instance lives in its own scope, stopped after the test, so no watcher outlives it
let scope = null
function useSoloDashboardData() {
  scope = effectScope()
  return scope.run(createSoloDashboardData)
}

describe('useSoloDashboardData', () => {
  afterEach(() => {
    scope?.stop()
    scope = null
  })

  beforeEach(() => {
    vi.clearAllMocks()
    Object.assign(authStore, { userId: 1, isInitialized: true, hasLinkedAccount: true, activeAccountPuuid: 'acc-1' })
    mockGetSoloClimb.mockResolvedValue(climbResponse())
    mockGetSoloStatTrends.mockResolvedValue(statTrendsResponse())
    mockGetSoloWinFactors.mockResolvedValue(winFactorsResponse())
    mockGetDeathPositions.mockResolvedValue({ deaths: [], totalDeaths: 0, matchesAnalyzed: 0 })
  })

  it('loads both cards in parallel and lets the server pick the queue', async () => {
    const data = useSoloDashboardData()
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenCalledWith(1, null, 'last20')
    expect(mockGetSoloWinFactors).toHaveBeenCalledWith(1, null, 'last20')
    expect(mockGetSoloClimb).toHaveBeenCalledWith(1, null, 'last20')
    expect(data.climb.value.mode).toBe('lp')
    expect(data.statTrends.value.matches).toBe(20)
    expect(data.winFactors.value.factors).toHaveLength(3)
    expect(data.selectedQueue.value).toBe('ranked_solo')
  })

  it('loads the death map in the queue the server picked', async () => {
    useSoloDashboardData()
    await flushPromises()

    expect(mockGetDeathPositions).toHaveBeenCalledWith(1, 'ranked_solo', 'current_season', 'all')
  })

  it('reloads with the chosen queue and range, and tracks the change', async () => {
    const data = useSoloDashboardData()
    await flushPromises()
    vi.clearAllMocks()

    data.setQueue('ranked_flex')
    await flushPromises()
    data.setRange('season')
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenLastCalledWith(1, 'ranked_flex', 'season')
    expect(mockGetSoloWinFactors).toHaveBeenLastCalledWith(1, 'ranked_flex', 'season')
    expect(mockGetSoloClimb).toHaveBeenLastCalledWith(1, 'ranked_flex', 'season')
    expect(mockTrackFilterChange).toHaveBeenCalledWith('queue', 'ranked_flex')
    expect(mockTrackFilterChange).toHaveBeenCalledWith('range', 'season')
  })

  it('does nothing when the chosen queue is already shown', async () => {
    const data = useSoloDashboardData()
    await flushPromises()
    vi.clearAllMocks()

    data.setQueue('ranked_solo')
    await flushPromises()

    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()
    expect(mockTrackFilterChange).not.toHaveBeenCalled()
  })

  it('keeps the old content while new data loads', async () => {
    const data = useSoloDashboardData()
    await flushPromises()

    mockGetSoloStatTrends.mockReturnValue(new Promise(() => {}))
    data.setRange('last50')
    await nextTick()

    expect(data.statTrendsLoading.value).toBe(false)
    expect(data.statTrends.value.range).toBe('last20')
  })

  it('shows loading before the first answer', () => {
    mockGetSoloStatTrends.mockReturnValue(new Promise(() => {}))
    const data = useSoloDashboardData()

    expect(data.statTrendsLoading.value).toBe(true)
  })

  it('keeps each card’s error to itself and retries only that card', async () => {
    mockGetSoloWinFactors.mockRejectedValueOnce(new Error('boom'))
    const data = useSoloDashboardData()
    await flushPromises()

    expect(data.winFactorsError.value).toBe(true)
    expect(data.statTrendsError.value).toBe(false)

    vi.clearAllMocks()
    await data.fetchWinFactors()

    expect(mockGetSoloWinFactors).toHaveBeenCalledTimes(1)
    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()
    expect(mockGetSoloClimb).not.toHaveBeenCalled()
    expect(data.winFactorsError.value).toBe(false)
  })

  it('reloads when the active account changes', async () => {
    useSoloDashboardData()
    await flushPromises()
    vi.clearAllMocks()

    authStore.activeAccountPuuid = 'acc-2'
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenCalledTimes(1)
    expect(mockGetSoloWinFactors).toHaveBeenCalledTimes(1)
    expect(mockGetSoloClimb).toHaveBeenCalledTimes(1)
  })

  it('waits for auth, and loads nothing without a linked account', async () => {
    Object.assign(authStore, { isInitialized: false })
    useSoloDashboardData()
    await flushPromises()
    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()

    Object.assign(authStore, { hasLinkedAccount: false, isInitialized: true })
    await flushPromises()
    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()
  })
})
