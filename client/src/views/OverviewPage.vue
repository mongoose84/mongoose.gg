<template>
  <div class="overview-page" data-testid="overview-page">
    <SyncProgress
      v-if="syncState"
      :state="syncState"
      :current="progressCurrent"
      :total="progressTotal"
      :synced-count="syncedCount"
      @retry="startSync"
    />

    <!-- No linked Riot account -->
    <BaseEmptyState
      v-if="hasNoLinkedAccount"
      :heading-level="1"
      title="Link your Riot account to see your Overview"
      description="We sync your recent matches and your Overview fills in within a few minutes."
      data-testid="overview-empty"
    >
      <template #action>
        <BaseButton size="lg" data-testid="overview-link-account" @click="showLinkModal = true">
          <template #icon-left><BaseIcon name="link" :size="20" /></template>
          Link Riot account
        </BaseButton>
      </template>
    </BaseEmptyState>

    <!-- Loading: the page frame with skeletons in the layout of the content -->
    <div
      v-else-if="pageIsLoading"
      class="overview-loading"
      aria-busy="true"
      data-testid="overview-loading"
    >
      <span class="visually-hidden">Loading your Overview</span>
      <div class="mp-hero overview-loading__hero">
        <div class="mp-hero-body">
          <div class="overview-loading__stack">
            <BaseSkeleton width="14rem" />
            <BaseSkeleton variant="title" width="70%" height="2.75rem" />
            <BaseSkeleton width="55%" />
          </div>
          <BaseSkeleton variant="block" width="11rem" height="3rem" />
        </div>
      </div>
      <div class="overview-grid overview-grid--3">
        <BaseSkeleton
          v-for="n in 3"
          :key="n"
          variant="block"
          width="100%"
          height="18.75rem"
          class="overview-loading__champion"
        />
      </div>
      <div class="mp-card overview-loading__stack">
        <BaseSkeleton variant="title" />
        <div class="overview-loading__row">
          <BaseSkeleton variant="portrait" />
          <div class="overview-loading__stack overview-loading__grow">
            <BaseSkeleton width="30%" />
            <BaseSkeleton width="50%" />
          </div>
        </div>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error || !overviewData" class="mp-card" data-testid="overview-error">
      <div class="mp-message mp-message--error" role="alert">
        <BaseIcon name="triangle-alert" :size="20" />
        <div class="mp-message__body">
          <p>We couldn't load your Overview. Try again in a minute.</p>
          <button
            type="button"
            class="mp-message__action"
            data-testid="overview-retry"
            @click="fetchData"
          >Try again</button>
        </div>
      </div>
    </div>

    <template v-else>
      <!-- Overall mode: one card per account in place of the hero -->
      <OverviewAccountCards
        v-if="authStore.isOverallMode && displayedAccounts.length > 0"
        :accounts="displayedAccounts"
        :linked-accounts="authStore.riotAccounts"
        :active-account-puuid="authStore.activeAccountPuuid"
      />

      <ChampionHero
        v-else
        :headline="heroHeadline"
        :text="heroText"
        :player-line="playerLine"
        :champion-name="mostPlayedChampionName || null"
        :chips="heroChips"
      >
        <template #action>
          <BaseButton to="/app/matches" size="lg" data-testid="hero-matches-link">
            See your matches
          </BaseButton>
        </template>
      </ChampionHero>

      <!-- Your champions (score rings go between the hero and this section once they exist) -->
      <OverviewChampionPool
        v-reveal-on-view
        :champions="championPool.champions"
        :also-played="championPool.alsoPlayed"
      />

      <!-- Today's matches -->
      <section
        v-reveal-on-view
        class="mp-card overview-card"
        aria-labelledby="overview-today-title"
        data-testid="today-matches-card"
      >
        <header class="overview-card__header">
          <div>
            <h2 id="overview-today-title" class="mp-card-title">Today's matches</h2>
            <p v-if="lastSyncedLabel" class="overview-card__meta" data-testid="today-matches-synced">{{ lastSyncedLabel }}</p>
          </div>
          <BaseButton
            variant="secondary"
            size="sm"
            :disabled="isSyncing"
            data-testid="overview-sync-button"
            @click="startSync"
          >
            <template #icon-left><BaseIcon name="refresh-cw" :size="16" /></template>
            {{ isSyncing ? 'Syncing…' : 'Sync matches' }}
          </BaseButton>
        </header>

        <template v-if="lastMatch">
          <p class="overview-card__summary" data-testid="today-matches-summary">{{ todaySummary }}</p>
          <BaseMatchRow
            :to="{ name: 'app-matches', params: { matchId: lastMatch.matchId } }"
            :champion-name="lastMatch.championName"
            :champion-icon-url="lastMatch.championIconUrl"
            :win="isWinResult(lastMatch.result)"
            :kda="lastMatch.kda"
            :queue="lastMatch.queueType"
            :timestamp="lastMatch.timestamp"
          />
          <div class="overview-card__footer">
            <BaseButton to="/app/matches" variant="ghost" size="sm" data-testid="today-matches-all">
              All matches
              <template #icon-right><BaseIcon name="arrow-right" :size="16" /></template>
            </BaseButton>
          </div>
        </template>

        <BaseEmptyState
          v-else
          title="No matches yet"
          description="Sync your matches from Riot and your latest one shows up here."
          data-testid="today-matches-empty"
        />
      </section>

      <!-- Insights -->
      <section
        v-reveal-on-view
        class="overview-section"
        aria-labelledby="overview-insights-title"
        data-testid="overview-insights"
      >
        <h2 id="overview-insights-title" class="mp-card-title">Insights</h2>
        <div v-if="survivalInsight" class="overview-grid overview-grid--3">
          <InsightCard
            :kind="survivalInsight.kind"
            :title="survivalInsight.title"
            :text="survivalInsight.text"
            :champion-name="mostPlayedChampionName || null"
          />
        </div>
        <BaseEmptyState
          v-else
          title="No insights yet"
          description="Insights need a few more matches with a clear pattern. Keep playing and check back after your next session."
          data-testid="overview-insights-empty"
        />
      </section>

      <!-- Next steps -->
      <section
        v-reveal-on-view
        class="overview-section"
        aria-labelledby="overview-next-title"
        data-testid="overview-next-steps"
      >
        <h2 id="overview-next-title" class="mp-card-title">Next steps</h2>
        <div class="overview-grid overview-grid--2">
          <article class="mp-card overview-next" data-testid="next-step-champion-select">
            <h3 class="overview-next__title">Pick your next champion</h3>
            <p class="overview-next__text">Picks from your champion pool against the enemy team, ready before you lock in.</p>
            <BaseButton to="/app/champion-select" variant="secondary" size="lg">Open Champion Select</BaseButton>
          </article>
          <article class="mp-card overview-next" data-testid="next-step-solo">
            <h3 class="overview-next__title">See how you're trending</h3>
            <p class="overview-next__text">Win rate, KDA and more across your recent matches, so you know what to work on.</p>
            <BaseButton to="/app/solo" variant="secondary" size="lg">See your trends</BaseButton>
          </article>
        </div>
      </section>
    </template>
  </div>

  <!-- Link Riot Account Modal -->
  <LinkRiotAccountModal
    :is-open="showLinkModal"
    @close="showLinkModal = false"
    @success="handleLinkSuccess"
  />
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSyncWebSocket } from '../composables/useSyncWebSocket'
import { useAsyncData } from '../composables/useAsyncData'
import { useSyncMatches } from '../composables/useSyncMatches'
import { vRevealOnView } from '../composables/useRevealOnView'
import { getOverview } from '../services/soloApi'
import { formatRelativeTime } from '../utils/formatters'
import {
  buildHeroHeadline,
  buildHeroText,
  buildHeroChips,
  buildPlayerLine,
  buildSurvivalInsight,
  buildTodaySummary,
  isWinResult
} from '../utils/overviewSummary'
import BaseButton from '../components/base/BaseButton.vue'
import BaseIcon from '../components/base/BaseIcon.vue'
import BaseSkeleton from '../components/base/BaseSkeleton.vue'
import BaseEmptyState from '../components/base/BaseEmptyState.vue'
import BaseMatchRow from '../components/base/BaseMatchRow.vue'
import ChampionHero from '../components/base/ChampionHero.vue'
import InsightCard from '../components/base/InsightCard.vue'
import SyncProgress from '../components/base/SyncProgress.vue'
import OverviewAccountCards from '../components/overview/OverviewAccountCards.vue'
import OverviewChampionPool from '../components/overview/OverviewChampionPool.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

