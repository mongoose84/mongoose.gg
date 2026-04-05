import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { ref } from 'vue'
import SoloPage from '@/views/SoloStatsPage.vue'

const mockGetSoloDashboard = vi.fn()
const mockGetWinrateTrend = vi.fn()
const mockGetGoldAt15Trend = vi.fn()
const mockGetCsPerMinuteTrend = vi.fn()
const mockGetDeathsTrend = vi.fn()
const mockGetDragonParticipationTrend = vi.fn()
const mockGetVisionScoreTrend = vi.fn()
const mockGetDeathPositions = vi.fn()
const mockGetRadarChart = vi.fn()
const mockTrackFilterChange = vi.fn()

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    riotAccounts: [],
    refreshUser: vi.fn()
  })
}))

vi.mock('@/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: () => ({
    syncProgress: ref(new Map()),
    resetProgress: vi.fn()
  })
}))

vi.mock('@/services/analyticsApi', () => ({
  trackFilterChange: (...args) => mockTrackFilterChange(...args)
}))

vi.mock('@/services/soloApi', () => ({
  getSoloDashboard: (...args) => mockGetSoloDashboard(...args),
  getDeathPositions: (...args) => mockGetDeathPositions(...args),
  getRadarChart: (...args) => mockGetRadarChart(...args)
}))

vi.mock('@/services/trendsApi', () => ({
  getWinrateTrend: (...args) => mockGetWinrateTrend(...args),
  getGoldAt15Trend: (...args) => mockGetGoldAt15Trend(...args),
  getCsPerMinuteTrend: (...args) => mockGetCsPerMinuteTrend(...args),
  getDeathsTrend: (...args) => mockGetDeathsTrend(...args),
  getDragonParticipationTrend: (...args) => mockGetDragonParticipationTrend(...args),
  getVisionScoreTrend: (...args) => mockGetVisionScoreTrend(...args)
}))

describe('SoloPage', () => {
  beforeEach(() => {
    mockGetSoloDashboard.mockResolvedValue({ gamesPlayed: 0 })
    mockGetWinrateTrend.mockResolvedValue({ winrateTrend: [] })
    mockGetGoldAt15Trend.mockResolvedValue({ goldAt15Trend: [] })
    mockGetCsPerMinuteTrend.mockResolvedValue({ csPerMinuteTrend: [] })
    mockGetDeathsTrend.mockResolvedValue({ deathsTrend: [], averageDeaths: 0, overallAverage: 0, trend: 'neutral' })
    mockGetDragonParticipationTrend.mockResolvedValue({ dragonParticipationTrend: [], averageParticipation: 0, overallAverage: 0, trend: 'neutral' })
    mockGetVisionScoreTrend.mockResolvedValue({ visionScoreTrend: [], averageVisionPerMinute: 0, overallAverage: 0, roleTarget: 1.0, trend: 'neutral' })
    mockGetDeathPositions.mockResolvedValue({ deaths: [], totalDeaths: 0, matchesAnalyzed: 0, phaseSummary: { early: 0, mid: 0, late: 0, veryLate: 0 } })
    mockGetRadarChart.mockResolvedValue({ axes: [], gamesAnalyzed: 0 })
  })

  it('renders deep-analysis slot with radar chart card', async () => {
    const wrapper = mount(SoloPage, {
      global: {
        stubs: {
          BaseQueueToggle: true,
          BaseTimeRangeSelect: true,
          SummaryStatsCard: true,
          TrendChartCard: true,
          WinrateChart: true,
          GoldAt15Chart: true,
          CsPerMinuteChart: true,
          DeathsChart: true,
          DragonParticipationChart: true,
          VisionChart: true,
          DangerZonesMap: true,
          RadarChart: true
        }
      }
    })

    await flushPromises()

    expect(wrapper.find('[data-testid="zone-deep-analysis"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="radar-chart-card"]').exists()).toBe(true)
    expect(mockGetRadarChart).toHaveBeenCalled()
  })
})
