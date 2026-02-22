import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { ref } from 'vue'
import OverviewPage from '@/views/OverviewPage.vue'

const mockGetOverview = vi.fn()
const mockGetMatchActivity = vi.fn()
const mockGetSoloDashboard = vi.fn()

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    primaryRiotAccount: null,
    refreshUser: vi.fn()
  })
}))

vi.mock('@/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: () => ({
    syncProgress: ref(new Map()),
    subscribe: vi.fn(),
    resetProgress: vi.fn()
  })
}))

vi.mock('@/services/authApi', () => ({
  getOverview: (...args) => mockGetOverview(...args),
  getMatchActivity: (...args) => mockGetMatchActivity(...args),
  getSoloDashboard: (...args) => mockGetSoloDashboard(...args)
}))

describe('OverviewPage', () => {
  beforeEach(() => {
    mockGetMatchActivity.mockResolvedValue(null)
    mockGetSoloDashboard.mockResolvedValue(null)
  })

  function mountPage() {
    return mount(OverviewPage, {
      global: {
        stubs: {
          OverviewPlayerHeader: true,
          MatchActivityHeatmap: true,
          RankSnapshot: true,
          LastMatchCard: true,
          ChampionSelectCTA: true,
          AnalysisStatusCard: true,
          SoloAnalyticsCTA: true,
          LinkRiotAccountModal: true
        }
      }
    })
  }

  it('renders SoloAnalyticsCTA when overview data is present', async () => {
    mockGetOverview.mockResolvedValue({})

    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('.recent-right-stack').exists()).toBe(true)
    expect(wrapper.find('solo-analytics-c-t-a-stub').exists()).toBe(true)
  })

  it('does not render SoloAnalyticsCTA when overview data is empty', async () => {
    mockGetOverview.mockResolvedValue(null)

    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('solo-analytics-c-t-a-stub').exists()).toBe(false)
  })
})