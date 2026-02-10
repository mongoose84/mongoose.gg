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
        :loading="isLoading"
      />
    </template>

    <!-- Zone 3: Trend Charts (LP left, Winrate right) -->
    <template #trend-charts>
      <!-- LP Trend Chart - Ranked Solo/Duo -->
      <TrendChartCard
        v-if="showSoloLpChart"
        title="LP Progression - Ranked Solo/Duo"
        subtitle="Track your ranked solo/duo LP over time"
        :loading="lpLoading"
        test-id="lp-trend-solo-card"
        @toggle-expand="handleLpExpand"
      >
        <template #default="{ dataLimit }">
          <LpChart :data="lpTrendDataSolo" />
        </template>
      </TrendChartCard>

      <!-- LP Trend Chart - Ranked Flex -->
      <TrendChartCard
        v-if="showFlexLpChart"
        title="LP Progression - Ranked Flex"
        subtitle="Track your ranked flex LP over time"
        :loading="lpLoading"
        test-id="lp-trend-flex-card"
        @toggle-expand="handleLpExpand"
      >
        <template #default="{ dataLimit }">
          <LpChart :data="lpTrendDataFlex" />
        </template>
      </TrendChartCard>

      <!-- Winrate Trend Chart -->
      <TrendChartCard
        title="Winrate Over Time"
        subtitle="Rolling 20-game average"
        :loading="winrateLoading"
        test-id="winrate-trend-card"
        @toggle-expand="handleWinrateExpand"
      >
        <template #default="{ dataLimit }">
          <WinrateChart :data="winrateTrendData" />
        </template>
      </TrendChartCard>
    </template>

    <!-- Zone 4 & 5: Not rendered in v1 -->
  </AnalysisLayout>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard, getLpTrend, getWinrateTrend } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'
import TrendChartCard from '../components/solo/TrendChartCard.vue'
import LpChart from '../components/solo/LpChart.vue'
import WinrateChart from '../components/solo/WinrateChart.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// Trend chart data
const lpTrendDataSolo = ref([])
const lpTrendDataFlex = ref([])
const winrateTrendData = ref([])
const lpLoading = ref(false)
const winrateLoading = ref(false)

// Expand state for charts (default: collapsed = last 20 games)
const lpExpanded = ref(false)
const winrateExpanded = ref(false)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Computed properties to determine which LP charts to show
const showSoloLpChart = computed(() => {
  // Show solo chart if:
  // 1. Queue filter is 'all' and there's solo data, OR
  // 2. Queue filter is 'ranked_solo' and there's solo data
  const isRelevantQueue = queueFilter.value === 'all' || queueFilter.value === 'ranked_solo'
  const hasSoloData = lpTrendDataSolo.value && lpTrendDataSolo.value.length > 0
  return isRelevantQueue && (hasSoloData || lpLoading.value)
})

const showFlexLpChart = computed(() => {
  // Show flex chart if:
  // 1. Queue filter is 'all' and there's flex data, OR
  // 2. Queue filter is 'ranked_flex' and there's flex data
  const isRelevantQueue = queueFilter.value === 'all' || queueFilter.value === 'ranked_flex'
  const hasFlexData = lpTrendDataFlex.value && lpTrendDataFlex.value.length > 0
  return isRelevantQueue && (hasFlexData || lpLoading.value)
})

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

// Fetch LP trend data
async function fetchLpTrend() {
  if (!authStore.userId) return

  lpLoading.value = true
  try {
    const limit = lpExpanded.value ? 500 : 20
    const result = await getLpTrend(authStore.userId, queueFilter.value, limit)

    // Handle new response structure with separate arrays for solo and flex
    lpTrendDataSolo.value = result?.rankedSolo ?? []
    lpTrendDataFlex.value = result?.rankedFlex ?? []
  } catch (err) {
    console.error('Failed to fetch LP trend:', err)
    lpTrendDataSolo.value = []
    lpTrendDataFlex.value = []
  } finally {
    lpLoading.value = false
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

// Handle expand toggle for LP chart
function handleLpExpand(expanded) {
  lpExpanded.value = expanded
  fetchLpTrend()
}

// Handle expand toggle for winrate chart
function handleWinrateExpand(expanded) {
  winrateExpanded.value = expanded
  fetchWinrateTrend()
}

// Fetch all data
async function fetchAllData() {
  await Promise.all([
    fetchData(),
    fetchLpTrend(),
    fetchWinrateTrend()
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