const authStore = useAuthStore()
const { syncProgress, resetProgress } = useSyncWebSocket()
const {
  syncState,
  isSyncing,
  progressCurrent,
  progressTotal,
  syncedCount,
  lastSyncAt,
  startSync
} = useSyncMatches()

// State
const overviewData = ref(null)
const showLinkModal = ref(false)
const previousSyncStatuses = ref(new Map())

const {
  error,
  isLoading,
  execute: executeOverviewFetch
} = useAsyncData(
  () => getOverview(authStore.userId),
  { immediate: false, errorMessage: 'Failed to load overview' }
)

// Until the first fetch settles the page shows its skeleton frame
const hasFetched = ref(false)
const pageIsLoading = computed(() => (isLoading.value || !hasFetched.value) && !overviewData.value)
// The API answers with an error when no Riot account is linked, so check the session first
const hasNoLinkedAccount = computed(() => authStore.isInitialized && authStore.hasLinkedAccount === false)

const mostPlayedChampionName = computed(() => overviewData.value?.mostPlayedChampion?.championName || '')
const lastMatch = computed(() => overviewData.value?.lastMatch ?? null)
const sessionStats = computed(() => overviewData.value?.sessionStats ?? null)
const survivalStats = computed(() => overviewData.value?.survivalStats ?? null)
const championPool = computed(() => ({
  champions: overviewData.value?.championPool?.champions ?? [],
  alsoPlayed: overviewData.value?.championPool?.alsoPlayed ?? []
}))

