<template>
  <section class="p-lg" data-testid="champion-select-page">
    <h1 class="sr-only">Champion Select</h1>

    <!-- Header with centered Queue Toggle and Time Filter on right -->
    <header class="relative flex items-center justify-center mb-lg" data-testid="champion-select-header">
      <!-- Queue Toggle Bar (centered) -->
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

      <!-- Time Range Filter (positioned right) -->
      <div class="absolute right-0 flex flex-col gap-xs">
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

    <!-- Main content area -->
    <div class="flex flex-col gap-lg">
      <!-- Main Champions Card -->
      <div class="max-w-4xl mx-auto w-full">
        <MainChampionCard
          v-if="dashboardData?.mainChampions && dashboardData.mainChampions.length"
          :main-champions="dashboardData.mainChampions"
        />
        <div v-else-if="isLoading" class="border border-border rounded-lg p-lg bg-background-surface">
          <h2 class="m-0 mb-sm text-lg font-semibold text-text">Main Champions</h2>
          <p class="m-0 text-text-secondary text-sm">Loading champion data...</p>
        </div>
        <div v-else class="border border-border rounded-lg p-lg bg-background-surface">
          <h2 class="m-0 mb-sm text-lg font-semibold text-text">Main Champions</h2>
          <p class="m-0 text-text-secondary text-sm">No champion data yet for this filter.</p>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getChampionSelectData } from '../services/authApi'
import MainChampionCard from '../components/MainChampionCard.vue'

const authStore = useAuthStore()

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// UI state for filters
const queueFilter = ref('ranked_solo')
const timeRange = ref('current_season')

// Queue options for toggle bar
const queueOptions = [
  { value: 'all', label: 'All Queues' },
  { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
  { value: 'ranked_flex', label: 'Ranked Flex' },
  { value: 'normal', label: 'Normal' },
  { value: 'aram', label: 'ARAM' }
]

// Fetch champion select data
async function fetchData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  try {
    const data = await getChampionSelectData(authStore.userId, queueFilter.value, timeRange.value)
    dashboardData.value = data
  } catch (err) {
    console.error('Failed to fetch champion select data:', err)
    error.value = err.message || 'Failed to load data'
    dashboardData.value = null
  } finally {
    isLoading.value = false
  }
}

// Fetch on mount
onMounted(() => {
  fetchData()
})

// Refetch when filters change
watch([queueFilter, timeRange], () => {
  fetchData()
})
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
