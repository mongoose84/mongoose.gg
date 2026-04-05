<template>
  <OverviewLayout
    :is-loading="pageIsLoading"
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
      <!-- Overall Mode: Show account cards -->
      <OverviewAccountCards
        v-if="authStore.isOverallMode && displayedAccounts.length > 0"
        :accounts="displayedAccounts"
        :linked-accounts="authStore.riotAccounts"
        :active-account-puuid="authStore.activeAccountPuuid"
        @select="handleAccountSelect"
      />
      
      <!-- Individual Mode: Show player header -->
      <OverviewPlayerHeader
        v-else-if="overviewData?.playerHeader"
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
        :primary-queue-label="rankSnapshotLabel"
        :rank="overviewData.rankSnapshot.rank"
        :lp="overviewData.rankSnapshot.lp"
        :last20-wins="overviewData.rankSnapshot.last20Wins"
        :last20-losses="overviewData.rankSnapshot.last20Losses"
        :wl-last20="overviewData.rankSnapshot.wlLast20"
      />
    </template>

    <!-- Today at a glance: Right - Champion Select CTA -->
    <template #glance-right>
      <div class="glance-right-fill">
        <ChampionSelectCTA
          :mural-url="championSelectMuralUrl"
          :champion-name="mostPlayedChampionName"
        />
      </div>
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
      <div class="recent-right-stack">
        <AnalysisStatusCard />
        <SoloAnalyticsCTA
          :subtitle="soloCtaSubtitle"
          :trend-direction="soloCtaTrendDirection"
        />
      </div>
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
        :account-name="lastMatchAccountName"
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
import { useAsyncData } from '../composables/useAsyncData'
import { getOverview, getMatchActivity, getSoloDashboard } from '../services/soloApi'
import { getChampionSplashUrl } from '../utils/leagueAssets'
import OverviewLayout from '../components/overview/OverviewLayout.vue'
import OverviewAccountCards from '../components/overview/OverviewAccountCards.vue'
import OverviewPlayerHeader from '../components/overview/OverviewPlayerHeader.vue'
import MatchActivityHeatmap from '../components/overview/MatchActivityHeatmap.vue'
import RankSnapshot from '../components/overview/RankSnapshot.vue'
import LastMatchCard from '../components/overview/LastMatchCard.vue'
import ChampionSelectCTA from '../components/overview/ChampionSelectCTA.vue'
import AnalysisStatusCard from '../components/overview/AnalysisStatusCard.vue'
import SoloAnalyticsCTA from '../components/overview/SoloAnalyticsCTA.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()

// State
const overviewData = ref(null)
const matchActivityData = ref(null)
const soloDashboardData = ref(null)
const isRefreshing = ref(false)
const showLinkModal = ref(false)
const previousSyncStatuses = ref(new Map())

const {
  error,
  isLoading,
  execute: executeOverviewFetch
} = useAsyncData(async () => {
  const [overview, activity, soloDashboard] = await Promise.all([
    getOverview(authStore.userId),
    getMatchActivity(authStore.userId),
    getSoloDashboard(authStore.userId)
  ])

  return {
    overview,
    activity,
    soloDashboard
  }
}, { immediate: false, errorMessage: 'Failed to load overview' })

const pageIsLoading = computed(() => isLoading.value && !overviewData.value)

const mostPlayedChampionName = computed(() => {
  return overviewData.value?.mostPlayedChampion?.championName || ''
})

const championSelectMuralUrl = computed(() => {
  return mostPlayedChampionName.value
    ? getChampionSplashUrl(mostPlayedChampionName.value)
    : ''
})

const displayedAccounts = computed(() => {
  const accounts = overviewData.value?.accountSummaries || []
  // Limit to 3 accounts for aesthetic reasons - maintains clean visual layout in header
  return accounts.slice(0, 3)
})

