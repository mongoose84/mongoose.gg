<template>
  <div class="cs-page" data-testid="champion-select-page">
    <!-- No linked Riot account -->
    <BaseEmptyState
      v-if="hasNoLinkedAccount"
      :heading-level="1"
      title="Link your Riot account to see your picks"
      description="We sync your recent matches and your best champions for each role show up here within a few minutes."
      data-testid="champion-select-no-account"
    >
      <template #action>
        <BaseButton size="lg" data-testid="champion-select-link-account" @click="showLinkModal = true">
          <template #icon-left><BaseIcon name="link" :size="20" /></template>
          Link Riot account
        </BaseButton>
      </template>
    </BaseEmptyState>

    <template v-else>
      <div class="cs-filters" data-testid="champion-select-filters">
        <BaseSegmentedControl
          v-model="queueFilter"
          :options="QUEUE_OPTIONS"
          aria-label="Queue"
          test-id-prefix="queue"
        />
        <BaseSegmentedControl
          v-model="timeRange"
          :options="TIME_OPTIONS"
          aria-label="Time range"
          test-id-prefix="time"
        />
      </div>

      <!-- Loading: the page frame with skeletons in the layout of the content -->
      <div
        v-if="pageIsLoading"
        class="cs-loading"
        aria-busy="true"
        data-testid="champion-select-loading"
      >
        <h1 class="visually-hidden">Champion Select</h1>
        <span class="visually-hidden">Loading your champion picks</span>
        <div class="mp-hero cs-loading__hero">
          <div class="mp-hero-body">
            <div class="cs-loading__stack">
              <BaseSkeleton width="12rem" />
              <BaseSkeleton variant="title" width="70%" height="2.75rem" />
              <BaseSkeleton width="55%" />
            </div>
            <BaseSkeleton variant="block" width="16rem" height="2.25rem" />
          </div>
        </div>
        <div class="cs-grid cs-grid--3">
          <BaseSkeleton
            v-for="n in 3"
            :key="n"
            variant="block"
            width="100%"
            height="18.75rem"
            class="cs-loading__card"
          />
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="mp-card" data-testid="champion-select-error">
        <h1 class="visually-hidden">Champion Select</h1>
        <div class="mp-message mp-message--error" role="alert">
          <BaseIcon name="triangle-alert" :size="20" />
          <div class="mp-message__body">
            <p>We couldn't load your champion picks. Try again in a minute.</p>
            <button
              type="button"
              class="mp-message__action"
              data-testid="champion-select-retry"
              @click="fetchData"
            >Try again</button>
          </div>
        </div>
      </div>

      <!-- Nothing played for these filters -->
      <BaseEmptyState
        v-else-if="!roleGroups.length"
        :heading-level="1"
        title="No champions for these filters yet"
        :description="emptyDescription"
        data-testid="champion-select-empty"
      >
        <template v-if="filtersNarrowed" #action>
          <BaseButton variant="secondary" size="lg" data-testid="champion-select-show-all" @click="showAllMatches">
            Show all matches
          </BaseButton>
        </template>
      </BaseEmptyState>

      <template v-else>
        <ChampionHero
          :headline="heroHeadline"
          :text="heroText"
          :player-line="heroContext"
          :champion-name="selectedChampion.championName"
          :chips="heroChips"
          chips-label="Your stats on this pick"
        />

        <section
          v-reveal-on-view
          class="cs-section"
          aria-labelledby="cs-picks-title"
          data-testid="champion-select-picks"
        >
          <header class="cs-section__header">
            <div>
              <h2 id="cs-picks-title" class="mp-card-title">Your picks</h2>
              <p class="cs-section__caption">Best first, from your win rate, matches, laning and KDA</p>
            </div>
            <BaseSegmentedControl
              v-if="roleGroups.length > 1"
              :model-value="activeRole"
              :options="roleOptions"
              aria-label="Role"
              test-id-prefix="role"
              @update:model-value="selectRole"
            />
          </header>

          <div class="cs-grid cs-grid--3">
            <ChampionCard
              v-for="(champion, index) in picks"
              :key="champion.championId"
              selectable
              :selected="index === selectedIndex"
              :champion-name="champion.championName"
              :win-rate="champion.winRate"
              :matches="champion.gamesPlayed"
              :avg-kda="champion.avgKda"
              :strength-tag="index === 0 ? 'Best pick' : null"
              @select="selectedChampionId = champion.championId"
            />
          </div>
        </section>

        <div v-reveal-on-view class="cs-grid cs-grid--details">
          <ChampionSelectMatchups
            :champion-name="selectedChampion.championName"
            :strong="matchupSplit.strong"
            :weak="matchupSplit.weak"
            :status="matchupsStatus"
            @retry="fetchMatchups"
          />
          <ChampionSelectSearch
            :matchups="matchups || []"
            :role="activeRole"
            :role-name="roleLabel(activeRole)"
            :disabled="matchupsStatus !== 'ready'"
          />
        </div>
      </template>
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
import { useAuthStore } from '../stores/authStore'
import { useAsyncData } from '../composables/useAsyncData'
import { vRevealOnView } from '../composables/useRevealOnView'
import { getChampionSelectData, getChampionMatchups } from '../services/soloApi'
import {
  sortRoleGroups,
  pickDefaultRole,
  splitMatchups,
  buildPickHeadline,
  buildPickText,
  buildPickChips,
  roleLabel
} from '../utils/championSelectSummary'
import BaseButton from '../components/base/BaseButton.vue'
import BaseIcon from '../components/base/BaseIcon.vue'
import BaseSkeleton from '../components/base/BaseSkeleton.vue'
import BaseEmptyState from '../components/base/BaseEmptyState.vue'
import BaseSegmentedControl from '../components/base/BaseSegmentedControl.vue'
import ChampionCard from '../components/base/ChampionCard.vue'
import ChampionHero from '../components/base/ChampionHero.vue'
import ChampionSelectMatchups from '../components/championSelect/ChampionSelectMatchups.vue'
import ChampionSelectSearch from '../components/championSelect/ChampionSelectSearch.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

