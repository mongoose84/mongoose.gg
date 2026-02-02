<template>
  <OverviewLayout
    :is-loading="isLoading"
    :error="error"
    :is-empty="!overviewData"
    @retry="fetchData"
  >
    <!-- Empty State Action -->
    <template #empty-action>
      <button class="btn-link-account" @click="showLinkModal = true">
        Link Riot Account
      </button>
    </template>

    <!-- Header: Player Header (full width) -->
    <template #header>
      <OverviewPlayerHeader
        v-if="overviewData?.playerHeader"
        :summoner-name="overviewData.playerHeader.summonerName"
        :level="overviewData.playerHeader.level"
        :region="overviewData.playerHeader.region"
        :profile-icon-url="overviewData.playerHeader.profileIconUrl"
        :active-contexts="overviewData.playerHeader.activeContexts"
      />
    </template>

    <!-- Today at a glance: Left - Rank Snapshot -->
    <template #glance-left>
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
    </template>

    <!-- Today at a glance: Right - Champion Select CTA -->
    <template #glance-right>
      <ChampionSelectCTA />
    </template>

    <!-- Recent games: Left - Match Activity Heatmap -->
    <template #recent-left>
      <MatchActivityHeatmap
        v-if="matchActivityData"
        :daily-match-counts="matchActivityData.dailyMatchCounts"
        :start-date="matchActivityData.startDate"
        :end-date="matchActivityData.endDate"
        :total-matches="matchActivityData.totalMatches"
      />
    </template>

    <!-- Recent games: Right - Analysis Status Card -->
    <template #recent-right>
      <AnalysisStatusCard />
    </template>

    <!-- Latest match (full width) -->
    <template #latest-match>
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
    </template>
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
import { getOverview, getMatchActivity } from '../services/authApi'
import OverviewLayout from '../components/overview/OverviewLayout.vue'
import OverviewPlayerHeader from '../components/overview/OverviewPlayerHeader.vue'
import MatchActivityHeatmap from '../components/overview/MatchActivityHeatmap.vue'
import RankSnapshot from '../components/overview/RankSnapshot.vue'
import LastMatchCard from '../components/overview/LastMatchCard.vue'
import ChampionSelectCTA from '../components/overview/ChampionSelectCTA.vue'
import AnalysisStatusCard from '../components/overview/AnalysisStatusCard.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

const authStore = useAuthStore()
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket()

// State
const overviewData = ref(null)
const matchActivityData = ref(null)
const isLoading = ref(false)
const error = ref(null)
const showLinkModal = ref(false)

// Get primary account PUUID for sync status tracking
const primaryPuuid = computed(() => {
  return authStore.primaryRiotAccount?.puuid || null
})

async function fetchData() {
  if (!authStore.userId) return

  isLoading.value = true
  error.value = null

  try {
    // Fetch overview and match activity data in parallel
    const [overview, activity] = await Promise.all([
      getOverview(authStore.userId),
      getMatchActivity(authStore.userId)
    ])
    overviewData.value = overview
    matchActivityData.value = activity
  } catch (e) {
    console.error('Failed to fetch overview data:', e)
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
      fetchData()
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
  fetchData()
}

onMounted(() => {
  fetchData()
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

