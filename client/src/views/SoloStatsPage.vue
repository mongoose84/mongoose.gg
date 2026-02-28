<template>
  <AnalysisLayout page-title="Solo Dashboard" data-testid="solo-dashboard">
    <!-- Zone 1: Context Bar -->
    <template #context-bar>
      <!-- Queue Toggle Bar (centered) -->
      <BaseQueueToggle v-model="queueFilter" />

      <!-- Time Range Filter (positioned right) -->
      <div class="absolute right-0">
        <BaseTimeRangeSelect v-model="timeRange" />
      </div>
    </template>

    <!-- Zone 2: Summary Stats -->
    <template #summary>
      <SummaryStatsCard
        :games-played="dashboardData?.gamesPlayed ?? 0"
        :win-rate="dashboardData?.winRate ?? null"
        :overall-win-rate="dashboardData?.overallWinRate ?? null"
        :avg-kda="dashboardData?.avgKda ?? null"
        :avg-kills="dashboardData?.avgKills ?? null"
        :avg-deaths="dashboardData?.avgDeaths ?? null"
        :avg-assists="dashboardData?.avgAssists ?? null"
        :overall-avg-kills="dashboardData?.overallAvgKills ?? null"
        :overall-avg-deaths="dashboardData?.overallAvgDeaths ?? null"
        :overall-avg-assists="dashboardData?.overallAvgAssists ?? null"
        :overall-avg-kda="dashboardData?.overallAvgKda ?? null"
        :solo-duo-rank="dashboardData?.rankInfo?.soloDuoRank ?? null"
        :flex-rank="dashboardData?.rankInfo?.flexRank ?? null"
        :queue-filter="queueFilter"
        :loading="isLoading"
      />
    </template>

    <!-- Zone 3: Trend Charts -->
    <template #trend-charts>
      <!-- Winrate Trend Chart — "How am I doing overall?" -->
      <TrendChartCard
        title="Winrate Over Time"
        subtitle="Rolling 20-game average"
        :loading="winrateLoading"
        test-id="winrate-trend-card"
        @toggle-expand="handleWinrateExpand"
      >
        <template #default="{ dataLimit }">
          <WinrateChart :data="winrateTrendData" :overall-win-rate="dashboardData?.overallWinRate ?? null" />
        </template>
      </TrendChartCard>

      <!-- Deaths Over Time Chart — "What's the #1 thing I can fix?" -->
      <TrendChartCard
        title="Deaths Over Time"
        subtitle="Most actionable metric for improvement"
        :loading="deathsLoading"
        test-id="deaths-trend-card"
        @toggle-expand="handleDeathsExpand"
      >
        <template #default="{ dataLimit }">
          <DeathsChart
            :data="deathsTrendData"
            :overall-average="deathsSummary.overallAverage"
            :trend="deathsSummary.trend"
          />
        </template>
      </TrendChartCard>

      <!-- Dragon Participation Trend Chart — "Am I showing up for objectives?" -->
      <TrendChartCard
        title="Dragon Participation"
        subtitle="First Dragon = 70.69% win rate correlation"
        :loading="dragonParticipationLoading"
        test-id="dragon-participation-trend-card"
        @toggle-expand="handleDragonParticipationExpand"
      >
        <template #default="{ dataLimit }">
          <DragonParticipationChart
            :data="dragonParticipationTrendData"
            :overall-average="dragonParticipationSummary.overallAverage"
            :trend="dragonParticipationSummary.trend"
          />
        </template>
      </TrendChartCard>

      <!-- Vision Score Trend Chart — "Am I giving myself information?" -->
      <TrendChartCard
        title="Vision Score Over Time"
        subtitle="Key metric for map awareness and objective control"
        :loading="visionScoreLoading"
        test-id="vision-score-trend-card"
        @toggle-expand="handleVisionScoreExpand"
      >
        <template #default="{ dataLimit }">
          <VisionChart
            :data="visionScoreTrendData"
            :overall-average="visionScoreSummary.overallAverage"
            :role-target="visionScoreSummary.roleTarget"
            :trend="visionScoreSummary.trend"
          />
        </template>
      </TrendChartCard>

      <!-- Gold at 15 Trend Chart — "Am I winning my lane?" -->
      <TrendChartCard
        title="Gold at 15 Minutes"
        subtitle="Most predictive metric for winning"
        :loading="goldAt15Loading"
        test-id="gold-at-15-trend-card"
        @toggle-expand="handleGoldAt15Expand"
      >
        <template #default="{ dataLimit }">
          <GoldAt15Chart :data="goldAt15TrendData" />
        </template>
      </TrendChartCard>

      <!-- CS Per Minute Trend Chart — "Am I farming efficiently?" -->
      <TrendChartCard
        title="CS Per Minute"
        subtitle="Farming efficiency over time"
        :loading="csPerMinuteLoading"
        test-id="cs-per-minute-trend-card"
        @toggle-expand="handleCsPerMinuteExpand"
      >
        <template #default="{ dataLimit }">
          <CsPerMinuteChart :data="csPerMinuteTrendData" />
        </template>
      </TrendChartCard>
    </template>

    <!-- Zone 4: Deep Analysis -->
    <template #deep-analysis>
      <div class="deep-analysis-grid" data-testid="deep-analysis-grid">
        <BaseCard
          title="Performance Profile"
          subtitle="Your strengths and weaknesses across 6 dimensions"
          data-testid="radar-chart-card"
        >
          <RadarChart
            :axes="radarChartData?.axes ?? []"
            :games-analyzed="radarChartData?.gamesAnalyzed ?? 0"
            :loading="radarChartLoading"
          />
        </BaseCard>

        <BaseCard title="Danger Zones" subtitle="Where you die most on the map" data-testid="danger-zones-card">
          <DangerZonesMap
            :deaths="deathPositionsData?.deaths ?? []"
            :total-deaths="deathPositionsData?.totalDeaths ?? 0"
            :matches-analyzed="deathPositionsData?.matchesAnalyzed ?? 0"
            :phase-summary="deathPositionsData?.phaseSummary ?? { early: 0, mid: 0, late: 0, veryLate: 0 }"
            :loading="deathPositionsLoading"
            :error="deathPositionsError"
            :queue-type="queueFilter"
            :time-range="timeRange"
            @update:side="onSideFilterChange"
          />
        </BaseCard>
      </div>
    </template>

    <!-- Zone 5: Not rendered in v1 -->
  </AnalysisLayout>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { useAsyncData } from '../composables/useAsyncData'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard, getWinrateTrend, getGoldAt15Trend, getCsPerMinuteTrend, getDeathsTrend, getDragonParticipationTrend, getVisionScoreTrend, getDeathPositions, getRadarChart } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect, BaseCard } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'
