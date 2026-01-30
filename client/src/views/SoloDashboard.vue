<template>
  <section class="p-lg" data-testid="solo-dashboard">
    <header class="flex items-center justify-between mb-lg" data-testid="dashboard-header">
      <h1 class="sr-only">Solo Dashboard</h1>

      <!-- Queue Toggle Bar -->
      <BaseQueueToggle v-model="queueFilter" />

      <!-- Time Range Filter -->
      <div class="flex flex-col gap-xs">
        <select
          id="time-range-filter"
          v-model="timeRange"
          aria-label="Filter matches by time range"
          class="py-sm px-md bg-[#020617] border border-border rounded-md text-text text-sm cursor-pointer transition-colors duration-200 hover:border-primary focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-[rgba(147,51,234,0.1)]"
        >
          <option value="current_season">Current Season</option>
          <option value="1w">Last Week</option>
          <option value="1m">Last Month</option>
          <option value="3m">Last 3 Months</option>
          <option value="6m">Last 6 Months</option>
          <option value="all">All Time</option>
        </select>
      </div>
    </header>

    <div class="flex flex-col gap-lg">
      <!-- Cards will be added here during refactor -->
    </div>
  </section>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { BaseQueueToggle } from '../components/base'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Track filter changes
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
      // Reset the status after refresh
      resetProgress(puuid)
      break
    }
  }
}, { deep: true })
</script>


