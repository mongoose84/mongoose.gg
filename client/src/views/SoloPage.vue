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
      <!-- Winrate Trend Chart -->
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

      <!-- Deaths Over Time Chart -->
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

      <!-- Gold at 15 Trend Chart -->
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

      <!-- CS Per Minute Trend Chart -->
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

      <!-- Dragon Participation Trend Chart -->
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
    </template>

    <!-- Zone 4 & 5: Not rendered in v1 -->
  </AnalysisLayout>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard, getWinrateTrend, getGoldAt15Trend, getCsPerMinuteTrend, getDeathsTrend, getDragonParticipationTrend } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'
import TrendChartCard from '../components/solo/TrendChartCard.vue'
import WinrateChart from '../components/solo/WinrateChart.vue'
import GoldAt15Chart from '../components/solo/GoldAt15Chart.vue'
import CsPerMinuteChart from '../components/solo/CsPerMinuteChart.vue'
import DeathsChart from '../components/solo/DeathsChart.vue'
import DragonParticipationChart from '../components/solo/DragonParticipationChart.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// Trend chart data
const winrateTrendData = ref([])
const winrateLoading = ref(false)
const goldAt15TrendData = ref([])
const goldAt15Loading = ref(false)
const csPerMinuteTrendData = ref([])
const csPerMinuteLoading = ref(false)
const deathsTrendData = ref([])
const deathsLoading = ref(false)
const deathsSummary = ref({ averageDeaths: 0, overallAverage: 0, trend: 'neutral' })
const dragonParticipationTrendData = ref([])
const dragonParticipationLoading = ref(false)
const dragonParticipationSummary = ref({ averageParticipation: 0, overallAverage: 0, trend: 'neutral' })

// Expand state for charts (default: collapsed = last 20 games)
const winrateExpanded = ref(false)
const goldAt15Expanded = ref(false)
const csPerMinuteExpanded = ref(false)
const deathsExpanded = ref(false)
const dragonParticipationExpanded = ref(false)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Fetch dashboard data (summary stats)
async function fetchData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  try {
    dashboardData.value = await getSoloDashboard(
      authStore.userId,
      queueFilter.value,
      timeRange.value
    )
  } catch (err) {
    console.error('Failed to fetch solo dashboard:', err)
    error.value = err.message
    dashboardData.value = null
  } finally {
    isLoading.value = false
  }
}

// Fetch winrate trend data
async function fetchWinrateTrend() {
  if (!authStore.userId) return

  winrateLoading.value = true
  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = winrateExpanded.value ? null : 20
    const result = await getWinrateTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
    winrateTrendData.value = result?.winrateTrend ?? []
  } catch (err) {
    console.error('Failed to fetch winrate trend:', err)
    winrateTrendData.value = []
  } finally {
    winrateLoading.value = false
  }
}

// Fetch gold at 15 trend data
async function fetchGoldAt15Trend() {
  if (!authStore.userId) return

  goldAt15Loading.value = true
  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = goldAt15Expanded.value ? null : 20
    const result = await getGoldAt15Trend(authStore.userId, queueFilter.value, timeRange.value, limit)
    goldAt15TrendData.value = result?.goldAt15Trend ?? []
  } catch (err) {
    console.error('Failed to fetch gold at 15 trend:', err)
    goldAt15TrendData.value = []
  } finally {
    goldAt15Loading.value = false
  }
}

// Fetch CS per minute trend data
async function fetchCsPerMinuteTrend() {
  if (!authStore.userId) return

  csPerMinuteLoading.value = true
  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = csPerMinuteExpanded.value ? null : 20
    const result = await getCsPerMinuteTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
    csPerMinuteTrendData.value = result?.csPerMinuteTrend ?? []
  } catch (err) {
    console.error('Failed to fetch CS per minute trend:', err)
    csPerMinuteTrendData.value = []
  } finally {
    csPerMinuteLoading.value = false
  }
}

// Fetch deaths trend data
async function fetchDeathsTrend() {
  if (!authStore.userId) return

  deathsLoading.value = true
  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = deathsExpanded.value ? null : 20
    const result = await getDeathsTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
    deathsTrendData.value = result?.deathsTrend ?? []
    deathsSummary.value = {
      averageDeaths: result?.averageDeaths ?? 0,
      overallAverage: result?.overallAverage ?? 0,
      trend: result?.trend ?? 'neutral'
    }
  } catch (err) {
    console.error('Failed to fetch deaths trend:', err)
    deathsTrendData.value = []
    deathsSummary.value = { averageDeaths: 0, overallAverage: 0, trend: 'neutral' }
  } finally {
    deathsLoading.value = false
  }
}

// Fetch dragon participation trend data
async function fetchDragonParticipationTrend() {
  if (!authStore.userId) return

  dragonParticipationLoading.value = true
  try {
    // Use limit parameter to get exact number of games at full resolution
    const limit = dragonParticipationExpanded.value ? null : 20
    const result = await getDragonParticipationTrend(authStore.userId, queueFilter.value, timeRange.value, limit)
    dragonParticipationTrendData.value = result?.dragonParticipationTrend ?? []
    dragonParticipationSummary.value = {
      averageParticipation: result?.averageParticipation ?? 0,
      overallAverage: result?.overallAverage ?? 0,
      trend: result?.trend ?? 'neutral'
    }
  } catch (err) {
    console.error('Failed to fetch dragon participation trend:', err)
    dragonParticipationTrendData.value = []
    dragonParticipationSummary.value = { averageParticipation: 0, overallAverage: 0, trend: 'neutral' }
  } finally {
    dragonParticipationLoading.value = false
  }
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

// Fetch all data
async function fetchAllData() {
  await Promise.all([
    fetchData(),
    fetchWinrateTrend(),
    fetchGoldAt15Trend(),
    fetchCsPerMinuteTrend(),
    fetchDeathsTrend(),
    fetchDragonParticipationTrend()
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


