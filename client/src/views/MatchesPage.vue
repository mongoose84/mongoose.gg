<template>
  <div
    class="matches-page"
    :class="{ 'matches-page--open': selectedMatchId }"
    data-testid="matches-page"
  >
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
      title="Link your Riot account to see your matches"
      description="We sync your recent matches and they show up here within a few minutes."
      data-testid="matches-no-account"
    >
      <template #action>
        <BaseButton size="lg" data-testid="matches-link-account" @click="showLinkModal = true">
          <template #icon-left><BaseIcon name="link" :size="20" /></template>
          Link Riot account
        </BaseButton>
      </template>
    </BaseEmptyState>

    <template v-else>
      <header class="matches-header" data-testid="matches-header">
        <div class="matches-header__summary">
          <div class="matches-header__text">
            <h1 class="matches-header__title" data-testid="matches-headline">{{ headline }}</h1>
            <p v-if="subline" class="matches-header__subline" data-testid="matches-subline">{{ subline }}</p>
          </div>
          <BaseFormStrip v-if="form.length > 1" :results="form" class="matches-header__form" />
          <p v-if="lpTotal" class="matches-header__lp" data-testid="matches-lp-total">
            <span class="matches-header__lp-value" :class="lpChangeClass(lpTotal.total)">{{ lpTotalText }}</span>
            <span class="matches-header__lp-label">LP over {{ lpTotal.matches }}</span>
          </p>
        </div>
        <BaseSegmentedControl
          v-model="queueFilter"
          :options="QUEUE_OPTIONS"
          aria-label="Queue"
          test-id-prefix="queue"
        />
      </header>

      <div class="matches-layout" :class="{ 'matches-layout--single': !showDetailColumn }">
        <!-- The list (and the start-time chart under it) stays in place while a match is open beside it -->
        <div class="matches-side">
          <section
            class="mp-card matches-list"
            aria-labelledby="matches-list-title"
            :aria-busy="listIsLoading ? 'true' : undefined"
            data-testid="matches-list"
          >
            <header class="matches-list__header">
              <h2 id="matches-list-title" class="mp-card-title">{{ listTitle }}</h2>
              <BaseButton
                variant="ghost"
                size="sm"
                :disabled="isSyncing"
                data-testid="matches-sync"
                @click="startSync"
              >
                <template #icon-left><BaseIcon name="refresh-cw" :size="16" /></template>
                Sync matches
              </BaseButton>
            </header>

            <!-- Loading: rows in the layout of the content, shown after 300ms -->
            <div v-if="listIsLoading" class="matches-list__loading" data-testid="matches-list-loading">
              <span class="visually-hidden">Loading your matches</span>
              <div v-for="n in 6" :key="n" class="matches-list__skeleton-row">
                <BaseSkeleton variant="portrait" />
                <div class="matches-list__skeleton-text">
                  <BaseSkeleton width="40%" />
                  <BaseSkeleton width="75%" />
                </div>
              </div>
            </div>

            <div v-else-if="error" class="mp-message mp-message--error" role="alert" data-testid="matches-list-error">
              <BaseIcon name="triangle-alert" :size="20" />
              <div class="mp-message__body">
                <p>We couldn't load your matches. Try again in a minute.</p>
                <button type="button" class="mp-message__action" data-testid="matches-retry" @click="fetchMatches">Try again</button>
              </div>
            </div>

            <BaseEmptyState
              v-else-if="!matches.length"
              :title="emptyTitle"
              :description="emptyDescription"
              data-testid="matches-empty"
            >
              <template #action>
                <BaseButton
                  v-if="queueFilter !== 'all'"
                  variant="secondary"
                  data-testid="matches-show-all"
                  @click="queueFilter = 'all'"
                >Show all queues</BaseButton>
                <BaseButton
                  v-else
                  :disabled="isSyncing"
                  data-testid="matches-empty-sync"
                  @click="startSync"
                >
                  <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
                  Sync matches
                </BaseButton>
              </template>
            </BaseEmptyState>

            <nav v-else aria-labelledby="matches-list-title" class="mp-match-list matches-list__rows">
              <BaseMatchRow
                v-for="(match, index) in matches"
                :key="match.matchId"
                :to="{ name: 'app-matches', params: { matchId: match.matchId } }"
                :champion-name="match.championName"
                :champion-icon-url="match.championIconUrl"
                :win="match.win"
                :remake="isRemake(match)"
                :kda="matchKda(match)"
                :queue="match.queueType"
                :duration-seconds="match.gameDurationSec"
                :timestamp="match.gameStartTime"
                :riot-id="matchRiotId(match)"
                :lp-change="match.lpChange ?? null"
                @click="trackMatchSelect(match.matchId, index, queueFilter)"
              />
            </nav>
          </section>

          <section
            v-if="startTimeChart"
            class="mp-card matches-hours"
            aria-labelledby="matches-hours-title"
            data-testid="matches-hours"
          >
            <header class="matches-hours__header">
              <h2 id="matches-hours-title" class="mp-card-title matches-hours__title" data-testid="matches-hours-title">
                {{ startTimeChart.title ?? startTimeCaption }}
              </h2>
              <p v-if="startTimeChart.title" class="matches-hours__caption">{{ startTimeCaption }}</p>
            </header>
            <BaseColumnChart
              :groups="startTimeColumns"
              :weak-key="startTimeChart.weakKey"
              measure="Win rate by start time"
            />
          </section>
        </div>

        <section
          v-if="showDetailColumn"
          class="matches-detail"
          aria-label="Match details"
          data-testid="matches-detail"
        >
          <router-link
            :to="{ name: 'app-matches' }"
            class="mp-btn mp-btn--ghost mp-btn--sm matches-detail__back"
            data-testid="matches-back"
          >All matches</router-link>

          <MatchDetails
            :match="matchDetails"
            :baseline="matchDetailsBaseline"
            :deciding-stat="matchDetailsDecidingStat"
            :account-id="matchDetailsAccountId"
            :loading="detailsLoading || (listIsLoading && !matchDetails)"
            :error="detailsError"
            @retry="fetchMatchDetails(selectedMatchId)"
          />
        </section>
      </div>
    </template>
  </div>

  <LinkRiotAccountModal
    :is-open="showLinkModal"
    @close="showLinkModal = false"
    @success="handleLinkSuccess"
  />
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { useAsyncData } from '../composables/useAsyncData'
import { useSyncMatches } from '../composables/useSyncMatches'
import { getMatchList, getMatchDetails } from '../services/matchesApi'
import { trackFilterChange, trackMatchSelect } from '../services/analyticsApi'
import {
  isRemake,
  matchKda,
  matchRiotId,
  buildMatchesHeadline,
  buildMatchesSubline,
  buildStartTimeChart,
  formResults,
  formatSigned,
  lpChangeClass,
  sumLpChange,
  RANKED_QUEUE_FILTERS
} from '../utils/matchesSummary'
import BaseButton from '../components/base/BaseButton.vue'
import BaseIcon from '../components/base/BaseIcon.vue'
import BaseSkeleton from '../components/base/BaseSkeleton.vue'
import BaseEmptyState from '../components/base/BaseEmptyState.vue'
import BaseSegmentedControl from '../components/base/BaseSegmentedControl.vue'
import BaseMatchRow from '../components/base/BaseMatchRow.vue'
import BaseFormStrip from '../components/base/BaseFormStrip.vue'
import BaseColumnChart from '../components/base/BaseColumnChart.vue'
import SyncProgress from '../components/base/SyncProgress.vue'
import MatchDetails from '../components/matches/MatchDetails.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

