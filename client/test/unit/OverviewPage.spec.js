import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

// Mock external dependencies
vi.mock('../../src/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    isOverallMode: false,
    riotAccounts: [],
    activeAccountPuuid: null,
    refreshUser: vi.fn(),
    setActiveAccount: vi.fn()
  })
}))

vi.mock('../../src/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: () => ({
    syncProgress: { entries: () => [] },
    resetProgress: vi.fn()
  })
}))

vi.mock('../../src/composables/useAsyncData', () => ({
  useAsyncData: (fn) => ({
    data: { value: null },
    error: { value: null },
    isLoading: { value: false },
    execute: vi.fn().mockResolvedValue({ overview: null, soloDashboard: null })
  })
}))

vi.mock('../../src/services/soloApi', () => ({
  getOverview: vi.fn().mockResolvedValue(null),
  getSoloDashboard: vi.fn().mockResolvedValue(null)
}))

vi.mock('../../src/utils/leagueAssets', () => ({
  getChampionSplashUrl: vi.fn().mockReturnValue('')
}))

// Stub child components to avoid deep rendering
const componentStubs = {
  OverviewLayout: {
    template: `<div data-testid="overview-layout">
      <slot name="header" />
      <slot name="glance-left" />
      <slot name="glance-right" />
      <slot name="actions-left" />
      <slot name="actions-right" />
      <slot name="latest-match" />
    </div>`
  },
  OverviewAccountCards: { template: '<div data-testid="overview-account-cards" />' },
  OverviewPlayerHeader: { template: '<div data-testid="overview-player-header" />', props: ['rank', 'lp', 'primaryQueueLabel', 'summonerName', 'level', 'region', 'profileIconUrl', 'activeContexts'] },
  TodaySessionCard: { template: '<div data-testid="today-session-card" />', props: ['sessionStats', 'combinedStats', 'loading'] },
  DeathInsightsCard: { template: '<div data-testid="death-insights-card" />', props: ['survivalStats', 'loading'] },
  ChampionSelectCTA: { template: '<div data-testid="champion-select-cta" />', props: ['muralUrl', 'championName'] },
  AnalysisStatusCard: { template: '<div data-testid="analysis-status-card" />' },
  SoloAnalyticsCTA: { template: '<div data-testid="solo-analytics-cta" />', props: ['subtitle'] },
  LastMatchCard: { template: '<div data-testid="last-match-card" />', props: ['matchId', 'championIconUrl', 'championName', 'result', 'kda', 'timestamp', 'queueType', 'accountName'] },
  LinkRiotAccountModal: { template: '<div />', props: ['isOpen'] }
}

import OverviewPage from '../../src/views/OverviewPage.vue'

describe('OverviewPage', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders TodaySessionCard in glance-left slot', () => {
    const wrapper = mount(OverviewPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="today-session-card"]').exists()).toBe(true)
  })

  it('renders DeathInsightsCard in glance-right slot', () => {
    const wrapper = mount(OverviewPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="death-insights-card"]').exists()).toBe(true)
  })

  it('renders ChampionSelectCTA in actions-left slot', () => {
    const wrapper = mount(OverviewPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="champion-select-cta"]').exists()).toBe(true)
  })

  it('does not render RankSnapshot component', () => {
    const wrapper = mount(OverviewPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="rank-snapshot"]').exists()).toBe(false)
  })

  it('does not render MatchActivityHeatmap on Overview', () => {
    const wrapper = mount(OverviewPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="match-activity-heatmap"]').exists()).toBe(false)
  })
})