// ARAM is left out: it has no lanes and no champion choice
const QUEUE_OPTIONS = [
  { value: 'all', label: 'All queues' },
  { value: 'ranked_solo', label: 'Solo/Duo' },
  { value: 'ranked_flex', label: 'Flex' },
  { value: 'normal', label: 'Normal' }
]

const TIME_OPTIONS = [
  { value: 'current_season', label: 'This season' },
  { value: '3m', label: 'Last 3 months' },
  { value: 'all', label: 'All time' }
]

const MAX_PICKS = 3

const authStore = useAuthStore()

const queueFilter = ref('all')
const timeRange = ref('current_season')
const showLinkModal = ref(false)

// Champion focus: null means "use the default" (most-played role, best pick)
const selectedRole = ref(null)
const selectedChampionId = ref(null)

const pageData = ref(null)
const hasFetched = ref(false)
let pageRequest = 0

const {
  error,
  isLoading,
  execute: executePageFetch
} = useAsyncData(
  () => getChampionSelectData(authStore.userId, queueFilter.value, timeRange.value),
  { immediate: false, errorMessage: 'Failed to load champion select data' }
)

const matchups = ref(null)
const matchupsLoading = ref(false)
const matchupsError = ref(false)
let matchupsRequest = 0

const hasNoLinkedAccount = computed(() => authStore.isInitialized && authStore.hasLinkedAccount === false)
const pageIsLoading = computed(() => (isLoading.value || !hasFetched.value) && !pageData.value && !error.value)

const roleGroups = computed(() => sortRoleGroups(pageData.value?.mainChampions))
const roleOptions = computed(() => roleGroups.value.map((group) => ({ value: group.role, label: roleLabel(group.role) })))

const activeRole = computed(() => {
  const roles = roleGroups.value.map((group) => group.role)
  return roles.includes(selectedRole.value) ? selectedRole.value : pickDefaultRole(roleGroups.value)
})

const picks = computed(() => {
  const group = roleGroups.value.find((g) => g.role === activeRole.value)
  return (group?.champions || []).slice(0, MAX_PICKS)
})

const selectedIndex = computed(() => {
  const index = picks.value.findIndex((c) => c.championId === selectedChampionId.value)
  return index === -1 ? 0 : index
})

const selectedChampion = computed(() => picks.value[selectedIndex.value] ?? null)

const matchupSplit = computed(() =>
  selectedChampion.value
    ? splitMatchups(matchups.value, selectedChampion.value.championId, activeRole.value)
    : { strong: [], weak: [] }
)