// Four options at most (SegmentedControl); ARAM matches show under All queues
const QUEUE_OPTIONS = [
  { value: 'all', label: 'All queues' },
  { value: 'ranked_solo', label: 'Solo/Duo' },
  { value: 'ranked_flex', label: 'Flex' },
  { value: 'normal', label: 'Normal' }
]

const DESKTOP_QUERY = '(min-width: 900px)'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const {
  syncState,
  isSyncing,
  progressCurrent,
  progressTotal,
  syncedCount,
  startSync
} = useSyncMatches()

const queueFilter = ref('all')
const showLinkModal = ref(false)
const hasFetched = ref(false)
let listRequest = 0
let detailsRequest = 0

const {
  data,
  error,
  isLoading,
  execute: executeMatchListFetch
} = useAsyncData(
  () => getMatchList(authStore.userId, queueFilter.value),
  { immediate: false, errorMessage: 'Failed to load matches' }
)

// Match details state (fetched on demand for the open match)
const matchDetails = ref(null)
const matchDetailsBaseline = ref(null)
const matchDetailsDecidingStat = ref(null)
const matchDetailsAccountId = ref(null)
const detailsLoading = ref(false)
const detailsError = ref(null)

const hasNoLinkedAccount = computed(() => authStore.isInitialized && authStore.hasLinkedAccount === false)
const matches = computed(() => data.value?.matches ?? [])
const listIsLoading = computed(() => (isLoading.value || !hasFetched.value) && !data.value && !error.value)

