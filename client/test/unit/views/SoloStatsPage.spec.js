import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { reactive, ref } from 'vue'
import SoloPage from '@/views/SoloStatsPage.vue'
import { statTrendsResponse, winFactorsResponse } from '@test/helpers/soloFixtures'

const mockGetSoloStatTrends = vi.fn()
const mockGetSoloWinFactors = vi.fn()
const mockGetDeathPositions = vi.fn()
const mockStartSync = vi.fn()
const syncState = ref(null)

const authStore = reactive({
  userId: 1,
  isInitialized: true,
  hasLinkedAccount: true,
  activeAccountPuuid: 'acc-1',
  refreshUser: vi.fn()
})

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => authStore
}))

vi.mock('@/services/analyticsApi', () => ({
  trackFilterChange: vi.fn()
}))

vi.mock('@/services/soloApi', () => ({
  getSoloStatTrends: (...args) => mockGetSoloStatTrends(...args),
  getSoloWinFactors: (...args) => mockGetSoloWinFactors(...args),
  getDeathPositions: (...args) => mockGetDeathPositions(...args)
}))

vi.mock('@/composables/useSyncMatches', () => ({
  useSyncMatches: () => ({
    syncState,
    isSyncing: ref(false),
    progressCurrent: ref(0),
    progressTotal: ref(0),
    syncedCount: ref(null),
    startSync: mockStartSync
  })
}))

let mounted = null

function mountPage() {
  mounted = mount(SoloPage, {
    global: {
      stubs: { DangerZonesMap: true, LinkRiotAccountModal: true, SyncProgress: true }
    }
  })
  return mounted
}

describe('SoloStatsPage', () => {
  afterEach(() => {
    mounted?.unmount()
    mounted = null
  })

  beforeEach(() => {
    vi.clearAllMocks()
    syncState.value = null
    Object.assign(authStore, { isInitialized: true, hasLinkedAccount: true, activeAccountPuuid: 'acc-1' })
    mockGetSoloStatTrends.mockResolvedValue(statTrendsResponse())
    mockGetSoloWinFactors.mockResolvedValue(winFactorsResponse())
    mockGetDeathPositions.mockResolvedValue(null)
  })

  it('opens with the headline and the stat that moved most', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="solo-headline"]').text()).toBe('Your last 20 matches')
    expect(wrapper.get('[data-testid="solo-subline"]').text()).toBe('Fewer deaths did most of it: 4.1 per match, down from 5.6.')
  })

  it('shows the queue the server picked and the range', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="queue-ranked_solo"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-testid="range-last20"]').attributes('aria-pressed')).toBe('true')
  })

  it('shows every card', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="solo-stat-trends"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="danger-zones-card"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="solo-win-factors"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="solo-patterns"]').exists()).toBe(true)
  })

  it('reloads the cards when the range changes', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('[data-testid="range-season"]').trigger('click')
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenLastCalledWith(1, null, 'season')
    expect(mockGetSoloWinFactors).toHaveBeenLastCalledWith(1, null, 'season')
  })

  it('switches to all queues from a queue with no matches', async () => {
    mockGetSoloStatTrends.mockResolvedValue(statTrendsResponse({ matches: 0, queueType: 'ranked_flex' }))
    mockGetSoloWinFactors.mockResolvedValue(winFactorsResponse({ matches: 0, queueType: 'ranked_flex', factors: [] }))
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('[data-testid="solo-stat-trends-show-all"]').trigger('click')
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenLastCalledWith(1, 'all', 'last20')
  })

  it('retries only the card that failed', async () => {
    mockGetSoloWinFactors.mockRejectedValueOnce(new Error('boom'))
    const wrapper = mountPage()
    await flushPromises()
    vi.clearAllMocks()

    await wrapper.get('[data-testid="solo-win-factors-retry"]').trigger('click')
    await flushPromises()

    expect(mockGetSoloWinFactors).toHaveBeenCalledTimes(1)
    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()
    expect(wrapper.find('[data-testid="solo-win-factors"]').exists()).toBe(true)
  })

  it('reloads every card after a sync finishes', async () => {
    mountPage()
    await flushPromises()
    vi.clearAllMocks()

    syncState.value = 'running'
    await flushPromises()
    syncState.value = 'done'
    await flushPromises()

    expect(mockGetSoloStatTrends).toHaveBeenCalledTimes(1)
    expect(mockGetSoloWinFactors).toHaveBeenCalledTimes(1)
  })

  it('asks to link a Riot account when none is linked', async () => {
    authStore.hasLinkedAccount = false
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="solo-no-account"]').text()).toContain('Link your Riot account to see your trends')
    expect(mockGetSoloStatTrends).not.toHaveBeenCalled()
  })
})