import TrendChartCard from '../components/solo/TrendChartCard.vue'
import WinrateChart from '../components/solo/WinrateChart.vue'
import GoldAt15Chart from '../components/solo/GoldAt15Chart.vue'
import CsPerMinuteChart from '../components/solo/CsPerMinuteChart.vue'
import DeathsChart from '../components/solo/DeathsChart.vue'
import DragonParticipationChart from '../components/solo/DragonParticipationChart.vue'
import VisionChart from '../components/solo/VisionChart.vue'
import DangerZonesMap from '../components/solo/DangerZonesMap.vue'
import RadarChart from '../components/solo/RadarChart.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const {
  data: dashboardData,
  isLoading,
  error,
  execute: executeDashboardFetch
} = useAsyncData(async () => {
  return await getSoloDashboard(
    authStore.userId,
    queueFilter.value,
    timeRange.value
  )
}, { immediate: false, errorMessage: 'Failed to load solo dashboard' })

// Trend chart data
const winrateTrendData = ref([])
const {
  isLoading: winrateLoading,
  execute: executeWinrateTrendFetch
} = useAsyncData(async (limit) => {
  return await getWinrateTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load winrate trend' })

const goldAt15TrendData = ref([])
const {
  isLoading: goldAt15Loading,
  execute: executeGoldAt15TrendFetch
} = useAsyncData(async (limit) => {
  return await getGoldAt15Trend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load gold at 15 trend' })

const csPerMinuteTrendData = ref([])
const {
  isLoading: csPerMinuteLoading,
  execute: executeCsPerMinuteTrendFetch
} = useAsyncData(async (limit) => {
  return await getCsPerMinuteTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load CS per minute trend' })

const deathsTrendData = ref([])
const {
  isLoading: deathsLoading,
  execute: executeDeathsTrendFetch
} = useAsyncData(async (limit) => {
  return await getDeathsTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load deaths trend' })

const deathsSummary = ref({ averageDeaths: 0, overallAverage: 0, trend: 'neutral' })
const dragonParticipationTrendData = ref([])
const {
  isLoading: dragonParticipationLoading,
  execute: executeDragonParticipationTrendFetch
} = useAsyncData(async (limit) => {
  return await getDragonParticipationTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load dragon participation trend' })

const dragonParticipationSummary = ref({ averageParticipation: 0, overallAverage: 0, trend: 'neutral' })
const visionScoreTrendData = ref([])
const {
  isLoading: visionScoreLoading,
  execute: executeVisionScoreTrendFetch
} = useAsyncData(async (limit) => {
  return await getVisionScoreTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
}, { immediate: false, errorMessage: 'Failed to load vision score trend' })

const visionScoreSummary = ref({ averageVisionPerMinute: 0, overallAverage: 0, roleTarget: 1.0, trend: 'neutral' })

// Radar chart data
const {
  data: radarChartData,
  isLoading: radarChartLoading,
  execute: executeRadarChartFetch
} = useAsyncData(async () => {
  return await getRadarChart(authStore.userId, queueFilter.value, timeRange.value)
}, { immediate: false, errorMessage: 'Failed to load radar chart' })

// Death positions data for danger zones
const {
  data: deathPositionsData,
  isLoading: deathPositionsLoading,
  error: deathPositionsError,
  execute: executeDeathPositionsFetch
} = useAsyncData(async () => {
  return await getDeathPositions(
    authStore.userId,
    queueFilter.value,
    timeRange.value,
    sideFilter.value
  )
}, { immediate: false, errorMessage: 'Failed to load death positions' })

const sideFilter = ref('all')

// Expand state for charts (default: collapsed = last 20 games)
const winrateExpanded = ref(false)
const goldAt15Expanded = ref(false)
const csPerMinuteExpanded = ref(false)
const deathsExpanded = ref(false)
const dragonParticipationExpanded = ref(false)
const visionScoreExpanded = ref(false)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Fetch dashboard data (summary stats)
async function fetchData() {
  if (!authStore.userId) return

  try {
    await executeDashboardFetch()
  } catch {
    dashboardData.value = null
  }
}

// Fetch winrate trend data
async function fetchWinrateTrend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = winrateExpanded.value ? null : 20
    const result = await executeWinrateTrendFetch(limit)
    winrateTrendData.value = result?.winrateTrend ?? []
  } catch {
    winrateTrendData.value = []
  }
}

// Fetch gold at 15 trend data
async function fetchGoldAt15Trend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = goldAt15Expanded.value ? null : 20
    const result = await executeGoldAt15TrendFetch(limit)
    goldAt15TrendData.value = result?.goldAt15Trend ?? []
  } catch {
    goldAt15TrendData.value = []
  }
}

// Fetch CS per minute trend data
async function fetchCsPerMinuteTrend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = csPerMinuteExpanded.value ? null : 20
    const result = await executeCsPerMinuteTrendFetch(limit)
    csPerMinuteTrendData.value = result?.csPerMinuteTrend ?? []
  } catch {
    csPerMinuteTrendData.value = []
  }
}

// Fetch deaths trend data
async function fetchDeathsTrend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
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

// Fetch dragon participation trend data
async function fetchDragonParticipationTrend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
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

// Fetch vision score trend data
async function fetchVisionScoreTrend() {
  if (!authStore.userId) return

  try {
    // Use limit parameter to get exact number of games at full resolution
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

// Fetch radar chart profile data
async function fetchRadarChart() {
  if (!authStore.userId) return

  try {
    await executeRadarChartFetch()
  } catch {
    radarChartData.value = null
  }
}

// Fetch death positions data for danger zones
async function fetchDeathPositions() {
  if (!authStore.userId) return

  try {
    await executeDeathPositionsFetch()
  } catch {
    deathPositionsData.value = null
  }
}

// Handle side filter change (server-side re-fetch)
function onSideFilterChange(newSide) {
  sideFilter.value = newSide
  fetchDeathPositions()
}

// Handle expand toggle for winrate chart
function handleWinrateExpand(expanded) {
  winrateExpanded.value = expanded
  fetchWinrateTrend()
}

// Handle expand toggle for gold at 15 chart
function handleGoldAt15Expand(expanded) {
  goldAt15Expanded.value = expanded
  fetchGoldAt15Trend()
}

// Handle expand toggle for CS per minute chart
function handleCsPerMinuteExpand(expanded) {
  csPerMinuteExpanded.value = expanded
  fetchCsPerMinuteTrend()
}

// Handle expand toggle for deaths chart
function handleDeathsExpand(expanded) {
  deathsExpanded.value = expanded
  fetchDeathsTrend()
}

// Handle expand toggle for dragon participation chart
function handleDragonParticipationExpand(expanded) {
  dragonParticipationExpanded.value = expanded
  fetchDragonParticipationTrend()
}

// Handle expand toggle for vision score chart
function handleVisionScoreExpand(expanded) {
  visionScoreExpanded.value = expanded
  fetchVisionScoreTrend()
}

// Fetch all data
async function fetchAllData() {
  await Promise.all([
    fetchData(),
    fetchWinrateTrend(),
    fetchGoldAt15Trend(),
    fetchCsPerMinuteTrend(),
    fetchDeathsTrend(),
    fetchDragonParticipationTrend(),
    fetchVisionScoreTrend(),
    fetchRadarChart(),
    fetchDeathPositions()
  ])
}

// Fetch data on mount
onMounted(() => {
  fetchAllData()
})

// Re-fetch when filters change
watch([queueFilter, timeRange], () => {
  fetchAllData()
})

// Track filter changes for analytics
watch(queueFilter, (newValue) => {
  trackFilterChange('queue', newValue)
})
watch(timeRange, (newValue) => {
  trackFilterChange('time', newValue)
})

// Watch for sync completion to refresh data
watch(syncProgress, (progress) => {
  for (const [puuid, data] of progress.entries()) {
    if (data.status === 'completed') {
      // Refresh user data to get updated profile icon/level
      authStore.refreshUser()
      // Refresh all dashboard data including charts
      fetchAllData()
      // Reset the status after refresh
      resetProgress(puuid)
      break
    }
  }
}, { deep: true })
</script>

<style scoped>
.deep-analysis-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  align-items: start;
  gap: var(--spacing-lg);
}

@media (max-width: 768px) {
  .deep-analysis-grid {
    grid-template-columns: 1fr;
  }
}
</style>


