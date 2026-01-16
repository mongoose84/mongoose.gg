<template>
  <section class="solo-dashboard">
    <header class="dashboard-header">
      <h1 class="visually-hidden">Solo Dashboard</h1>

      <!-- Queue Toggle Bar -->
      <div class="queue-toggle" role="group" aria-label="Filter by queue type">
        <button
          v-for="queue in queueOptions"
          :key="queue.value"
          type="button"
          :class="['queue-toggle-btn', { active: queueFilter === queue.value }]"
          @click="queueFilter = queue.value"
          :aria-pressed="queueFilter === queue.value"
        >
          {{ queue.label }}
        </button>
      </div>

      <!-- Time Range Filter -->
      <div class="filter-group">
        <select id="time-range-filter" v-model="timeRange" aria-label="Filter matches by time range">
          <option value="current_season">Current Season</option>
          <option value="1w">Last Week</option>
          <option value="1m">Last Month</option>
          <option value="3m">Last 3 Months</option>
          <option value="6m">Last 6 Months</option>
          <option value="all">All Time</option>
        </select>
      </div>
    </header>

	<div class="sections">
	  	<div class="top-row">
	  	  <div class="top-row-item">
	  	    <!-- Profile Header Card -->
	  	    <ProfileHeaderCard
	  	      v-if="primaryAccount"
	  	      :game-name="primaryAccount.gameName"
	  	      :tag-line="primaryAccount.tagLine"
	  	      :region="primaryAccount.region"
	  	      :profile-icon-id="primaryAccount.profileIconId"
	  	      :summoner-level="primaryAccount.summonerLevel"
	  	      :solo-tier="primaryAccount.soloTier"
	  	      :solo-rank="primaryAccount.soloRank"
	  	      :solo-lp="primaryAccount.soloLp"
	  	      :flex-tier="primaryAccount.flexTier"
	  	      :flex-rank="primaryAccount.flexRank"
	  	      :flex-lp="primaryAccount.flexLp"
	  	      :win-rate="dashboardData?.winRate"
	  	      :games-played="dashboardData?.gamesPlayed"
	  	    />
	  	    <div v-else class="section placeholder-card">
	  	      <h2>Profile Header</h2>
	  	      <p>No linked Riot account found.</p>
	  	    </div>
	  	  </div>
	  	  <div class="top-row-item">
	  	    <MainChampionCard
	  	      v-if="dashboardData?.mainChampions && dashboardData.mainChampions.length"
	  	      :main-champions="dashboardData.mainChampions"
	  	    />
	  	    <div v-else class="section placeholder-card">
	  	      <h2>Main Champions</h2>
	  	      <p>No champion data yet for this filter.</p>
	  	    </div>
	  	  </div>
	  	</div>

	  	<!-- Second row: Stats charts + Champion Matchups -->
	  	<div class="top-row">
	  	  <div class="top-row-item">
	  	    <div class="stacked-cards">
	  	      <WinrateChart
	  	        v-if="winrateTrendData && winrateTrendData.length > 0"
	  	        :winrate-trend="winrateTrendData"
	  	      />
	  	      <div v-else class="section placeholder-card">
	  	        <h2>Winrate Over Time</h2>
	  	        <p>No data for selected time range.</p>
	  	      </div>
	  	      <LpTrendChart
	  	        v-if="lpTrendData && lpTrendData.length > 0"
	  	        :lp-trend="lpTrendData"
	  	      />
	  	      <div v-else-if="isRankedQueue" class="section placeholder-card">
	  	        <h2>LP Over Time</h2>
	  	        <p>No LP data available yet. LP tracking starts after your next ranked game.</p>
	  	      </div>
	  	    </div>
	  	  </div>
	  	  <div class="top-row-item">
	  	    <div class="section placeholder-card matchups-card">
	  	      <h2>Champion Matchups</h2>
	  	      <p>Top 5 champions with expandable opponent details.</p>
	  	    </div>
	  	  </div>
	  	</div>

	  	<div class="section placeholder-card">
	  	  <h2>Goals Panel</h2>
	  	  <p>Active goals and progress (upgrade CTA for Free).</p>
	  	</div>
	  </div>
  </section>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getSoloDashboard, getWinrateTrend } from '../services/authApi'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import ProfileHeaderCard from '../components/ProfileHeaderCard.vue'
