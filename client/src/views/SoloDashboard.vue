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
          class="queue-toggle-btn py-sm px-md bg-transparent border-none text-text-secondary text-sm font-medium cursor-pointer transition-all duration-200 relative focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-soft"
          :class="{
            'bg-primary text-white': queueFilter === queue.value,
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
      <!-- First row: Main Champions -->
      <div class="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] gap-lg items-stretch">
        <div class="min-w-0">
          <MainChampionCard
            v-if="dashboardData?.mainChampions && dashboardData.mainChampions.length"
            :main-champions="dashboardData.mainChampions"
          />
          <div v-else class="border border-border rounded-lg p-lg bg-background-surface">
            <h2 class="m-0 mb-sm text-lg font-semibold text-text">Main Champions</h2>
            <p class="m-0 text-text-secondary text-sm">No champion data yet for this filter.</p>
          </div>
        </div>
      </div>

      <!-- Second row: Stats charts + Champion Matchups -->
      <div class="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] gap-lg items-stretch">
        <div class="min-w-0">
          <div class="flex flex-col gap-lg h-full">
            <WinrateChart
              v-if="winrateTrendData && winrateTrendData.length > 0"
              :winrate-trend="winrateTrendData"
            />
            <div v-else class="flex-1 border border-border rounded-lg p-lg bg-background-surface">
              <h2 class="m-0 mb-sm text-lg font-semibold text-text">Winrate Over Time</h2>
              <p class="m-0 text-text-secondary text-sm">No data for selected time range.</p>
            </div>
            <LpTrendChart
              v-if="lpTrendData && lpTrendData.length > 0"
              :lp-trend="lpTrendData"
            />
            <div v-else-if="isRankedQueue" class="flex-1 border border-border rounded-lg p-lg bg-background-surface">
              <h2 class="m-0 mb-sm text-lg font-semibold text-text">LP Over Time</h2>
              <p class="m-0 text-text-secondary text-sm">No LP data available yet. LP tracking starts after your next ranked game.</p>
            </div>
          </div>
        </div>
        <div class="min-w-0">
          <ChampionMatchupsTable
            v-if="matchupsData && matchupsData.length > 0"
            :matchups="matchupsData"
            class="h-full"
          />
          <div v-else class="h-full border border-border rounded-lg p-lg bg-background-surface">
            <h2 class="m-0 mb-sm text-lg font-semibold text-text">Champion Matchups</h2>
            <p class="m-0 text-text-secondary text-sm">No matchup data for selected filter. Play more games to see your champion matchups.</p>
          </div>
        </div>
      </div>

      <div class="border border-border rounded-lg p-lg bg-background-surface">
        <h2 class="m-0 mb-sm text-lg font-semibold text-text">Goals Panel</h2>
        <p class="m-0 text-text-secondary text-sm">Active goals and progress (upgrade CTA for Free).</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getSoloDashboard, getWinrateTrend, getChampionMatchups } from '../services/authApi'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import MainChampionCard from '../components/MainChampionCard.vue'
import WinrateChart from '../components/WinrateChart.vue'
import LpTrendChart from '../components/LpTrendChart.vue'
import ChampionMatchupsTable from '../components/ChampionMatchupsTable.vue'

const authStore = useAuthStore()
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket()

// Dashboard data from API
const dashboardData = ref(null)
const winrateTrendData = ref(null)
const matchupsData = ref(null)
const isLoading = ref(false)
const error = ref(null)

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

// LP trend data from dashboard response
const lpTrendData = computed(() => dashboardData.value?.lpTrend || [])

// Check if current queue filter includes ranked modes (for showing LP chart)
const isRankedQueue = computed(() =>
  ['all', 'ranked_solo', 'ranked_flex'].includes(queueFilter.value)
)

  // Fetch dashboard data, winrate trend, and matchups in parallel (isolated failures)
  async function fetchDashboardData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

    // Fetch in parallel but handle errors independently
    const [dashboardResult, trendResult, matchupsResult] = await Promise.allSettled([
      getSoloDashboard(authStore.userId, queueFilter.value, timeRange.value),
      getWinrateTrend(authStore.userId, queueFilter.value, timeRange.value),
      getChampionMatchups(authStore.userId, queueFilter.value, timeRange.value)
    ])

    // Handle dashboard result
    if (dashboardResult.status === 'fulfilled') {
      dashboardData.value = dashboardResult.value
    } else {
      console.error('Failed to fetch solo dashboard:', dashboardResult.reason)
      error.value = dashboardResult.reason?.message || 'Failed to load dashboard'
    }

    // Handle trend result independently
    if (trendResult.status === 'fulfilled') {
      winrateTrendData.value = trendResult.value?.winrateTrend || null
    } else {
      console.warn('Failed to fetch winrate trend:', trendResult.reason)
      winrateTrendData.value = null
    }

    // Handle matchups result independently
    if (matchupsResult.status === 'fulfilled') {
      matchupsData.value = matchupsResult.value?.matchups || null
    } else {
      console.warn('Failed to fetch champion matchups:', matchupsResult.reason)
      matchupsData.value = null
    }

  isLoading.value = false
}

// Subscribe to sync updates for primary account
onMounted(() => {
  fetchDashboardData()
  
})

  // Fetch when filters change and track filter usage
  watch(queueFilter, (newValue) => {
    trackFilterChange('queue', newValue)
    fetchDashboardData()
  })
  watch(timeRange, (newValue) => {
    trackFilterChange('time', newValue)
    fetchDashboardData()
  })

// Watch for sync completion to refresh data
watch(syncProgress, (progress) => {
  for (const [puuid, data] of progress.entries()) {
    if (data.status === 'completed') {
      // Refresh user data to get updated profile icon/level
      authStore.refreshUser()
      // Refresh dashboard data to get updated stats
      fetchDashboardData()
      // Reset the status after refresh
      resetProgress(puuid)
      break
    }
  }
}, { deep: true })
</script>

<style scoped>
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
.queue-toggle-btn.bg-primary::after {
  display: none;
}

.queue-toggle-btn:has(+ .bg-primary)::after {
  display: none;
}
</style>
