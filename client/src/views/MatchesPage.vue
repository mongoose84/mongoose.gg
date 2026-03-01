<template>
  <section class="matches-page" data-testid="matches-page">
    <header class="page-header" data-testid="matches-header">
      <h1 class="sr-only">Matches</h1>

      <!-- Queue Toggle Bar -->
      <BaseQueueToggle v-model="queueFilter" />
    </header>

    <!-- Main Content: Two Column Layout -->
    <div class="main-content">
      <!-- Left Column: Match List -->
      <div class="match-list-column">
        <div class="column-header">
          <h2 class="column-title">Recent Matches</h2>
          <span v-if="data" class="match-count">{{ data.totalMatches }} matches</span>
        </div>
        <div v-if="error" class="error-message">{{ error }}</div>
        <MatchList
          v-if="!error"
          :matches="data?.matches || []"
          :selectedMatchId="selectedMatchId"
          :loading="loading"
          @select="handleMatchSelect"
        />
      </div>

      <!-- Right Column: Match Details Card -->
      <div class="match-details-column">
        <div class="details-card">
          <div class="details-card-header">
            <h2 class="column-title">Match Details</h2>
            <button
              v-if="matchDetails && !detailsLoading"
              class="download-btn"
              title="Download match data"
              @click="matchDetailsRef?.downloadMatchData()"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path d="M10.75 2.75a.75.75 0 00-1.5 0v8.614L6.295 8.235a.75.75 0 10-1.09 1.03l4.25 4.5a.75.75 0 001.09 0l4.25-4.5a.75.75 0 00-1.09-1.03l-2.955 3.129V2.75z" />
                <path d="M3.5 12.75a.75.75 0 00-1.5 0v2.5A2.75 2.75 0 004.75 18h10.5A2.75 2.75 0 0018 15.25v-2.5a.75.75 0 00-1.5 0v2.5c0 .69-.56 1.25-1.25 1.25H4.75c-.69 0-1.25-.56-1.25-1.25v-2.5z" />
              </svg>
            </button>
          </div>
          <div class="details-card-content">
            <MatchDetails
              ref="matchDetailsRef"
              :match="matchDetails"
              :baseline="matchDetailsBaseline"
              :loading="detailsLoading"
              :error="detailsError"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { useAsyncData } from '../composables/useAsyncData'
import { getMatchList, getMatchDetails } from '../services/authApi'
import { trackFilterChange, trackMatchSelect } from '../services/analyticsApi'
import MatchList from '../components/matches/MatchList.vue'
import MatchDetails from '../components/matches/MatchDetails.vue'
import { BaseQueueToggle } from '../components/base'

const route = useRoute()
const authStore = useAuthStore()

// State
const queueFilter = ref('all')
const selectedMatchId = ref(null)
const matchDetailsRef = ref(null)

const {
  data,
  error,
  isLoading: loading,
  execute: executeMatchListFetch
} = useAsyncData(async () => {
  return await getMatchList(authStore.userId, queueFilter.value)
}, { immediate: false, errorMessage: 'Failed to load matches' })

// Match details state (fetched on-demand)
const matchDetails = ref(null)
const matchDetailsBaseline = ref(null)
const detailsLoading = ref(false)
const detailsError = ref(null)

// Fetch match list (lightweight summary data)
async function fetchMatches() {
  if (!authStore.userId) return

  try {
    const result = await executeMatchListFetch()

    // Use matchId from query param if provided, otherwise auto-select first match
    const queryMatchId = route.query.matchId
    if (queryMatchId && result?.matches?.some(m => m.matchId === queryMatchId)) {
      selectedMatchId.value = queryMatchId
    } else if (result?.matches?.length > 0 && !selectedMatchId.value) {
      selectedMatchId.value = result.matches[0].matchId
    }
  } catch {
    selectedMatchId.value = null
  }
}

// Fetch full match details on-demand
async function fetchMatchDetails(matchId) {
  const puuid = authStore.primaryRiotAccount?.puuid
  if (!matchId || !puuid) return

  detailsLoading.value = true
  detailsError.value = null

  try {
    const result = await getMatchDetails(matchId, puuid)

    // Guard against race condition: only update if this is still the selected match
    if (selectedMatchId.value !== matchId) {
      return // User selected a different match while we were fetching
    }

    // Handle 404 (match not found)
    if (result === null) {
      detailsError.value = 'Match not found'
      matchDetails.value = null
      matchDetailsBaseline.value = null
      return
    }

    matchDetails.value = result.match ?? null
    matchDetailsBaseline.value = result.baseline ?? null
  } catch (err) {
    // Guard against race condition for error state too
    if (selectedMatchId.value !== matchId) {
      return
    }

    console.error('Failed to fetch match details:', err)
    detailsError.value = err.message || 'Failed to load match details'
    matchDetails.value = null
    matchDetailsBaseline.value = null
  } finally {
    // Only clear loading if this is still the selected match
    if (selectedMatchId.value === matchId) {
      detailsLoading.value = false
    }
  }
}

// Handlers
function handleMatchSelect(matchId) {
  selectedMatchId.value = matchId

  // Track match selection with position in list
  const matchIndex = data.value?.matches?.findIndex(m => m.matchId === matchId) ?? -1
  trackMatchSelect(matchId, matchIndex, queueFilter.value)
}

// Initial load
onMounted(() => {
  fetchMatches()
})

// Watch selectedMatchId to fetch full details on-demand
watch(selectedMatchId, (newMatchId) => {
  if (newMatchId) {
    fetchMatchDetails(newMatchId)
  } else {
    matchDetails.value = null
    matchDetailsBaseline.value = null
    detailsError.value = null
  }
})

// Watch queue filter changes - reset selection, track, and refetch
watch(queueFilter, (newValue) => {
  selectedMatchId.value = null
  matchDetails.value = null
  matchDetailsBaseline.value = null
  detailsError.value = null
  trackFilterChange('queue', newValue)
  fetchMatches()
})

// Watch for changes to matchId in the route query and sync selection
watch(
  () => route.query.matchId,
  (newMatchId) => {
    if (!data.value?.matches) return
    if (newMatchId && data.value.matches.some(m => m.matchId === newMatchId)) {
      selectedMatchId.value = newMatchId
    }
  }
)
</script>

<style scoped>
.error-message {
  color: var(--color-danger, #ef4444);
  background: rgba(239, 68, 68, 0.08);
  border: 1px solid var(--color-danger, #ef4444);
  border-radius: var(--radius-sm);
  padding: var(--spacing-xs) var(--spacing-sm);
  margin-bottom: var(--spacing-sm);
  font-size: var(--font-size-sm);
}
.matches-page {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  height: 100vh;
  overflow: hidden;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* Main Content Layout */
.main-content {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: var(--spacing-xl);
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

.match-list-column {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  min-height: 0;
  overflow: hidden;
}

.match-details-column {
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}

.column-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
}

.column-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.match-count {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Details Card */
.details-card {
  display: flex;
  flex-direction: column;
  flex: 1;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.details-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-elevated);
}

.download-btn {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.15s ease;
}

.download-btn:hover {
  background: var(--color-elevated);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.download-btn svg {
  width: 16px;
  height: 16px;
}

.details-card-content {
  flex: 1;
  padding: var(--spacing-lg);
  overflow-y: auto;
}

/* Responsive: Stack on mobile */
@media (max-width: 1024px) {
  .main-content {
    grid-template-columns: 1fr;
  }

  .match-details-column {
    display: none; /* Hide details on mobile for now */
  }

  .matches-page {
    max-height: none;
  }
}
</style>