import MainChampionCard from '../components/MainChampionCard.vue'
import WinrateChart from '../components/WinrateChart.vue'
import LpTrendChart from '../components/LpTrendChart.vue'

const authStore = useAuthStore()
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket()

// Get the primary Riot account for the profile header
const primaryAccount = computed(() => authStore.primaryRiotAccount)

// Dashboard data from API
const dashboardData = ref(null)
const winrateTrendData = ref(null)
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

  // Fetch dashboard data and winrate trend in parallel (isolated failures)
  async function fetchDashboardData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

    // Fetch in parallel but handle errors independently
    const [dashboardResult, trendResult] = await Promise.allSettled([
      getSoloDashboard(authStore.userId, queueFilter.value, timeRange.value),
      getWinrateTrend(authStore.userId, queueFilter.value, timeRange.value)
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

  isLoading.value = false
}

// Subscribe to sync updates for primary account
onMounted(() => {
  fetchDashboardData()
  if (primaryAccount.value?.puuid) {
    subscribe(primaryAccount.value.puuid)
  }
})

  // Fetch when filters change
  watch(queueFilter, fetchDashboardData)
  watch(timeRange, fetchDashboardData)

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
.solo-dashboard {
  padding: var(--spacing-lg);
}
.dashboard-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-lg);
}

/* Queue Toggle Bar */
.queue-toggle {
  display: flex;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--color-surface);
}

.queue-toggle-btn {
  padding: var(--spacing-sm) var(--spacing-md);
  background: transparent;
  border: none;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.queue-toggle-btn:not(:last-child)::after {
  content: '';
  position: absolute;
  right: 0;
  top: 25%;
  height: 50%;
  width: 1px;
  background: var(--color-border);
}

.queue-toggle-btn:hover:not(.active) {
  color: var(--color-text);
  background: var(--color-elevated);
}

.queue-toggle-btn.active {
  background: var(--color-primary);
  color: white;
}

.queue-toggle-btn.active::after {
  display: none;
}

.queue-toggle-btn:has(+ .active)::after {
  display: none;
}

.queue-toggle-btn:focus {
  outline: none;
  box-shadow: inset 0 0 0 2px var(--color-primary-soft);
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}
.filter-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}
.filter-group select {
  padding: var(--spacing-sm) var(--spacing-md);
  background-color: #020617;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: border-color 0.2s ease;
}
.filter-group select:hover {
  border-color: var(--color-primary);
}
.filter-group select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(147, 51, 234, 0.1);
}
.sections {
	  display: flex;
	  flex-direction: column;
	  gap: var(--spacing-lg);
	}

	.top-row {
	  display: grid;
	  grid-template-columns: minmax(0, 1fr) minmax(0, 3fr);
	  gap: var(--spacing-lg);
	  align-items: stretch;
	}

	.top-row-item {
	  min-width: 0;
	}

	.stacked-cards {
	  display: flex;
	  flex-direction: column;
	  gap: var(--spacing-lg);
	  height: 100%;
	}

	.stacked-cards .placeholder-card {
	  flex: 1;
	}

	.matchups-card {
	  height: 100%;
	  box-sizing: border-box;
	}

	@media (max-width: 1024px) {
	  .top-row {
	    grid-template-columns: 1fr;
	  }
	}

.placeholder-card {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
}
.placeholder-card h2 {
  margin: 0 0 var(--spacing-sm) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}
.placeholder-card p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
</style>