const matchupsStatus = computed(() => {
  if (matchupsError.value) return 'error'
  if (matchupsLoading.value && !matchups.value) return 'loading'
  return 'ready'
})

const queueLabel = computed(() => QUEUE_OPTIONS.find((o) => o.value === queueFilter.value)?.label ?? '')
const timeLabel = computed(() => TIME_OPTIONS.find((o) => o.value === timeRange.value)?.label ?? '')

const heroHeadline = computed(() => buildPickHeadline(selectedChampion.value, selectedIndex.value, activeRole.value))
const heroText = computed(() => buildPickText(selectedChampion.value, matchupSplit.value))
const heroChips = computed(() => buildPickChips(selectedChampion.value))
const heroContext = computed(() => `${roleLabel(activeRole.value)} · ${queueLabel.value} · ${timeLabel.value}`)

const filtersNarrowed = computed(() => queueFilter.value !== 'all' || timeRange.value !== 'all')
const emptyDescription = computed(() =>
  filtersNarrowed.value
    ? `You have no matches in ${queueLabel.value.toLowerCase()} for ${timeLabel.value.toLowerCase()}. Widen the filters or play a few matches and your picks show up here.`
    : 'Play a few matches and your best champions for each role show up here.'
)

function selectRole(role) {
  selectedRole.value = role
  selectedChampionId.value = null
}

function showAllMatches() {
  queueFilter.value = 'all'
  timeRange.value = 'all'
}

async function fetchPage() {
  const request = ++pageRequest
  try {
    const result = await executePageFetch()
    if (request === pageRequest) pageData.value = result
  } catch {
    if (request === pageRequest) pageData.value = null
  } finally {
    if (request === pageRequest) hasFetched.value = true
  }
}

async function fetchMatchups() {
  const request = ++matchupsRequest
  matchupsLoading.value = true
  matchupsError.value = false
  try {
    const result = await getChampionMatchups(authStore.userId, queueFilter.value, timeRange.value)
    if (request === matchupsRequest) matchups.value = result?.matchups ?? []
  } catch (err) {
    console.error('Failed to load champion matchups:', err)
    if (request === matchupsRequest) {
      matchups.value = null
      matchupsError.value = true
    }
  } finally {
    if (request === matchupsRequest) matchupsLoading.value = false
  }
}

function fetchData() {
  if (!authStore.userId || hasNoLinkedAccount.value) return
  fetchPage()
  fetchMatchups()
}

async function handleLinkSuccess() {
  await authStore.refreshUser()
  fetchData()
}

watch([queueFilter, timeRange], fetchData)

// Wait for auth so the active account is settled before the first request
watch(() => authStore.isInitialized, (initialized) => {
  if (initialized) fetchData()
}, { immediate: true })

watch(() => authStore.activeAccountPuuid, () => {
  if (!authStore.isInitialized) return
  selectedRole.value = null
  selectedChampionId.value = null
  fetchData()
})
</script>

<style scoped>
.cs-page {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  padding: 1.75rem 0 3.5rem;
}

.cs-filters {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.75rem;
}

.cs-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.cs-section__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 0.75rem 1rem;
}

.cs-section__caption {
  margin-top: 0.25rem;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.cs-grid {
  display: grid;
  gap: 1.25rem;
}

.cs-grid--3 {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

/* Matchups flexible, search a fixed column beside it */
.cs-grid--details {
  grid-template-columns: minmax(0, 1fr) 26.25rem;
  align-items: start;
}

/* Skeletons wait 300ms before showing, so fast loads never flash them */
.cs-loading {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  animation: cs-loading-appear 0s linear 300ms both;
}

.cs-loading__stack {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.cs-loading__card {
  border-radius: 1.5rem;
}

@keyframes cs-loading-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

@media (max-width: 1099px) {
  .cs-grid--details {
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  }
}

@media (max-width: 899px) {
  .cs-page {
    padding: 1rem 0 1.75rem;
  }

  .cs-filters {
    justify-content: flex-start;
  }

  .cs-grid--3,
  .cs-grid--details {
    grid-template-columns: minmax(0, 1fr);
  }

  .cs-loading__hero .mp-hero-body {
    padding: 1.75rem 1.25rem;
  }
}
</style>
