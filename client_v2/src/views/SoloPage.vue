<template>
  <div class="solo-page">
    <div class="solo-container">
      <!-- Header with back link -->
      <div class="solo-header">
        <router-link to="/app/user" class="back-link">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="back-icon">
            <path fill-rule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clip-rule="evenodd" />
          </svg>
          Back to Dashboard
        </router-link>
        <h1 class="page-title">Solo Performance</h1>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="loading-state">
        <div class="loading-spinner"></div>
        <p>Loading your stats...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="error-state">
        <p class="error-message">{{ error }}</p>
        <button class="btn-retry" @click="loadData">Try Again</button>
      </div>

      <!-- No Account State -->
      <div v-else-if="!selectedAccount" class="no-account-state">
        <p>No Riot account selected. Please link an account first.</p>
        <router-link to="/app/user" class="btn-primary">Link Account</router-link>
      </div>

      <!-- Main Content -->
      <template v-else>
        <!-- Filters Bar -->
        <div class="filters-bar">
          <QueueFilter v-model="queueFilter" />
          <TimePeriodFilter v-model="timePeriod" />
        </div>

        <!-- Player Profile Card -->
        <PlayerProfileCard
          :account="selectedAccount"
          :stats="dashboardData"
          :loading="loading"
        />

        <!-- Stats Grid -->
        <div class="stats-grid">
          <!-- Stats Overview -->
          <StatsOverview
            :stats="dashboardData"
            :trends="trendsData"
            :loading="loading"
          />

          <!-- Role Breakdown -->
          <RoleBreakdown
            :roles="roleData"
            :loading="loading"
          />
        </div>

        <!-- Champion Matchups (hidden until v2 endpoint available) -->
        <ChampionMatchups
          v-if="matchupsData"
          :matchups="matchupsData"
          :loading="matchupsLoading"
        />
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getSoloDashboard } from '../services/soloApi'
import QueueFilter from '../components/solo/QueueFilter.vue'
import TimePeriodFilter from '../components/solo/TimePeriodFilter.vue'
import PlayerProfileCard from '../components/solo/PlayerProfileCard.vue'
import StatsOverview from '../components/solo/StatsOverview.vue'
import RoleBreakdown from '../components/solo/RoleBreakdown.vue'
import ChampionMatchups from '../components/solo/ChampionMatchups.vue'

const authStore = useAuthStore()

// State
const loading = ref(false)
const error = ref(null)
const matchupsLoading = ref(false)

// Filters
const queueFilter = ref('all_ranked')
const timePeriod = ref('month')

// Data
const dashboardData = ref(null)
const matchupsData = ref(null)
const trendsData = ref(null)
const roleData = ref(null)

// Computed
const selectedAccount = computed(() => {
  // Get first linked account (primary in future)
  return authStore.riotAccounts?.[0] || null
})

// Load all data
async function loadData() {
  if (!selectedAccount.value || !authStore.userId) return

  loading.value = true
  error.value = null

  try {
    // Load dashboard data (includes roleBreakdown)
    const dashboard = await getSoloDashboard(authStore.userId, {
      queueFilter: queueFilter.value,
      timePeriod: timePeriod.value
    })

    dashboardData.value = dashboard
    // Extract roleBreakdown from dashboard response
    roleData.value = dashboard.roleBreakdown || []

    // Set trends from last10/last20 games in dashboard
    trendsData.value = dashboard.last20Games || dashboard.last10Games || null

    // TODO: Champion matchups v2 endpoint not yet available
    matchupsData.value = null
  } catch (e) {
    console.error('Failed to load solo dashboard:', e)
    error.value = e.message || 'Failed to load dashboard data'
  } finally {
    loading.value = false
  }
}

// Note: Champion matchups v2 endpoint not yet available
// These will be enabled when the backend endpoints are created

// Watch for filter changes
watch([queueFilter, timePeriod], () => {
  loadData()
})

// Initial load
onMounted(() => {
  loadData()
})
</script>

<style scoped>
.solo-page {
  min-height: calc(100vh - 64px);
  padding: var(--spacing-xl);
}

.solo-container {
  max-width: 1200px;
  margin: 0 auto;
}

.solo-header {
  margin-bottom: var(--spacing-xl);
}

.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  text-decoration: none;
  margin-bottom: var(--spacing-sm);
  transition: color 0.2s;
}

.back-link:hover {
  color: var(--color-primary);
}

.back-icon {
  width: 16px;
  height: 16px;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.filters-bar {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xl);
  flex-wrap: wrap;
}

.stats-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-xl);
  margin-bottom: var(--spacing-xl);
}

@media (max-width: 900px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-3xl);
  color: var(--color-text-secondary);
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: var(--spacing-md);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error State */
.error-state {
  text-align: center;
  padding: var(--spacing-3xl);
}

.error-message {
  color: var(--color-error);
  margin-bottom: var(--spacing-md);
}

.btn-retry {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  color: var(--color-text);
  padding: var(--spacing-sm) var(--spacing-lg);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-retry:hover {
  border-color: var(--color-primary);
}

/* No Account State */
.no-account-state {
  text-align: center;
  padding: var(--spacing-3xl);
  color: var(--color-text-secondary);
}

.no-account-state p {
  margin-bottom: var(--spacing-lg);
}

.btn-primary {
  display: inline-block;
  background: var(--color-primary);
  color: white;
  padding: var(--spacing-sm) var(--spacing-lg);
  border-radius: var(--radius-md);
  text-decoration: none;
  font-weight: var(--font-weight-semibold);
  transition: all 0.2s;
}

.btn-primary:hover {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}
</style>