const heroHeadline = computed(() => buildHeroHeadline(sessionStats.value))
const heroText = computed(() => buildHeroText(sessionStats.value, survivalStats.value))
const heroChips = computed(() => buildHeroChips(sessionStats.value))
const playerLine = computed(() => buildPlayerLine(overviewData.value?.playerHeader, mostPlayedChampionName.value))
const survivalInsight = computed(() => buildSurvivalInsight(survivalStats.value))
const todaySummary = computed(() => buildTodaySummary(sessionStats.value))

const lastSyncedLabel = computed(() => {
  if (!lastSyncAt.value) return null
  return `Synced ${formatRelativeTime(new Date(lastSyncAt.value).getTime())}`
})

const displayedAccounts = computed(() => {
  const accounts = overviewData.value?.accountSummaries || []
  // At most three account cards, one row on desktop
  return accounts.slice(0, 3)
})

async function fetchData() {
  if (!authStore.userId) return

  try {
    overviewData.value = await executeOverviewFetch()
  } catch {
    overviewData.value = null
  } finally {
    hasFetched.value = true
  }
}

// Refresh once a sync that brought in new matches completes
watch(syncProgress, (progress) => {
  for (const [puuid, data] of progress.entries()) {
    const previousStatus = previousSyncStatuses.value.get(puuid)
    const currentStatus = data.status

    if (previousStatus === 'syncing' && currentStatus === 'completed') {
      const totalSynced = typeof data.totalSynced === 'number' ? data.totalSynced : 0

      if (totalSynced > 0) {
        authStore.refreshUser()
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

async function handleLinkSuccess() {
  await authStore.refreshUser()
  fetchData()
}

// Defer the initial fetch until auth is fully initialized so that
// validateActiveAccount() has already corrected activeAccountPuuid before
// we hit the API. This prevents a transient 'overall' mode flash where
// OverviewAccountCards appears briefly then disappears when the real mode
// is applied, causing flaky E2E tests on slower CI runners.
watch(() => authStore.isInitialized, (initialized) => {
  if (initialized) fetchData()
}, { immediate: true })

watch(() => authStore.activeAccountPuuid, () => {
  if (authStore.isInitialized) fetchData()
})
</script>

<style scoped>
.overview-page {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  padding: 1.75rem 0 3.5rem;
}

.overview-section {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.overview-grid {
  display: grid;
  gap: 1.25rem;
}

.overview-grid--3 {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.overview-grid--2 {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.overview-card {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.overview-card__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.overview-card__meta {
  margin-top: 0.25rem;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.overview-card__summary {
  font-size: 0.9375rem;
  line-height: 1.55;
  font-variant-numeric: tabular-nums;
  color: var(--color-ink-soft);
}

.overview-card__footer {
  display: flex;
  justify-content: flex-end;
}

.overview-next {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.75rem;
}

.overview-next__title {
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.3;
  color: var(--color-text);
}

.overview-next__text {
  max-width: 52ch;
  margin-bottom: 0.5rem;
  font-size: 0.9375rem;
  line-height: 1.55;
  color: var(--color-ink-soft);
}

/* Skeletons wait 300ms before showing, so fast loads never flash them */
.overview-loading {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  animation: overview-loading-appear 0s linear 300ms both;
}

.overview-loading__stack {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.overview-loading__row {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.overview-loading__grow {
  flex: 1;
}

.overview-loading__champion {
  border-radius: 1.5rem;
}

@keyframes overview-loading-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

@media (max-width: 899px) {
  .overview-grid--3,
  .overview-grid--2 {
    grid-template-columns: minmax(0, 1fr);
  }

  .overview-page {
    padding: 1rem 0 1.75rem;
  }

  .overview-loading__hero .mp-hero-body {
    padding: 1.75rem 1.25rem;
  }
}
</style>
