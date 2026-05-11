import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref, nextTick } from 'vue'

const mockGetSoloDashboard = vi.fn()
const mockGetWinrateTrend = vi.fn()
const mockGetGoldAt15Trend = vi.fn()
const mockGetCsPerMinuteTrend = vi.fn()
const mockGetDeathsTrend = vi.fn()
const mockGetDragonParticipationTrend = vi.fn()
const mockGetVisionScoreTrend = vi.fn()
const mockGetDpmTrend = vi.fn()
const mockGetDeathPositions = vi.fn()
const mockGetRadarChart = vi.fn()
const mockTrackFilterChange = vi.fn()
const mockRefreshUser = vi.fn()
const mockResetProgress = vi.fn()

const syncProgress = ref(new Map())

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    activeAccountPuuid: null,
    refreshUser: mockRefreshUser
  })
}))

vi.mock('@/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: () => ({
    syncProgress,
    resetProgress: mockResetProgress
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
  getVisionScoreTrend: (...args) => mockGetVisionScoreTrend(...args),
  getDpmTrend: (...args) => mockGetDpmTrend(...args)
}))

import { useSoloDashboardData } from '@/composables/useSoloDashboardData'

describe('useSoloDashboardData', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    syncProgress.value = new Map()
    mockGetSoloDashboard.mockResolvedValue({ gamesPlayed: 10, accountCount: 1 })
    mockGetWinrateTrend.mockResolvedValue({ winrateTrend: [{ value: 0.5 }] })
    mockGetGoldAt15Trend.mockResolvedValue({ goldAt15Trend: [] })
    mockGetCsPerMinuteTrend.mockResolvedValue({ csPerMinuteTrend: [] })
    mockGetDeathsTrend.mockResolvedValue({ deathsTrend: [], averageDeaths: 2, overallAverage: 3, trend: 'improving' })
    mockGetDragonParticipationTrend.mockResolvedValue({ dragonParticipationTrend: [], averageParticipation: 0.5, overallAverage: 0.6, trend: 'neutral' })
    mockGetVisionScoreTrend.mockResolvedValue({ visionScoreTrend: [], averageVisionPerMinute: 1.2, overallAverage: 1.0, roleTarget: 1.5, trend: 'improving' })
    mockGetDpmTrend.mockResolvedValue({ dpmTrend: [], averageDamagePerMinute: 850, overallAverage: 900, trend: 'neutral' })
    mockGetDeathPositions.mockResolvedValue({ deaths: [], totalDeaths: 0, matchesAnalyzed: 0, phaseSummary: { early: 0, mid: 0, late: 0, veryLate: 0 } })
    mockGetRadarChart.mockResolvedValue({ axes: [], gamesAnalyzed: 5 })
  })

  it('starts with default filter values', () => {
    const { queueFilter, timeRange } = useSoloDashboardData()

    expect(queueFilter.value).toBe('all')
    expect(timeRange.value).toBe('current_season')
  })

  it('fetchAllData calls all 10 endpoints', async () => {
    const { fetchAllData } = useSoloDashboardData()

    await fetchAllData()

    expect(mockGetSoloDashboard).toHaveBeenCalled()
    expect(mockGetWinrateTrend).toHaveBeenCalled()
    expect(mockGetGoldAt15Trend).toHaveBeenCalled()
    expect(mockGetCsPerMinuteTrend).toHaveBeenCalled()
    expect(mockGetDeathsTrend).toHaveBeenCalled()
    expect(mockGetDragonParticipationTrend).toHaveBeenCalled()
    expect(mockGetVisionScoreTrend).toHaveBeenCalled()
    expect(mockGetDpmTrend).toHaveBeenCalled()
    expect(mockGetRadarChart).toHaveBeenCalled()
    expect(mockGetDeathPositions).toHaveBeenCalled()
  })

  it('populates winrateTrendData from API response', async () => {
    const { fetchAllData, winrateTrendData } = useSoloDashboardData()

    await fetchAllData()

    expect(winrateTrendData.value).toEqual([{ value: 0.5 }])
  })

  it('populates deathsSummary from deaths trend response', async () => {
    const { fetchAllData, deathsSummary } = useSoloDashboardData()

    await fetchAllData()

    expect(deathsSummary.value).toEqual({
      averageDeaths: 2,
      overallAverage: 3,
      trend: 'improving'
    })
  })

  it('populates visionScoreSummary from vision score response', async () => {
    const { fetchAllData, visionScoreSummary } = useSoloDashboardData()

    await fetchAllData()

    expect(visionScoreSummary.value).toEqual({
      averageVisionPerMinute: 1.2,
      overallAverage: 1.0,
      roleTarget: 1.5,
      trend: 'improving'
    })
  })

  it('populates dpmTrendData from API response', async () => {
    mockGetDpmTrend.mockResolvedValue({ dpmTrend: [{ damagePerMinute: 850 }], averageDamagePerMinute: 850, overallAverage: 900, trend: 'neutral' })
    const { fetchAllData, dpmTrendData } = useSoloDashboardData()

    await fetchAllData()

    expect(dpmTrendData.value).toEqual([{ damagePerMinute: 850 }])
  })

  it('populates dpmSummary from DPM trend response', async () => {
    mockGetDpmTrend.mockResolvedValue({ dpmTrend: [], averageDamagePerMinute: 850, overallAverage: 900, trend: 'improving' })
    const { fetchAllData, dpmSummary } = useSoloDashboardData()

    await fetchAllData()

    expect(dpmSummary.value).toEqual({
      averageDamagePerMinute: 850,
      overallAverage: 900,
      trend: 'improving'
    })
  })

  it('handleDpmExpand re-fetches with null limit when expanded', async () => {
    const { handleDpmExpand } = useSoloDashboardData()

    await handleDpmExpand(true)

    const lastCall = mockGetDpmTrend.mock.calls[mockGetDpmTrend.mock.calls.length - 1]
    expect(lastCall[3]).toBeNull()
  })

  it('handleDpmExpand re-fetches with limit 20 when collapsed', async () => {
    const { handleDpmExpand } = useSoloDashboardData()

    await handleDpmExpand(false)

    const lastCall = mockGetDpmTrend.mock.calls[mockGetDpmTrend.mock.calls.length - 1]
    expect(lastCall[3]).toBe(20)
  })

  it('gracefully resets dpmTrendData to [] on fetch failure', async () => {
    mockGetDpmTrend.mockRejectedValue(new Error('Network error'))
    const { fetchAllData, dpmTrendData } = useSoloDashboardData()

    await fetchAllData()

    expect(dpmTrendData.value).toEqual([])
  })

  it('handleWinrateExpand re-fetches with null limit when expanded', async () => {
    const { handleWinrateExpand } = useSoloDashboardData()

    await handleWinrateExpand(true)

    const lastCall = mockGetWinrateTrend.mock.calls[mockGetWinrateTrend.mock.calls.length - 1]
    expect(lastCall[3]).toBeNull()
  })

  it('handleWinrateExpand re-fetches with limit 20 when collapsed', async () => {
    const { handleWinrateExpand } = useSoloDashboardData()

    await handleWinrateExpand(false)

    const lastCall = mockGetWinrateTrend.mock.calls[mockGetWinrateTrend.mock.calls.length - 1]
    expect(lastCall[3]).toBe(20)
  })

  it('onSideFilterChange updates sideFilter and re-fetches death positions', async () => {
    const { onSideFilterChange, sideFilter } = useSoloDashboardData()

    const callsBefore = mockGetDeathPositions.mock.calls.length
    onSideFilterChange('blue')
    await nextTick()
    await Promise.resolve()

    expect(sideFilter.value).toBe('blue')
    expect(mockGetDeathPositions.mock.calls.length).toBeGreaterThan(callsBefore)
  })

  it('gracefully resets winrateTrendData to [] on fetch failure', async () => {
    mockGetWinrateTrend.mockRejectedValue(new Error('Network error'))
    const { fetchAllData, winrateTrendData } = useSoloDashboardData()

    await fetchAllData()

    expect(winrateTrendData.value).toEqual([])
  })

  it('calls refreshUser and resetProgress when sync completes', async () => {
    useSoloDashboardData()

    vi.clearAllMocks()
    mockGetSoloDashboard.mockResolvedValue({ gamesPlayed: 0 })
    mockGetWinrateTrend.mockResolvedValue({ winrateTrend: [] })
    mockGetGoldAt15Trend.mockResolvedValue({ goldAt15Trend: [] })
    mockGetCsPerMinuteTrend.mockResolvedValue({ csPerMinuteTrend: [] })
    mockGetDeathsTrend.mockResolvedValue({ deathsTrend: [], averageDeaths: 0, overallAverage: 0, trend: 'neutral' })
    mockGetDragonParticipationTrend.mockResolvedValue({ dragonParticipationTrend: [], overallAverage: 0, trend: 'neutral' })
    mockGetVisionScoreTrend.mockResolvedValue({ visionScoreTrend: [], overallAverage: 0, trend: 'neutral' })
    mockGetDpmTrend.mockResolvedValue({ dpmTrend: [], overallAverage: 0, trend: 'neutral' })
    mockGetDeathPositions.mockResolvedValue({ deaths: [] })
    mockGetRadarChart.mockResolvedValue({ axes: [] })

    syncProgress.value = new Map([['puuid-1', { status: 'completed' }]])
    await nextTick()
    await nextTick()

    expect(mockRefreshUser).toHaveBeenCalled()
    expect(mockResetProgress).toHaveBeenCalledWith('puuid-1')
  })
})
