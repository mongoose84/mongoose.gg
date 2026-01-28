<template>
  <div class="match-narrative">
    <h3 class="section-title">Match Narrative</h3>

    <!-- Loading state -->
    <div v-if="loading" class="loading-state">
      <span class="loading-text">Loading lane matchups...</span>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="error-state">
      <span class="error-text">{{ error }}</span>
    </div>

    <!-- No data state -->
    <div v-else-if="!narrativeData || narrativeData.laneMatchups.length === 0" class="empty-state">
      <span class="empty-text">No lane matchup data available</span>
    </div>

    <!-- Lane matchups -->
    <div v-else class="lane-matchups">
      <div
        v-for="matchup in narrativeData.laneMatchups"
        :key="matchup.role"
        class="lane-row"
        :class="{
          expanded: expandedRole === matchup.role,
          'user-role': isUserRole(matchup.role)
        }"
        @click="toggleExpand(matchup.role)"
      >
        <!-- Collapsed View -->
        <div class="lane-header">
          <span class="role-icon">{{ getRoleIcon(matchup.role) }}</span>
          <span class="role-name">
            {{ formatRole(matchup.role) }}
            <span v-if="isUserRole(matchup.role)" class="you-badge">YOU</span>
          </span>

          <!-- Ally Champion -->
          <div class="champion-info ally" :class="{ winner: matchup.laneWinner === 'ally' }">
            <img :src="matchup.allyParticipant.championIconUrl" :alt="matchup.allyParticipant.championName" class="champion-icon" />
            <span class="kda">{{ formatKda(matchup.allyParticipant) }}</span>
          </div>

          <span class="vs-text">VS</span>

          <!-- Enemy Champion -->
          <div class="champion-info enemy" :class="{ winner: matchup.laneWinner === 'enemy' }">
            <span class="kda">{{ formatKda(matchup.enemyParticipant) }}</span>
            <img :src="matchup.enemyParticipant.championIconUrl" :alt="matchup.enemyParticipant.championName" class="champion-icon" />
          </div>

          <!-- Winner indicator -->
          <span v-if="matchup.laneWinner !== 'even'" class="winner-badge" :class="matchup.laneWinner">
            {{ matchup.laneWinner === 'ally' ? '✓' : '✗' }}
          </span>
          <span v-else class="winner-badge even">—</span>

          <span class="expand-icon">{{ expandedRole === matchup.role ? '▼' : '▶' }}</span>
        </div>

        <!-- Expanded View -->
        <div v-if="expandedRole === matchup.role" class="lane-details">
          <LaneMatchupDetails :matchup="matchup" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import { useAuthStore } from '../../stores/authStore'
import { getMatchNarrative } from '../../services/authApi'
import { trackLaneExpand } from '../../services/analyticsApi'
import LaneMatchupDetails from './LaneMatchupDetails.vue'

const props = defineProps({
  matchId: {
    type: String,
    default: null
  }
})

const authStore = useAuthStore()

// State
const loading = ref(false)
const error = ref(null)
const narrativeData = ref(null)
const expandedRole = ref(null)

// Watch both matchId and puuid - puuid may be populated asynchronously after mount
const puuid = computed(() => authStore.primaryRiotAccount?.puuid)

watch(
  [() => props.matchId, puuid],
  async ([newMatchId, newPuuid]) => {
    if (!newMatchId) {
      narrativeData.value = null
      return
    }

    if (!newPuuid) {
      error.value = 'No linked Riot account'
      return
    }

    loading.value = true
    error.value = null

    try {
      narrativeData.value = await getMatchNarrative(newMatchId, newPuuid)
    } catch (err) {
      console.error('Failed to fetch match narrative:', err)
      error.value = err.message || 'Failed to load lane matchups'
    } finally {
      loading.value = false
    }
  },
  { immediate: true }
)

// Toggle expanded state
function toggleExpand(role) {
  const isExpanding = expandedRole.value !== role
  expandedRole.value = isExpanding ? role : null

  // Track lane expansion (only when expanding, not collapsing)
  if (isExpanding) {
    const matchup = narrativeData.value?.laneMatchups?.find(m => m.role === role)
    if (matchup) {
      trackLaneExpand(role, isUserRole(role), matchup.laneWinner)
    }
  }
}

// Helpers
function isUserRole(role) {
  return narrativeData.value?.userRole === role
}

function getRoleIcon(role) {
  const icons = { TOP: '⚔️', JUNGLE: '🌲', MIDDLE: '🎯', BOTTOM: '🏹', UTILITY: '🛡️' }
  return icons[role] || '❓'
}

function formatRole(role) {
  const names = { TOP: 'Top', JUNGLE: 'Jungle', MIDDLE: 'Mid', BOTTOM: 'Bot', UTILITY: 'Support' }
  return names[role] || role
}

function formatKda(participant) {
  return `${participant.kills}/${participant.deaths}/${participant.assists}`
}
</script>

<style scoped>
.match-narrative {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.section-title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.loading-state, .error-state, .empty-state {
  padding: var(--spacing-md);
  text-align: center;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.error-state { color: #ef4444; }

.lane-matchups {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.lane-row {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.2s ease;
}

.lane-row:hover { border-color: var(--color-text-secondary); }
.lane-row.expanded { border-color: var(--color-primary); }

/* User's role highlighting */
.lane-row.user-role {
  border-color: var(--color-primary);
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.08) 0%, var(--color-surface) 100%);
}
.lane-row.user-role:hover { border-color: var(--color-primary); }

.lane-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-xl);
  padding: var(--spacing-sm) var(--spacing-md);
}

.you-badge {
  font-size: 8px;
  font-weight: var(--font-weight-bold);
  color: white;
  background: var(--color-primary);
  padding: 1px 4px;
  border-radius: var(--radius-xs, 2px);
  text-transform: uppercase;
  letter-spacing: 0.3px;
  flex-shrink: 0;
}

.role-icon { font-size: 16px; }
.role-name {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  min-width: 60px;
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.champion-info {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.champion-info.winner .champion-icon {
  box-shadow: 0 0 0 2px #22c55e;
}

.champion-icon {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-sm);
}

.kda {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  min-width: 50px;
}

.ally .kda { text-align: right; }
.enemy .kda { text-align: left; }

.vs-text {
  font-size: 10px;
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-bold);
}

.winner-badge {
  font-size: 12px;
  font-weight: var(--font-weight-bold);
  width: 20px;
  text-align: center;
}
.winner-badge.ally { color: #22c55e; }
.winner-badge.enemy { color: #ef4444; }
.winner-badge.even { color: var(--color-text-secondary); }

.expand-icon {
  font-size: 10px;
  color: var(--color-text-secondary);
  margin-left: auto;
}

.lane-details {
  border-top: 1px solid var(--color-border);
  padding: var(--spacing-md);
}
</style>

