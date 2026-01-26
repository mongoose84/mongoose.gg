<template>
  <OverviewLayout
    :is-loading="isLoading"
    :error="error"
    :is-empty="!overviewData"
    @retry="fetchOverviewData"
  >
    <!-- Empty State Action -->
    <template #empty-action>
      <button class="btn-link-account" @click="showLinkModal = true">
        Link Riot Account
      </button>
    </template>

    <!-- Player Header (G14b) -->
    <OverviewPlayerHeader
      v-if="overviewData?.playerHeader"
      :summoner-name="overviewData.playerHeader.summonerName"
      :level="overviewData.playerHeader.level"
      :region="overviewData.playerHeader.region"
      :profile-icon-url="overviewData.playerHeader.profileIconUrl"
      :active-contexts="overviewData.playerHeader.activeContexts"
      :sync-status="currentSyncStatus"
      :sync-progress="currentSyncProgress"
      :sync-total="currentSyncTotal"
      :last-sync-at="authStore.primaryRiotAccount?.lastSyncAt"
    />

    <!-- Rank Snapshot (G14c) -->
    <RankSnapshot
      v-if="overviewData?.rankSnapshot"
      :primary-queue-label="overviewData.rankSnapshot.primaryQueueLabel"
      :rank="overviewData.rankSnapshot.rank"
      :lp="overviewData.rankSnapshot.lp"
      :lp-delta-last20="overviewData.rankSnapshot.lpDeltaLast20"
      :last20-wins="overviewData.rankSnapshot.last20Wins"
      :last20-losses="overviewData.rankSnapshot.last20Losses"
      :wl-last20="overviewData.rankSnapshot.wlLast20"
    />

    <!-- Last Match Card (G14d) -->
    <LastMatchCard
      v-if="overviewData?.lastMatch"
      :match-id="overviewData.lastMatch.matchId"
      :champion-icon-url="overviewData.lastMatch.championIconUrl"
      :champion-name="overviewData.lastMatch.championName"
      :result="overviewData.lastMatch.result"
      :kda="overviewData.lastMatch.kda"
      :timestamp="overviewData.lastMatch.timestamp"
      :queue-type="overviewData.lastMatch.queueType"
    />

    <!-- Placeholder for GoalProgressPreview (G14e) -->
    <div v-if="overviewData?.activeGoals && overviewData.activeGoals.length > 0" class="placeholder-card">
      <h3 class="text-lg font-semibold text-text mb-xs">Active Goals</h3>
      <p class="text-text-secondary text-sm">{{ overviewData.activeGoals.length }} goal(s) in progress</p>
    </div>

    <!-- Placeholder for SuggestedActions (G14f) -->
    <div v-if="overviewData?.suggestedActions && overviewData.suggestedActions.length > 0" class="placeholder-card">
      <h3 class="text-lg font-semibold text-text mb-xs">Suggested Actions</h3>
      <p class="text-text-secondary text-sm">{{ overviewData.suggestedActions.length }} action(s) available</p>
    </div>
  </OverviewLayout>

  <!-- Link Riot Account Modal -->
  <LinkRiotAccountModal
    :is-open="showLinkModal"
    @close="showLinkModal = false"
    @success="handleLinkSuccess"
  />
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { getOverview } from '../services/authApi'
import OverviewLayout from '../components/overview/OverviewLayout.vue'
import OverviewPlayerHeader from '../components/overview/OverviewPlayerHeader.vue'
import RankSnapshot from '../components/overview/RankSnapshot.vue'
import LastMatchCard from '../components/overview/LastMatchCard.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

const authStore = useAuthStore()
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket()

// State
const overviewData = ref(null)
const isLoading = ref(false)
const error = ref(null)
const showLinkModal = ref(false)

// Get primary account PUUID for sync status tracking
const primaryPuuid = computed(() => {
  return authStore.primaryRiotAccount?.puuid || null
})

// Sync status computed from WebSocket progress, falls back to stored status
const currentSyncStatus = computed(() => {
  if (!primaryPuuid.value) return null
  // First check WebSocket for real-time updates
  const progress = syncProgress.get(primaryPuuid.value)
  if (progress?.status) return progress.status
  // Fall back to stored status from account
  return authStore.primaryRiotAccount?.syncStatus || null
})

const currentSyncProgress = computed(() => {
  if (!primaryPuuid.value) return null
  // First check WebSocket for real-time updates
  const progress = syncProgress.get(primaryPuuid.value)
  if (progress?.progress != null) return progress.progress
  // Fall back to stored progress from account (for page refresh during sync)
  return authStore.primaryRiotAccount?.syncProgress ?? null
})

const currentSyncTotal = computed(() => {
  if (!primaryPuuid.value) return null
  // First check WebSocket for real-time updates
  const progress = syncProgress.get(primaryPuuid.value)
  if (progress?.total != null) return progress.total
  // Fall back to stored total from account (for page refresh during sync)
  return authStore.primaryRiotAccount?.syncTotal ?? null
})

async function fetchOverviewData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  try {
    overviewData.value = await getOverview(authStore.userId)
  } catch (e) {
    console.error('Failed to fetch overview:', e)
    error.value = e.message || 'Failed to load overview'
  } finally {
    isLoading.value = false
  }
}

// Watch for sync completion to refresh data
watch(syncProgress, (progress) => {
  for (const [puuid, data] of progress.entries()) {
    if (data.status === 'completed') {
      // Refresh user data to get updated profile info
      authStore.refreshUser()
      // Refresh overview data to get updated stats
      fetchOverviewData()
      // Reset the status after refresh to avoid repeated refreshes
      resetProgress(puuid)
      break
    }
  }
}, { deep: true })

// Handle successful account link
async function handleLinkSuccess() {
  // Refresh user data to get updated riot accounts list
  await authStore.refreshUser()
  // Subscribe to sync updates for the newly linked account
  if (primaryPuuid.value) {
    subscribe(primaryPuuid.value)
  }
  // Refresh overview data
  fetchOverviewData()
}

onMounted(() => {
  fetchOverviewData()
  // Subscribe to sync updates for primary account
  if (primaryPuuid.value) {
    subscribe(primaryPuuid.value)
  }
})
</script>

<style scoped>
.placeholder-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  backdrop-filter: blur(10px);
}

.btn-link-account {
  margin-top: var(--spacing-md);
  background: var(--color-primary);
  color: white;
  padding: var(--spacing-sm) var(--spacing-xl);
  border: none;
  border-radius: var(--radius-md);
  font-weight: 600;
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-link-account:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}
</style>

