<template>
  <section class="py-lg px-2xl" data-testid="solo-dashboard">
    <h1 class="sr-only">Solo Dashboard</h1>

    <!-- Header with centered Queue Toggle and Time Filter on right -->
    <header class="relative flex items-center justify-center mb-lg" data-testid="dashboard-header">
      <!-- Queue Toggle Bar (centered) -->
      <BaseQueueToggle v-model="queueFilter" />

      <!-- Time Range Filter (positioned right) -->
      <div class="absolute right-0">
        <BaseTimeRangeSelect v-model="timeRange" />
      </div>
    </header>

    <div class="flex flex-col gap-lg">
      <!-- Summary Stats Card -->
      <SummaryStatsCard
        :games-played="dashboardData?.gamesPlayed ?? 0"
        :win-rate="dashboardData?.winRate ?? null"
        :avg-kda="dashboardData?.avgKda ?? null"
        :loading="isLoading"
      />
    </div>
  </section>
</template>

<script setup>
import { ref, watch, onMounted, computed } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloDashboard } from '../services/authApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'
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


