<template>
  <section class="py-lg px-2xl" data-testid="champion-select-page">
    <h1 class="sr-only">Champion Select</h1>

    <!-- Header with centered Queue Toggle and Time Filter on right -->
    <header class="relative flex items-center justify-center mb-lg" data-testid="champion-select-header">
      <!-- Queue Toggle Bar (centered) -->
      <BaseQueueToggle v-model="queueFilter" />

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
      <div class="w-full">
        <MainChampionCard
          v-if="dashboardData?.mainChampions && dashboardData.mainChampions.length"
          :main-champions="dashboardData.mainChampions"
          :user-id="authStore.userId"
          :queue-type="queueFilter"
          :time-range="timeRange"
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

      <!-- Opponent Search Bar (centered) -->
      <div class="flex justify-center mt-2xl">
        <OpponentSearchBar
          :matchups="matchupsData"
          @select="onOpponentSelect"
        />
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getChampionSelectData, getChampionMatchups } from '../services/authApi'
import MainChampionCard from '../components/MainChampionCard.vue'
import OpponentSearchBar from '../components/OpponentSearchBar.vue'
import { BaseQueueToggle } from '../components/base'

const authStore = useAuthStore()

// Dashboard data from API
const dashboardData = ref(null)
const matchupsData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('current_season')

// Fetch champion select data and matchups in parallel
async function fetchData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  const [dashboardResult, matchupsResult] = await Promise.allSettled([
    getChampionSelectData(authStore.userId, queueFilter.value, timeRange.value),
    getChampionMatchups(authStore.userId, queueFilter.value, timeRange.value)
  ])

  if (dashboardResult.status === 'fulfilled') {
    dashboardData.value = dashboardResult.value
  } else {
    console.error('Failed to fetch champion select data:', dashboardResult.reason)
    error.value = dashboardResult.reason?.message || 'Failed to load data'
    dashboardData.value = null
  }

  if (matchupsResult.status === 'fulfilled') {
    matchupsData.value = matchupsResult.value?.matchups || null
  } else {
    console.warn('Failed to fetch matchups:', matchupsResult.reason)
    matchupsData.value = null
  }

  isLoading.value = false
}

// Fetch on mount
onMounted(() => {
  fetchData()
})

// Refetch when filters change
watch([queueFilter, timeRange], () => {
  fetchData()
})

// Refetch when userId becomes available (async auth initialization)
watch(() => authStore.userId, (newUserId, oldUserId) => {
  if (newUserId && !oldUserId) {
    fetchData()
  }
})

// Handle opponent selection from search
function onOpponentSelect(result) {
  // For now, just log the selection - can be extended later to show details
  console.log('Selected matchup:', result)
}
</script>


