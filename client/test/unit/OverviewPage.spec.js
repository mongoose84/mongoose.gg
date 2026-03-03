import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { ref, computed } from 'vue'
import OverviewPage from '@/views/OverviewPage.vue'

const mockGetOverview = vi.fn()
const mockGetMatchActivity = vi.fn()
const mockGetSoloDashboard = vi.fn()

const mockIsOverallMode = ref(false)
const mockActiveAccountPuuid = ref('overall')
const mockSetActiveAccount = vi.fn()

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    primaryRiotAccount: null,
    refreshUser: vi.fn(),
    get isOverallMode() {
      return mockIsOverallMode.value
    },
    get activeAccountPuuid() {
      return mockActiveAccountPuuid.value
    },
    setActiveAccount: mockSetActiveAccount
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
    mockGetOverview.mockReset()
    mockGetMatchActivity.mockResolvedValue(null)
    mockGetSoloDashboard.mockResolvedValue(null)
    mockIsOverallMode.value = false
    mockActiveAccountPuuid.value = 'acc_1'
    mockSetActiveAccount.mockClear()
  })

  function mountPage() {
    return mount(OverviewPage, {
      global: {
        stubs: {
          OverviewAccountCards: true,
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
          OverviewAccountCards: true,
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
    mockGetOverview.mockResolvedValue({
      playerHeader: {
        summonerName: 'Test',
        level: 100,
        region: 'EUW',
        profileIconUrl: 'test.png',
        activeContexts: ['Solo']
      }
    })

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

  describe('Overall Mode', () => {
    it('renders OverviewAccountCards when in Overall mode with account summaries', async () => {
      mockIsOverallMode.value = true
      mockGetOverview.mockResolvedValue({
        accountSummaries: [
          { accountId: 'acc_1', gameName: 'Test1', tagLine: 'EUW', region: 'EUW', rank: 'Gold I', lp: 50, gamesToday: 2, gamesThisWeek: 10 },
          { accountId: 'acc_2', gameName: 'Test2', tagLine: 'NA', region: 'NA', rank: 'Silver II', lp: 30, gamesToday: 0, gamesThisWeek: 5 }
        ]
      })

      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.find('overview-account-cards-stub').exists()).toBe(true)
      expect(wrapper.find('overview-player-header-stub').exists()).toBe(false)
    })

    it('renders OverviewPlayerHeader when in individual account mode', async () => {
      mockIsOverallMode.value = false
      mockGetOverview.mockResolvedValue({
        playerHeader: {
          summonerName: 'Test',
          level: 100,
          region: 'EUW',
          profileIconUrl: 'test.png',
          activeContexts: ['Solo']
        }
      })

      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.find('overview-player-header-stub').exists()).toBe(true)
      expect(wrapper.find('overview-account-cards-stub').exists()).toBe(false)
    })

    it('shows "Highest Rank" label in RankSnapshot when in Overall mode', async () => {
      mockIsOverallMode.value = true
      mockGetOverview.mockResolvedValue({
        rankSnapshot: {
          primaryQueueLabel: 'Ranked Solo/Duo',
          rank: 'Diamond II',
          lp: 67,
          last20Wins: 12,
          last20Losses: 8,
          wlLast20: []
        }
      })

      const wrapper = mount(OverviewPage, {
        global: {
          stubs: {
            OverviewPlayerHeader: true,
            OverviewAccountCards: true,
            MatchActivityHeatmap: true,
            RankSnapshot: {
              props: ['primaryQueueLabel', 'rank', 'lp', 'last20Wins', 'last20Losses', 'wlLast20'],
              template: '<div data-testid="rank-snapshot-stub">{{ primaryQueueLabel }}</div>'
            },
            LastMatchCard: true,
            ChampionSelectCTA: true,
            AnalysisStatusCard: true,
            SoloAnalyticsCTA: true,
            LinkRiotAccountModal: true
          }
        }
      })
      await flushPromises()

      expect(wrapper.find('[data-testid="rank-snapshot-stub"]').text()).toBe('Highest Rank')
    })

    it('shows primary queue label in RankSnapshot when in individual mode', async () => {
      mockIsOverallMode.value = false
      mockGetOverview.mockResolvedValue({
        rankSnapshot: {
          primaryQueueLabel: 'Ranked Solo/Duo',
          rank: 'Diamond II',
          lp: 67,
          last20Wins: 12,
          last20Losses: 8,
          wlLast20: []
        }
      })

      const wrapper = mount(OverviewPage, {
        global: {
          stubs: {
            OverviewPlayerHeader: true,
            OverviewAccountCards: true,
            MatchActivityHeatmap: true,
            RankSnapshot: {
              props: ['primaryQueueLabel', 'rank', 'lp', 'last20Wins', 'last20Losses', 'wlLast20'],
              template: '<div data-testid="rank-snapshot-stub">{{ primaryQueueLabel }}</div>'
            },
            LastMatchCard: true,
            ChampionSelectCTA: true,
            AnalysisStatusCard: true,
            SoloAnalyticsCTA: true,
            LinkRiotAccountModal: true
          }
        }
      })
      await flushPromises()

      expect(wrapper.find('[data-testid="rank-snapshot-stub"]').text()).toBe('Ranked Solo/Duo')
    })

    it('calls setActiveAccount when account is selected', async () => {
      mockIsOverallMode.value = true
      mockGetOverview.mockResolvedValue({
        accountSummaries: [
          { accountId: 'acc_1', gameName: 'Test1', tagLine: 'EUW', region: 'EUW', rank: 'Gold I', lp: 50, gamesToday: 2, gamesThisWeek: 10 }
        ]
      })

      const wrapper = mount(OverviewPage, {
        global: {
          stubs: {
            OverviewPlayerHeader: true,
            OverviewAccountCards: {
              props: ['accounts', 'activeAccountPuuid'],
              template: '<div><button data-testid="account-select" @click="$emit(\'select\', \'acc_1\')">Select</button></div>'
            },
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
      await flushPromises()

      const selectButton = wrapper.find('[data-testid="account-select"]')
      await selectButton.trigger('click')

      expect(mockSetActiveAccount).toHaveBeenCalledWith('acc_1')
    })
  })
})