/** The open match comes from the URL, so a match can be shared and Back works */
const selectedMatchId = computed(() => {
  const id = route.params.matchId
  return typeof id === 'string' && id ? id : null
})

// The detail column shows once a match is open, or on desktop while the list has matches
const showDetailColumn = computed(() => Boolean(selectedMatchId.value) || matches.value.length > 0 || listIsLoading.value)

const queueLabel = computed(() => QUEUE_OPTIONS.find((o) => o.value === queueFilter.value)?.label ?? '')

const headline = computed(() => buildMatchesHeadline(matches.value) ?? 'Your matches')
const subline = computed(() => buildMatchesSubline(matches.value))
const form = computed(() => formResults(matches.value))

// LP over the list, only for one ranked queue (Solo/Duo and Flex are separate ladders)
const lpTotal = computed(() =>
  RANKED_QUEUE_FILTERS.includes(queueFilter.value) ? sumLpChange(matches.value) : null
)
const lpTotalText = computed(() => {
  const total = lpTotal.value?.total
  if (typeof total !== 'number') return ''
  return total === 0 ? '±0' : formatSigned(total)
})

// Win rate by start time, from the same list (null when fewer than two groups can be compared)
const startTimeChart = computed(() => buildStartTimeChart(matches.value))
const startTimeColumns = computed(() =>
  (startTimeChart.value?.groups ?? []).map((g) => ({ key: g.key, label: g.label, value: g.winRate }))
)
const startTimeCaption = computed(() => {
  const counted = matches.value.filter((m) => !isRemake(m)).length
  return `Win rate by start time, last ${counted} ${counted === 1 ? 'match' : 'matches'}`
})

const listTitle = computed(() => {
  if (!matches.value.length) return 'Recent matches'
  return `Last ${matches.value.length} ${matches.value.length === 1 ? 'match' : 'matches'}`
})

const emptyTitle = computed(() =>
  queueFilter.value === 'all' ? 'No matches yet' : `No ${queueLabel.value} matches yet`
)
const emptyDescription = computed(() =>
  queueFilter.value === 'all'
    ? 'Play a match or sync now, and your matches show up here within a few minutes.'
    : `Your ${queueLabel.value} matches show up here after you play one and sync.`
)

function isDesktop() {
  return typeof window !== 'undefined'
    && typeof window.matchMedia === 'function'
    && window.matchMedia(DESKTOP_QUERY).matches
}

function normalizeAccountField(value) {
  return String(value ?? '').trim().toLowerCase()
}

function getSafeAccountId(account) {
  const id = account?.accountId
  return typeof id === 'string' && id.startsWith('acc_') ? id : null
}

