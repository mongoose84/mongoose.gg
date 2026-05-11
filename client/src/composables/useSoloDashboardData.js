import { ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from './useSyncWebSocket'
import { useAsyncData } from './useAsyncData'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard, getDeathPositions, getRadarChart, getMatchActivity } from '../services/soloApi'
import {
  getWinrateTrend,
  getGoldAt15Trend,
  getCsPerMinuteTrend,
  getDeathsTrend,
  getDragonParticipationTrend,
  getVisionScoreTrend,
  getDpmTrend
} from '../services/trendsApi'

export function useSoloDashboardData() {
  const authStore = useAuthStore()
  const { syncProgress, resetProgress } = useSyncWebSocket()

  // Filter state
  const queueFilter = ref('all')
  const timeRange = ref('current_season')
  const sideFilter = ref('all')

  // Expand state for trend charts (collapsed = last 20 games)
  const winrateExpanded = ref(false)
  const goldAt15Expanded = ref(false)
  const csPerMinuteExpanded = ref(false)
  const deathsExpanded = ref(false)
  const dragonParticipationExpanded = ref(false)
  const visionScoreExpanded = ref(false)
  const dpmExpanded = ref(false)

  // Dashboard summary data
  const {
    data: dashboardData,
    isLoading,
    error,
    execute: executeDashboardFetch
  } = useAsyncData(async () => {
    return await getSoloDashboard(authStore.userId, queueFilter.value, timeRange.value)
  }, { immediate: false, errorMessage: 'Failed to load solo dashboard' })

  // Winrate trend
  const winrateTrendData = ref([])
  const { isLoading: winrateLoading, execute: executeWinrateTrendFetch } = useAsyncData(
    async (limit) => await getWinrateTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load winrate trend' }
  )

  // Gold at 15 trend
  const goldAt15TrendData = ref([])
  const { isLoading: goldAt15Loading, execute: executeGoldAt15TrendFetch } = useAsyncData(
    async (limit) => await getGoldAt15Trend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load gold at 15 trend' }
  )

  // CS per minute trend
  const csPerMinuteTrendData = ref([])
  const { isLoading: csPerMinuteLoading, execute: executeCsPerMinuteTrendFetch } = useAsyncData(
    async (limit) => await getCsPerMinuteTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load CS per minute trend' }
  )

  // Deaths trend
  const deathsTrendData = ref([])
  const deathsSummary = ref({ averageDeaths: 0, overallAverage: 0, trend: 'neutral' })
  const { isLoading: deathsLoading, execute: executeDeathsTrendFetch } = useAsyncData(
    async (limit) => await getDeathsTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load deaths trend' }
  )

  // Dragon participation trend
  const dragonParticipationTrendData = ref([])
  const dragonParticipationSummary = ref({ averageParticipation: 0, overallAverage: 0, trend: 'neutral' })
  const { isLoading: dragonParticipationLoading, execute: executeDragonParticipationTrendFetch } = useAsyncData(
    async (limit) => await getDragonParticipationTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load dragon participation trend' }
  )

  // Vision score trend
  const visionScoreTrendData = ref([])
  const visionScoreSummary = ref({ averageVisionPerMinute: 0, overallAverage: 0, roleTarget: 1.0, trend: 'neutral' })
  const { isLoading: visionScoreLoading, execute: executeVisionScoreTrendFetch } = useAsyncData(
    async (limit) => await getVisionScoreTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load vision score trend' }
  )

  // DPM trend
  const dpmTrendData = ref([])
  const dpmSummary = ref({ averageDamagePerMinute: 0, overallAverage: 0, trend: 'neutral' })
  const { isLoading: dpmLoading, execute: executeDpmTrendFetch } = useAsyncData(
    async (limit) => await getDpmTrend(authStore.userId, queueFilter.value, timeRange.value, limit),
    { immediate: false, errorMessage: 'Failed to load damage per minute trend' }
  )

  // Radar chart
  const {
    data: radarChartData,
    isLoading: radarChartLoading,
    execute: executeRadarChartFetch
  } = useAsyncData(
    async () => await getRadarChart(authStore.userId, queueFilter.value, timeRange.value),
    { immediate: false, errorMessage: 'Failed to load radar chart' }
  )

  // Death positions
  const {
    data: deathPositionsData,
    isLoading: deathPositionsLoading,
    error: deathPositionsError,
    execute: executeDeathPositionsFetch
  } = useAsyncData(
    async () => await getDeathPositions(authStore.userId, queueFilter.value, timeRange.value, sideFilter.value),
    { immediate: false, errorMessage: 'Failed to load death positions' }
  )

  // Match activity heatmap
  const {
    data: matchActivityData,
    isLoading: matchActivityLoading,
    execute: executeMatchActivityFetch
  } = useAsyncData(
    async () => await getMatchActivity(authStore.userId),
    { immediate: false, errorMessage: 'Failed to load match activity' }
  )

  // Individual fetch functions
  async function fetchData() {
    if (!authStore.userId) return
    try {
      await executeDashboardFetch()
    } catch {
      dashboardData.value = null
    }
  }

  async function fetchWinrateTrend() {
    if (!authStore.userId) return
    try {
      const limit = winrateExpanded.value ? null : 20
      const result = await executeWinrateTrendFetch(limit)
      winrateTrendData.value = result?.winrateTrend ?? []
    } catch {
      winrateTrendData.value = []
    }
  }

  async function fetchGoldAt15Trend() {
    if (!authStore.userId) return
    try {
      const limit = goldAt15Expanded.value ? null : 20
      const result = await executeGoldAt15TrendFetch(limit)
      goldAt15TrendData.value = result?.goldAt15Trend ?? []
    } catch {
      goldAt15TrendData.value = []
    }
  }

  async function fetchCsPerMinuteTrend() {
    if (!authStore.userId) return
    try {
      const limit = csPerMinuteExpanded.value ? null : 20
      const result = await executeCsPerMinuteTrendFetch(limit)
      csPerMinuteTrendData.value = result?.csPerMinuteTrend ?? []
    } catch {
      csPerMinuteTrendData.value = []
    }
  }

  async function fetchDeathsTrend() {
    if (!authStore.userId) return
    try {
      const limit = deathsExpanded.value ? null : 20
      const result = await executeDeathsTrendFetch(limit)
      deathsTrendData.value = result?.deathsTrend ?? []
      deathsSummary.value = {
        averageDeaths: result?.averageDeaths ?? 0,
        overallAverage: result?.overallAverage ?? 0,
        trend: result?.trend ?? 'neutral'
      }
    } catch {
      deathsTrendData.value = []
      deathsSummary.value = { averageDeaths: 0, overallAverage: 0, trend: 'neutral' }
    }
  }

  async function fetchDragonParticipationTrend() {
    if (!authStore.userId) return
    try {
      const limit = dragonParticipationExpanded.value ? null : 20
      const result = await executeDragonParticipationTrendFetch(limit)
      dragonParticipationTrendData.value = result?.dragonParticipationTrend ?? []
      dragonParticipationSummary.value = {
        averageParticipation: result?.averageParticipation ?? 0,
        overallAverage: result?.overallAverage ?? 0,
        trend: result?.trend ?? 'neutral'
      }
    } catch {
      dragonParticipationTrendData.value = []
      dragonParticipationSummary.value = { averageParticipation: 0, overallAverage: 0, trend: 'neutral' }
    }
  }

  async function fetchVisionScoreTrend() {
    if (!authStore.userId) return
    try {
      const limit = visionScoreExpanded.value ? null : 20
      const result = await executeVisionScoreTrendFetch(limit)
      visionScoreTrendData.value = result?.visionScoreTrend ?? []
      visionScoreSummary.value = {
        averageVisionPerMinute: result?.averageVisionPerMinute ?? 0,
        overallAverage: result?.overallAverage ?? 0,
        roleTarget: result?.roleTarget ?? 1.0,
        trend: result?.trend ?? 'neutral'
      }
    } catch {
      visionScoreTrendData.value = []
      visionScoreSummary.value = { averageVisionPerMinute: 0, overallAverage: 0, roleTarget: 1.0, trend: 'neutral' }
    }
  }

  async function fetchDpmTrend() {
    if (!authStore.userId) return
    try {
      const limit = dpmExpanded.value ? null : 20
      const result = await executeDpmTrendFetch(limit)
      dpmTrendData.value = result?.dpmTrend ?? []
      dpmSummary.value = {
        averageDamagePerMinute: result?.averageDamagePerMinute ?? 0,
        overallAverage: result?.overallAverage ?? 0,
        trend: result?.trend ?? 'neutral'
      }
    } catch {
      dpmTrendData.value = []
      dpmSummary.value = { averageDamagePerMinute: 0, overallAverage: 0, trend: 'neutral' }
    }
  }

  async function fetchRadarChart() {
    if (!authStore.userId) return
    try {
      await executeRadarChartFetch()
    } catch {
      radarChartData.value = null
    }
  }

  async function fetchDeathPositions() {
    if (!authStore.userId) return
    try {
      await executeDeathPositionsFetch()
    } catch {
      deathPositionsData.value = null
    }
  }

  async function fetchMatchActivity() {
    if (!authStore.userId) return
    try {
      await executeMatchActivityFetch()
    } catch {
      matchActivityData.value = null
    }
  }

  // Fetch all data in parallel
  async function fetchAllData() {
    await Promise.all([
      fetchData(),
      fetchWinrateTrend(),
      fetchGoldAt15Trend(),
      fetchCsPerMinuteTrend(),
      fetchDeathsTrend(),
      fetchDragonParticipationTrend(),
      fetchVisionScoreTrend(),
      fetchDpmTrend(),
      fetchRadarChart(),
      fetchDeathPositions(),
      fetchMatchActivity()
    ])
  }

  // Expand/collapse handlers
  function handleWinrateExpand(expanded) {
    winrateExpanded.value = expanded
    fetchWinrateTrend()
  }

  function handleGoldAt15Expand(expanded) {
    goldAt15Expanded.value = expanded
    fetchGoldAt15Trend()
  }

  function handleCsPerMinuteExpand(expanded) {
    csPerMinuteExpanded.value = expanded
    fetchCsPerMinuteTrend()
  }

  function handleDeathsExpand(expanded) {
    deathsExpanded.value = expanded
    fetchDeathsTrend()
  }

  function handleDragonParticipationExpand(expanded) {
    dragonParticipationExpanded.value = expanded
    fetchDragonParticipationTrend()
  }

  function handleVisionScoreExpand(expanded) {
    visionScoreExpanded.value = expanded
    fetchVisionScoreTrend()
  }

  function handleDpmExpand(expanded) {
    dpmExpanded.value = expanded
    fetchDpmTrend()
  }

  // Side filter change triggers server re-fetch
  function onSideFilterChange(newSide) {
    sideFilter.value = newSide
    fetchDeathPositions()
  }

  // Re-fetch on filter or active account change
  watch([queueFilter, timeRange], () => { fetchAllData() })
  watch(() => authStore.activeAccountPuuid, () => { fetchAllData() })

  // Track filter analytics
  watch(queueFilter, (newValue) => { trackFilterChange('queue', newValue) })
  watch(timeRange, (newValue) => { trackFilterChange('time', newValue) })

  // Re-fetch on sync completion
  watch(syncProgress, (progress) => {
    for (const [puuid, data] of progress.entries()) {
      if (data.status === 'completed') {
        authStore.refreshUser()
        fetchAllData()
        resetProgress(puuid)
        break
      }
    }
  }, { deep: true })

  return {
    // Filters
    queueFilter,
    timeRange,
    sideFilter,
    // Dashboard summary
    dashboardData,
    isLoading,
    error,
    // Winrate trend
    winrateTrendData,
    winrateLoading,
    // Gold at 15 trend
    goldAt15TrendData,
    goldAt15Loading,
    // CS per minute trend
    csPerMinuteTrendData,
    csPerMinuteLoading,
    // Deaths trend
    deathsTrendData,
    deathsLoading,
    deathsSummary,
    // Dragon participation trend
    dragonParticipationTrendData,
    dragonParticipationLoading,
    dragonParticipationSummary,
    // Vision score trend
    visionScoreTrendData,
    visionScoreLoading,
    visionScoreSummary,
    // DPM trend
    dpmTrendData,
    dpmLoading,
    dpmSummary,
    // Radar chart
    radarChartData,
    radarChartLoading,
    // Death positions
    deathPositionsData,
    deathPositionsLoading,
    deathPositionsError,
    // Match activity
    matchActivityData,
    matchActivityLoading,
    // Handlers
    handleWinrateExpand,
    handleGoldAt15Expand,
    handleCsPerMinuteExpand,
    handleDeathsExpand,
    handleDragonParticipationExpand,
    handleVisionScoreExpand,
    handleDpmExpand,
    onSideFilterChange,
    fetchAllData
  }
}
