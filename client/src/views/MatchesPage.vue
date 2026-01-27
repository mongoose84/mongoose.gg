<template>
  <section class="matches-page" data-testid="matches-page">
    <header class="page-header" data-testid="matches-header">
      <h1 class="sr-only">Matches</h1>

      <!-- Queue Toggle Bar -->
      <div class="queue-toggle-group" role="group" aria-label="Filter by queue type">
        <button
          v-for="queue in queueOptions"
          :key="queue.value"
          type="button"
          class="queue-toggle-btn"
          :class="{ 'queue-toggle-btn--active': queueFilter === queue.value }"
          @click="handleQueueChange(queue.value)"
          :aria-pressed="queueFilter === queue.value"
        >
          {{ queue.label }}
        </button>
      </div>
    </header>

    <!-- Main Content: Two Column Layout -->
    <div class="main-content">
      <!-- Left Column: Match List -->
      <div class="match-list-column">
        <div class="column-header">
          <h2 class="column-title">Recent Matches</h2>
          <span v-if="data" class="match-count">{{ data.totalMatches }} matches</span>
        </div>
        <MatchList
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
          </div>
          <div class="details-card-content">
            <MatchDetails
              :match="selectedMatch"
              :baseline="selectedBaseline"
            />
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { getMatchList } from '../services/authApi'
import { trackFilterChange } from '../services/analyticsApi'
import MatchList from '../components/matches/MatchList.vue'
import MatchDetails from '../components/matches/MatchDetails.vue'

const authStore = useAuthStore()

// State
const loading = ref(false)
const error = ref(null)
const data = ref(null)
const queueFilter = ref('all')
const selectedMatchId = ref(null)

// Queue options for toggle bar
const queueOptions = [
  { value: 'all', label: 'All Queues' },
  { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
  { value: 'ranked_flex', label: 'Ranked Flex' },
  { value: 'normal', label: 'Normal' },
  { value: 'aram', label: 'ARAM' }
]

// Computed: Selected match object
const selectedMatch = computed(() => {
  if (!selectedMatchId.value || !data.value?.matches) return null
  return data.value.matches.find(m => m.matchId === selectedMatchId.value) || null
})

// Computed: Baseline for selected match's role
const selectedBaseline = computed(() => {
  if (!selectedMatch.value || !data.value?.baselinesByRole) return null
  const role = selectedMatch.value.role || 'UNKNOWN'
  return data.value.baselinesByRole[role] || null
})

// Fetch match data
async function fetchMatches() {
  if (!authStore.userId) return

  loading.value = true
  error.value = null

  try {
    const result = await getMatchList(authStore.userId, queueFilter.value)
    data.value = result

    // Auto-select first match if none selected
    if (result?.matches?.length > 0 && !selectedMatchId.value) {
      selectedMatchId.value = result.matches[0].matchId
    }
  } catch (err) {
    console.error('Failed to fetch matches:', err)
    error.value = err.message || 'Failed to load matches'
  } finally {
    loading.value = false
  }
}

// Handlers
function handleMatchSelect(matchId) {
  selectedMatchId.value = matchId
}

function handleQueueChange(value) {
  queueFilter.value = value
  selectedMatchId.value = null // Reset selection on filter change
  trackFilterChange('queue', value)
  fetchMatches()
}

// Initial load
onMounted(() => {
  fetchMatches()
})
</script>

<style scoped>
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

/* Queue Toggle Group */
.queue-toggle-group {
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

.queue-toggle-btn:hover:not(.queue-toggle-btn--active) {
  color: var(--color-text);
  background: var(--color-elevated);
}

.queue-toggle-btn--active {
  background: var(--color-primary);
  color: white;
}

.queue-toggle-btn--active::after {
  display: none;
}

.queue-toggle-btn:has(+ .queue-toggle-btn--active)::after {
  display: none;
}

.queue-toggle-btn:focus {
  outline: none;
  box-shadow: inset 0 0 0 2px var(--color-primary-soft);
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
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-elevated);
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