function getMatchDetailsAccountId(matchId) {
  if (!authStore.isOverallMode) {
    return getSafeAccountId(authStore.activeAccount)
  }

  const selectedMatch = matches.value.find((match) => match.matchId === matchId)
  if (selectedMatch) {
    const selectedGameName = normalizeAccountField(selectedMatch.accountGameName)
    const selectedTagLine = normalizeAccountField(selectedMatch.accountTagLine)
    const selectedRegion = normalizeAccountField(selectedMatch.accountRegion)

    const matchedAccount = authStore.riotAccounts.find((account) => {
      return normalizeAccountField(account.gameName) === selectedGameName &&
        normalizeAccountField(account.tagLine) === selectedTagLine &&
        normalizeAccountField(account.region) === selectedRegion
    })

    if (matchedAccount) {
      return getSafeAccountId(matchedAccount)
    }
  }

  return getSafeAccountId(authStore.activeAccount) ?? getSafeAccountId(authStore.primaryRiotAccount)
}

function clearDetails() {
  // Any request still in flight is now stale
  detailsRequest++
  matchDetails.value = null
  matchDetailsBaseline.value = null
  matchDetailsDecidingStat.value = null
  matchDetailsAccountId.value = null
  detailsError.value = null
  detailsLoading.value = false
}

// Fetch the list (summary rows only)
async function fetchMatches() {
  if (!authStore.userId || hasNoLinkedAccount.value) return

  const request = ++listRequest
  let result = null
  try {
    result = await executeMatchListFetch()
  } catch {
    // useAsyncData holds the error for the list card
  }
  if (request !== listRequest) return
  hasFetched.value = true

  const first = result?.matches?.[0]?.matchId
  if (!selectedMatchId.value && first && isDesktop()) {
    // Desktop opens the newest match beside the list
    router.replace({ name: 'app-matches', params: { matchId: first } })
  } else if (selectedMatchId.value && authStore.isOverallMode && !matchDetails.value && !detailsLoading.value) {
    // Overall mode waited for the list to know which account played the match. If the list
    // failed, the account falls back to the active or primary one, so the open match still loads.
    fetchMatchDetails(selectedMatchId.value)
  }
}

// Fetch full match details on demand
async function fetchMatchDetails(matchId) {
  if (!matchId) {
    clearDetails()
    return
  }

  const accountId = getMatchDetailsAccountId(matchId)
  if (!accountId) {
    clearDetails()
    detailsError.value = 'no-account'
    return
  }

  const request = ++detailsRequest
  matchDetailsAccountId.value = accountId
  detailsLoading.value = true
  detailsError.value = null

  // Only the latest request may touch the state: an older one for the same match
  // (opened again, or a double retry) must not overwrite or clear what it loaded
  const isCurrent = () => request === detailsRequest && selectedMatchId.value === matchId

  try {
    const result = await getMatchDetails(matchId, accountId)
    if (!isCurrent()) return

    if (result === null) {
      clearDetails()
      detailsError.value = 'not-found'
      return
    }

    matchDetails.value = result.match ?? null
    matchDetailsBaseline.value = result.baseline ?? null
    matchDetailsDecidingStat.value = result.decidingStat ?? null
  } catch (err) {
    if (!isCurrent()) return

    console.error('Failed to fetch match details:', err)
    clearDetails()
    detailsError.value = 'failed'
  } finally {
    if (isCurrent()) {
      detailsLoading.value = false
    }
  }
}

/** Back to the plain list URL, then reload: used when the filter or account changes */
function resetAndFetch() {
  clearDetails()
  if (selectedMatchId.value) {
    router.replace({ name: 'app-matches' })
  }
  fetchMatches()
}

async function handleLinkSuccess() {
  await authStore.refreshUser()
  fetchMatches()
}

watch(selectedMatchId, (matchId) => {
  // Overall mode waits for the list to resolve the account (fetchMatches loads it then)
  if (matchId && authStore.isOverallMode && !hasFetched.value) return
  if (matchId) {
    fetchMatchDetails(matchId)
    return
  }
  clearDetails()
  // Back on the plain list URL (e.g. the Matches tab) with the list loaded: desktop opens the
  // newest match again. While the list reloads, fetchMatches opens it when the answer arrives.
  const first = matches.value[0]?.matchId
  if (first && hasFetched.value && !isLoading.value && isDesktop()) {
    router.replace({ name: 'app-matches', params: { matchId: first } })
  }
}, { immediate: true })

watch(queueFilter, (value) => {
  trackFilterChange('queue', value)
  resetAndFetch()
})

