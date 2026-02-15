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
          <WinrateChart :data="winrateTrendData" />
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
import { getSoloDashboard, getWinrateTrend } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'
import TrendChartCard from '../components/solo/TrendChartCard.vue'
import WinrateChart from '../components/solo/WinrateChart.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// Trend chart data
const winrateTrendData = ref([])
const winrateLoading = ref(false)

// Expand state for charts (default: collapsed = last 20 games)
const winrateExpanded = ref(false)

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

// Handle expand toggle for winrate chart
function handleWinrateExpand(expanded) {
  winrateExpanded.value = expanded
  fetchWinrateTrend()
}

// Fetch all data
async function fetchAllData() {
  await Promise.all([
    fetchData(),
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


