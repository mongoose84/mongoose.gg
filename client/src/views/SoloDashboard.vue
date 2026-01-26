<template>
  <section class="p-lg" data-testid="solo-dashboard">
    <header class="flex items-center justify-between mb-lg" data-testid="dashboard-header">
      <h1 class="sr-only">Solo Dashboard</h1>

      <!-- Queue Toggle Bar -->
      <div class="flex border border-border rounded-md overflow-hidden bg-background-surface" role="group" aria-label="Filter by queue type">
        <button
          v-for="queue in queueOptions"
          :key="queue.value"
          type="button"
          class="queue-toggle-btn py-sm px-md bg-transparent border-none text-text-secondary text-sm font-medium cursor-pointer transition-colors duration-200 relative focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-soft"
          :class="{
            'queue-toggle-btn--active': queueFilter === queue.value,
            'hover:text-text hover:bg-background-elevated': queueFilter !== queue.value
          }"
          @click="queueFilter = queue.value"
          :aria-pressed="queueFilter === queue.value"
        >
          {{ queue.label }}
        </button>
      </div>

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

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Queue options for toggle bar
const queueOptions = [
  { value: 'all', label: 'All Queues' },
  { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
  { value: 'ranked_flex', label: 'Ranked Flex' },
  { value: 'normal', label: 'Normal' },
  { value: 'aram', label: 'ARAM' }
]

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

<style scoped>
/* Active state with darker purple for better visibility */
.queue-toggle-btn--active {
  background-color: #5b21b6; /* Darker purple (violet-800) */
  color: white;
}

/* Queue toggle button dividers (pseudo-elements can't be done in Tailwind) */
.queue-toggle-btn:not(:last-child)::after {
  content: '';
  position: absolute;
  right: 0;
  top: 25%;
  height: 50%;
  width: 1px;
  background: var(--color-border);
}

/* Hide divider when button is active or next to active */
.queue-toggle-btn--active::after {
  display: none;
}

.queue-toggle-btn:has(+ .queue-toggle-btn--active)::after {
  display: none;
}
</style>