const rankSnapshotLabel = computed(() => {
  if (authStore.isOverallMode) {
    const originalLabel = overviewData.value?.rankSnapshot?.primaryQueueLabel || ''
    // Extract queue type from label (e.g., "Ranked Solo/Duo" -> "Solo", "Ranked Flex" -> "Flex")
    let queueType = ''
    if (originalLabel.includes('Solo')) {
      queueType = ' (Solo)'
    } else if (originalLabel.includes('Flex')) {
      queueType = ' (Flex)'
    } else if (originalLabel.includes('ARAM')) {
      queueType = ' (ARAM)'
    } else if (originalLabel.includes('Normal')) {
      queueType = ' (Normal)'
    }
    return `Highest Rank${queueType}`
  }
  return overviewData.value?.rankSnapshot?.primaryQueueLabel || ''
})

const lastMatchAccountName = computed(() => {
  // Backend does not yet include which account played the last match in Overall mode.
  // Return null to avoid showing a potentially incorrect account tag.
  if (authStore.isOverallMode) {
    return null
  }
  return null
})

const soloCtaSubtitle = computed(() => {
  const avgKda = soloDashboardData.value?.avgKda
  const overallAvgKda = soloDashboardData.value?.overallAvgKda

  if (typeof avgKda !== 'number' || typeof overallAvgKda !== 'number') {
    return 'Track your trends and improve'
  }

  const diff = avgKda - overallAvgKda
  if (Math.abs(diff) < 0.05) {
    return `KDA trend: ${avgKda.toFixed(1)} (even vs overall)`
  }

  const sign = diff > 0 ? '+' : ''
  return `KDA trend: ${avgKda.toFixed(1)} (${sign}${diff.toFixed(1)} vs overall)`
})

const soloCtaTrendDirection = computed(() => {
  const avgKda = soloDashboardData.value?.avgKda
  const overallAvgKda = soloDashboardData.value?.overallAvgKda

  if (typeof avgKda !== 'number' || typeof overallAvgKda !== 'number') {
    return 'neutral'
  }

  const diff = avgKda - overallAvgKda
  if (Math.abs(diff) < 0.05) {
    return 'neutral'
  }

  return diff > 0 ? 'up' : 'down'
})

async function fetchData() {
  if (!authStore.userId) return

  const isInitialLoad = !overviewData.value

  if (!isInitialLoad) {
    isRefreshing.value = true
  }

  try {
    const result = await executeOverviewFetch()
    overviewData.value = result.overview
    matchActivityData.value = result.activity
    soloDashboardData.value = result.soloDashboard
  } catch {
    overviewData.value = null
    matchActivityData.value = null
    soloDashboardData.value = null
  } finally {
    if (!isInitialLoad) {
      isRefreshing.value = false
    }
  }
}

// Watch for sync completion to refresh data
watch(syncProgress, (progress) => {
  for (const [puuid, data] of progress.entries()) {
    const previousStatus = previousSyncStatuses.value.get(puuid)
    const currentStatus = data.status

    if (previousStatus === 'syncing' && currentStatus === 'completed') {
      const totalSynced = typeof data.totalSynced === 'number' ? data.totalSynced : 0
      const shouldRefreshOverview = totalSynced > 0

      if (shouldRefreshOverview) {
        // Refresh user data to get updated profile info
        authStore.refreshUser()
        // Refresh overview data to get updated stats
        fetchData()
      }

      // Reset the status after refresh to avoid repeated refreshes
      resetProgress(puuid)
      previousSyncStatuses.value.set(puuid, null)
      break
    }

    previousSyncStatuses.value.set(puuid, currentStatus)
  }
}, { deep: true })

// Handle successful account link
async function handleLinkSuccess() {
  // Refresh user data to get updated riot accounts list
  await authStore.refreshUser()
  // Refresh overview data
  fetchData()
}

function handleAccountSelect(accountId) {
  // Switch to the selected account
  authStore.setActiveAccount(accountId)
  // Data will refresh automatically via watcher
}

onMounted(() => {
  fetchData()
})

watch(() => authStore.activeAccountPuuid, () => {
  fetchData()
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

.recent-right-stack {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  height: 100%;
}

.recent-right-stack :deep(.analysis-status-card),
.recent-right-stack :deep(.solo-analytics-cta) {
  flex: 1;
  height: 100%;
}

.glance-right-fill {
  display: flex;
  height: 100%;
}

.glance-right-fill :deep(.champion-select-cta) {
  flex: 1;
  height: 100%;
}
</style>

