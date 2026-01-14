<template>
  <section class="solo-dashboard">
    <header class="dashboard-header">
      <h1>Solo Dashboard</h1>
      <div class="filters">
        <div class="filter-group">
          <label for="queue-filter">Queue</label>
          <select id="queue-filter" v-model="queueFilter" aria-label="Filter matches by queue type">
            <option value="all">All Queues</option>
            <option value="ranked_solo">Ranked Solo/Duo</option>
            <option value="ranked_flex">Ranked Flex</option>
            <option value="normal">Normal</option>
            <option value="aram">ARAM</option>
          </select>
        </div>
        <div class="filter-group">
          <label for="time-range-filter">Time Range</label>
          <select id="time-range-filter" v-model="timeRange" aria-label="Filter matches by time range">
            <option value="1w">Last Week</option>
            <option value="1m">Last Month</option>
            <option value="3m">Last 3 Months</option>
            <option value="6m">Last 6 Months</option>
          </select>
        </div>
      </div>
    </header>

    <div class="sections">
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

      <div class="section placeholder-card">
        <h2>Main Champion Card</h2>
        <p>Role tabs with top 3 champions per role.</p>
      </div>

      <div class="section placeholder-card">
        <h2>Winrate Over Time</h2>
        <p>Rolling average line chart (shared component).</p>
      </div>

      <div class="section placeholder-card">
        <h2>LP Over Time</h2>
        <p>Per-game LP changes with rank annotations (ranked only).</p>
      </div>

      <div class="section placeholder-card">
        <h2>Champion Matchups</h2>
        <p>Top 5 champions with expandable opponent details.</p>
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
import { getSoloDashboard } from '../services/authApi'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import ProfileHeaderCard from '../components/ProfileHeaderCard.vue'

const authStore = useAuthStore()
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket()

// Get the primary Riot account for the profile header
const primaryAccount = computed(() => authStore.primaryRiotAccount)

// Dashboard data from API
const dashboardData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// UI state for filters
const queueFilter = ref('all')
const timeRange = ref('3m')

// Fetch dashboard data
async function fetchDashboardData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  try {
    dashboardData.value = await getSoloDashboard(authStore.userId, queueFilter.value)
  } catch (err) {
    console.error('Failed to fetch solo dashboard:', err)
    error.value = err.message
  } finally {
    isLoading.value = false
  }
}

// Subscribe to sync updates for primary account
onMounted(() => {
  fetchDashboardData()
  if (primaryAccount.value?.puuid) {
    subscribe(primaryAccount.value.puuid)
  }
})

// Fetch when queue filter changes
watch(queueFilter, fetchDashboardData)

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
.filters {
  display: flex;
  gap: var(--spacing-md);
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
  background: var(--color-surface);
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
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--spacing-lg);
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