watch(() => authStore.activeAccountPuuid, () => {
  if (!authStore.isInitialized) return
  resetAndFetch()
})

// A finished sync that brought in matches refreshes the list
watch(syncState, (state, previous) => {
  if (state === 'done' && previous === 'running') fetchMatches()
})

// Wait for auth so the active account is settled before the first request
watch(() => authStore.isInitialized, (initialized) => {
  if (initialized) fetchMatches()
}, { immediate: true })
</script>

<style scoped>
.matches-page {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  padding: 1.75rem 0 3.5rem;
}

.matches-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem 1.5rem;
}

/* Headline and FormStrip side by side; the strip drops under the headline when space runs out */
.matches-header__summary {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 1rem 2.5rem;
}

.matches-header__text {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  max-width: 40.625rem;
}

.matches-header__lp {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.matches-header__lp-value {
  font-family: var(--font-display);
  font-size: 2rem;
  font-weight: 700;
  line-height: 1;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.matches-header__lp-value.mp-up { color: var(--color-positive-text); }
.matches-header__lp-value.mp-down { color: var(--color-warn-text); }

.matches-header__lp-label {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.matches-header__form {
  width: 22.5rem;
  max-width: 100%;
}

.matches-header__title {
  font-family: var(--font-display);
  font-size: 2.25rem;
  font-weight: 600;
  line-height: 1.15;
  letter-spacing: -0.01em;
  color: var(--color-text);
}

.matches-header__subline {
  font-size: 1rem;
  line-height: 1.5;
  color: var(--color-ink-soft);
}

.matches-layout {
  display: grid;
  grid-template-columns: 26.25rem minmax(0, 1fr);
  gap: 1.25rem;
  align-items: start;
}

.matches-layout--single {
  grid-template-columns: minmax(0, 1fr);
}

/* The list column stays in place under the 80px header while the open match scrolls;
   the list scrolls inside it and the start-time chart stays under it */
.matches-side {
  position: sticky;
  top: 6.25rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  max-height: calc(100dvh - 7.5rem);
}

.matches-list {
  display: flex;
  flex-direction: column;
  flex: 0 1 auto;
  gap: 1rem;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
}

.matches-layout--single .matches-side {
  position: static;
  max-height: none;
}

.matches-hours {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  gap: 1rem;
  padding-block: 1.5rem;
}

.matches-hours__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.matches-hours__title {
  font-size: 1.125rem;
}

.matches-hours__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.matches-list__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.matches-list__rows {
  display: flex;
  flex-direction: column;
}

/* Skeletons wait 300ms before showing, so fast loads never flash them */
.matches-list__loading {
  display: flex;
  flex-direction: column;
  animation: matches-loading-appear 0s linear 300ms both;
}

.matches-list__skeleton-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  min-height: 4.25rem;
  border-top: 1px solid var(--color-border);
}

.matches-list__skeleton-text {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  gap: 0.5rem;
}

@keyframes matches-loading-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

.matches-detail {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
}

/* The back link is for phones, where a match is its own page */
.matches-detail__back {
  display: none;
  align-self: flex-start;
}

@media (max-width: 1099px) {
  .matches-layout {
    grid-template-columns: 22.5rem minmax(0, 1fr);
  }
}

/* Phones: the list is the page; an open match replaces it with a back link */
@media (max-width: 899px) {
  .matches-page {
    gap: 1.25rem;
    padding: 1rem 0 1.75rem;
  }

  .matches-header__title {
    font-size: 1.75rem;
  }

  .matches-layout,
  .matches-layout--single {
    grid-template-columns: minmax(0, 1fr);
  }

  .matches-header__form {
    width: 100%;
  }

  .matches-side {
    position: static;
    max-height: none;
  }

  .matches-list {
    overflow: visible;
  }

  .matches-page:not(.matches-page--open) .matches-detail {
    display: none;
  }

  .matches-page--open .matches-header,
  .matches-page--open .matches-side {
    display: none;
  }

  .matches-detail__back {
    display: inline-flex;
  }
}
</style>
