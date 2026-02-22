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

  function mountPageWithCtaStub() {
    return mount(OverviewPage, {
      global: {
        stubs: {
          OverviewPlayerHeader: true,
          MatchActivityHeatmap: true,
          RankSnapshot: true,
          LastMatchCard: true,
          ChampionSelectCTA: {
            props: ['muralUrl', 'championName'],
            template: '<div data-testid="champion-select-cta-stub">{{ championName }}|{{ muralUrl }}</div>'
          },
          AnalysisStatusCard: true,
          SoloAnalyticsCTA: {
            props: ['subtitle', 'trendDirection'],
            template: '<div data-testid="solo-cta-stub">{{ subtitle }}|{{ trendDirection }}</div>'
          },
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

  it('passes KDA trend subtitle and direction to SoloAnalyticsCTA', async () => {
    mockGetOverview.mockResolvedValue({})
    mockGetSoloDashboard.mockResolvedValue({
      avgKda: 3.4,
      overallAvgKda: 2.9
    })

    const wrapper = mountPageWithCtaStub()
    await flushPromises()

    expect(wrapper.find('[data-testid="solo-cta-stub"]').text()).toBe('KDA trend: 3.4 (+0.5 vs overall)|up')
  })

  it('passes neutral trend when KDA difference is below threshold', async () => {
    mockGetOverview.mockResolvedValue({})
    mockGetSoloDashboard.mockResolvedValue({
      avgKda: 3.02,
      overallAvgKda: 3.0
    })

    const wrapper = mountPageWithCtaStub()
    await flushPromises()

    expect(wrapper.find('[data-testid="solo-cta-stub"]').text()).toBe('KDA trend: 3.0 (even vs overall)|neutral')
  })

  it('passes down trend when KDA is below overall baseline', async () => {
    mockGetOverview.mockResolvedValue({})
    mockGetSoloDashboard.mockResolvedValue({
      avgKda: 2.3,
      overallAvgKda: 2.9
    })

    const wrapper = mountPageWithCtaStub()
    await flushPromises()

    expect(wrapper.find('[data-testid="solo-cta-stub"]').text()).toBe('KDA trend: 2.3 (-0.6 vs overall)|down')
  })

  it('passes mural props to ChampionSelectCTA when most played champion exists', async () => {
    mockGetOverview.mockResolvedValue({
      mostPlayedChampion: {
        championName: 'Ahri',
        gamesPlayed: 28,
        source: 'current_season'
      }
    })

    const wrapper = mountPageWithCtaStub()
    await flushPromises()

    expect(wrapper.find('[data-testid="champion-select-cta-stub"]').text())
      .toBe('Ahri|https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Ahri_0.jpg')
  })

  it('passes empty mural props to ChampionSelectCTA when no champion data exists', async () => {
    mockGetOverview.mockResolvedValue({})

    const wrapper = mountPageWithCtaStub()
    await flushPromises()

    expect(wrapper.find('[data-testid="champion-select-cta-stub"]').text()).toBe('|')
  })
})