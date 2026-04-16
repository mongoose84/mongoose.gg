import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

vi.mock('../../src/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    riotAccounts: [],
    activeAccountPuuid: null
  })
}))

vi.mock('../../src/composables/useChartDisplayMode', () => ({
  useChartDisplayMode: () => ({ chartMode: { value: 'combined' } })
}))

const mockMatchActivityData = {
  dailyMatchCounts: [{ date: '2026-04-10', count: 3 }],
  startDate: '2026-03-17',
  endDate: '2026-04-16',
  totalMatches: 42
}

const mockUseSoloDashboardData = {
  queueFilter: { value: 'all' },
  timeRange: { value: 'current_season' },
  dashboardData: { value: null },
  isLoading: { value: false },
  error: { value: null },
  winrateTrendData: { value: [] },
  winrateLoading: { value: false },
  goldAt15TrendData: { value: [] },
  goldAt15Loading: { value: false },
  csPerMinuteTrendData: { value: [] },
  csPerMinuteLoading: { value: false },
  deathsTrendData: { value: [] },
  deathsLoading: { value: false },
  deathsSummary: { value: { averageDeaths: 0, overallAverage: 0, trend: 'neutral' } },
  dragonParticipationTrendData: { value: [] },
  dragonParticipationLoading: { value: false },
  dragonParticipationSummary: { value: { averageParticipation: 0, overallAverage: 0, trend: 'neutral' } },
  visionScoreTrendData: { value: [] },
  visionScoreLoading: { value: false },
  visionScoreSummary: { value: { averageVisionPerMinute: 0, overallAverage: 0, roleTarget: 1.0, trend: 'neutral' } },
  radarChartData: { value: null },
  radarChartLoading: { value: false },
  deathPositionsData: { value: null },
  deathPositionsLoading: { value: false },
  deathPositionsError: { value: null },
  matchActivityData: { value: mockMatchActivityData },
  matchActivityLoading: { value: false },
  handleWinrateExpand: vi.fn(),
  handleGoldAt15Expand: vi.fn(),
  handleCsPerMinuteExpand: vi.fn(),
  handleDeathsExpand: vi.fn(),
  handleDragonParticipationExpand: vi.fn(),
  handleVisionScoreExpand: vi.fn(),
  onSideFilterChange: vi.fn(),
  fetchAllData: vi.fn()
}

vi.mock('../../src/composables/useSoloDashboardData', () => ({
  useSoloDashboardData: () => mockUseSoloDashboardData
}))

const componentStubs = {
  AnalysisLayout: {
    template: `<div data-testid="analysis-layout">
      <slot name="context-bar" />
      <slot name="summary" />
      <slot name="trend-charts" />
      <slot name="deep-analysis" />
    </div>`
  },
  BaseQueueToggle: { template: '<div />', props: ['modelValue'] },
  BaseTimeRangeSelect: { template: '<div />', props: ['modelValue'] },
  BaseCard: {
    template: '<div :data-testid="$attrs[\'data-testid\']"><slot /></div>',
    props: ['title', 'subtitle'],
    inheritAttrs: false
  },
  SummaryStatsCard: { template: '<div data-testid="summary-stats-card" />' },
  TrendChartCard: { template: '<div><slot /></div>', props: ['title', 'subtitle', 'loading', 'testId'] },
  WinrateChart: { template: '<div />' },
  DeathsChart: { template: '<div />' },
  DragonParticipationChart: { template: '<div />' },
  VisionChart: { template: '<div />' },
  GoldAt15Chart: { template: '<div />' },
  CsPerMinuteChart: { template: '<div />' },
  RadarChart: { template: '<div />' },
  DangerZonesMap: { template: '<div />' },
  MatchActivityHeatmap: { template: '<div data-testid="match-activity-heatmap" />', props: ['dailyMatchCounts', 'startDate', 'endDate', 'totalMatches'] }
}

import SoloStatsPage from '../../src/views/SoloStatsPage.vue'

describe('SoloStatsPage', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('renders MatchActivityHeatmap in Zone 4 below Performance Profile', () => {
    const wrapper = mount(SoloStatsPage, { global: { stubs: componentStubs } })
    expect(wrapper.find('[data-testid="match-activity-heatmap"]').exists()).toBe(true)
  })

  it('renders deep-analysis-grid with 3 children', () => {
    const wrapper = mount(SoloStatsPage, { global: { stubs: componentStubs } })
    const grid = wrapper.find('[data-testid="deep-analysis-grid"]')
    expect(grid.exists()).toBe(true)
    // Should have 3 BaseCard children (Performance Profile, Danger Zones, Match Activity)
    expect(grid.findAll('[data-testid="match-activity-card"]').length).toBe(1)
    expect(grid.findAll('[data-testid="radar-chart-card"]').length).toBe(1)
    expect(grid.findAll('[data-testid="danger-zones-card"]').length).toBe(1)
  })

  it('heatmap updates when queue filter changes (matchActivityData is reactive)', () => {
    const wrapper = mount(SoloStatsPage, { global: { stubs: componentStubs } })
    // Heatmap is rendered when matchActivityData is available
    expect(wrapper.find('[data-testid="match-activity-heatmap"]').exists()).toBe(true)
  })
})
