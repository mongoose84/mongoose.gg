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
        :avg-kda="dashboardData?.avgKda ?? null"
        :loading="isLoading"
      />
    </template>

    <!-- Zone 3: Trend Charts (to be implemented) -->
    <!-- <template #trend-charts>
      <LpTrendChart />
      <WinrateChart />
    </template> -->

    <!-- Zone 4 & 5: Not rendered in v1 -->
  </AnalysisLayout>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Fetch dashboard data
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

// Fetch data on mount
onMounted(() => {
  fetchData()
})

// Re-fetch when filters change
watch([queueFilter, timeRange], () => {
  fetchData()
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
      // Refresh dashboard data
      fetchData()
      // Reset the status after refresh
      resetProgress(puuid)
      break
    }
  }
}, { deep: true })
</script>


