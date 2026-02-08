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
      <!-- Cards will be added here during refactor -->
    </div>
  </section>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { BaseQueueToggle, BaseTimeRangeSelect } from '../components/base'

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


