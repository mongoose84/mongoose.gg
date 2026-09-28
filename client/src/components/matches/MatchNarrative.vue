<template>
  <section
    class="mp-card narrative"
    aria-labelledby="narrative-title"
    :aria-busy="loading ? 'true' : undefined"
    data-testid="match-narrative"
  >
    <header class="narrative__header">
      <h3 id="narrative-title" class="mp-card-title" data-testid="narrative-title">{{ title }}</h3>
      <p class="narrative__caption">{{ caption }}</p>
    </header>

    <div v-if="loading" class="loading-state" data-testid="narrative-loading">
      <span class="loading-text visually-hidden">Loading the lanes</span>
      <div v-for="n in 5" :key="n" class="narrative__skeleton-row">
        <BaseSkeleton width="4.5rem" />
        <BaseSkeleton variant="block" width="2.25rem" height="2.25rem" />
        <BaseSkeleton width="3rem" />
        <BaseSkeleton variant="block" width="2.25rem" height="2.25rem" />
      </div>
    </div>

    <div v-else-if="error" class="error-state mp-message mp-message--error" role="alert" data-testid="narrative-error">
      <BaseIcon name="triangle-alert" :size="20" />
      <div class="mp-message__body">
        <p class="error-text">{{ error }}</p>
        <button
          v-if="canRetry"
          type="button"
          class="mp-message__action"
          data-testid="narrative-retry"
          @click="load"
        >Try again</button>
      </div>
    </div>

    <p v-else-if="!narrativeData || narrativeData.laneMatchups.length === 0" class="empty-state narrative__none" data-testid="narrative-empty">
      Lane details aren't available for this match.
    </p>

    <!-- ARAM: both teams by damage share -->
    <div v-else-if="narrativeData.isAram" class="aram-players">
      <div v-for="team in aramTeams" :key="team.key" class="narrative__team">
        <h4 class="team-header" :class="team.key">{{ team.title }}</h4>
        <ul class="narrative__list">
          <li
            v-for="(participant, index) in team.players"
            :key="`${team.key}-${index}-${participant.championId}`"
            class="aram-player-row"
            :class="{ 'user-row': isUserChampion(participant) }"
          >
            <img :src="participant.championIconUrl" alt="" class="narrative__icon" />
            <span class="narrative__name">
              {{ participant.championName }}
              <span v-if="isUserChampion(participant)" class="you-badge mp-chip mp-chip--strength">You</span>
            </span>
            <span class="narrative__kda">{{ formatKda(participant) }}</span>
            <span class="narrative__share">{{ formatPercent(participant.damageShare) }} damage</span>
          </li>
        </ul>
      </div>
    </div>

    <!-- Lanes: one row per role, opens to the lane details -->
    <ul v-else class="lane-matchups narrative__list">
      <li
        v-for="matchup in narrativeData.laneMatchups"
        :key="matchup.role"
        class="lane-row"
        :class="{ expanded: expandedRole === matchup.role, 'user-role': isUserRole(matchup.role) }"
        data-testid="lane-row"
      >
        <button
          type="button"
          class="lane-header"
          :aria-expanded="expandedRole === matchup.role ? 'true' : 'false'"
          :aria-controls="`lane-details-${matchup.role}`"
          :data-testid="`lane-toggle-${matchup.role}`"
          @click="toggleExpand(matchup.role)"
        >
          <span class="lane-role">
            <img :src="getRoleIconUrl(matchup.role)" alt="" class="role-icon" />
            {{ formatRole(matchup.role) }}
            <span v-if="isUserRole(matchup.role)" class="you-badge mp-chip mp-chip--strength">You</span>
          </span>

          <span class="lane-champion">
            <img :src="matchup.allyParticipant.championIconUrl" :alt="matchup.allyParticipant.championName" class="narrative__icon" />
            <span class="narrative__kda">{{ formatKda(matchup.allyParticipant) }}</span>
          </span>
          <span class="lane-vs" aria-hidden="true">vs</span>
          <span class="lane-champion lane-champion--enemy">
            <span class="visually-hidden">versus</span>
            <span class="narrative__kda">{{ formatKda(matchup.enemyParticipant) }}</span>
            <img :src="matchup.enemyParticipant.championIconUrl" :alt="matchup.enemyParticipant.championName" class="narrative__icon" />
          </span>

          <span class="winner-badge" :class="winnerClass(matchup.laneWinner)" data-testid="lane-result">
            {{ winnerText(matchup.laneWinner) }}
          </span>
          <BaseIcon name="chevron-down" :size="20" class="lane-chevron" />
        </button>

        <div
          v-if="expandedRole === matchup.role"
          :id="`lane-details-${matchup.role}`"
          class="lane-details"
        >
          <LaneMatchupDetails :matchup="matchup" />
        </div>
      </li>
    </ul>
  </section>
</template>

<script setup>
/**
 * Lane by lane for the open match: each role's matchup with who won the lane (gold at 10),
 * opening to the lane details. ARAM lists both teams by damage share instead.
 */
import { ref, watch, computed } from 'vue'
import BaseIcon from '../base/BaseIcon.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import LaneMatchupDetails from './LaneMatchupDetails.vue'
import { getMatchNarrative } from '../../services/matchesApi'
import { trackLaneExpand } from '../../services/analyticsApi'
import { formatRole, formatKdaFromParticipant as formatKda, formatPercent } from '@/utils/formatters'
import { getRoleIconUrl } from '@/utils/leagueAssets'

const props = defineProps({
  matchId: {
    type: String,
    default: null
  },
  accountId: {
    type: String,
    default: null
  }
})

const loading = ref(false)
const error = ref(null)
const canRetry = ref(false)
const narrativeData = ref(null)
const expandedRole = ref(null)
let request = 0

async function load() {
  const matchId = props.matchId
  const accountId = props.accountId
  const current = ++request
  expandedRole.value = null

  if (!matchId) {
    narrativeData.value = null
    error.value = null
    return
  }

  if (!accountId) {
    narrativeData.value = null
    canRetry.value = false
    error.value = 'No linked Riot account for this match.'
    return
  }

  loading.value = true
  error.value = null
  try {
    const result = await getMatchNarrative(matchId, accountId)
    if (current === request) narrativeData.value = result
  } catch (err) {
    console.error('Failed to fetch match narrative:', err)
    if (current === request) {
      narrativeData.value = null
      canRetry.value = true
      error.value = "We couldn't load the lanes. Try again in a minute."
    }
  } finally {
    if (current === request) loading.value = false
  }
}

watch([() => props.matchId, () => props.accountId], load, { immediate: true })

const lanesWon = computed(() => (narrativeData.value?.laneMatchups || []).filter((m) => m.laneWinner === 'ally').length)

const title = computed(() => {
  const data = narrativeData.value
  if (!data || !data.laneMatchups?.length) return 'Lane by lane'
  if (data.isAram) return 'Both teams by damage'
  return `Your team won ${lanesWon.value} of ${data.laneMatchups.length} lanes`
})

const caption = computed(() =>
  narrativeData.value?.isAram
    ? 'Each player with their share of their team\'s damage.'
    : 'The lane goes to whoever is 300 or more gold ahead at 10 minutes. Open a lane for the details.'
)

function toggleExpand(role) {
  const isExpanding = expandedRole.value !== role
  expandedRole.value = isExpanding ? role : null

  // Track lane expansion (only when expanding, not collapsing)
  if (isExpanding) {
    const matchup = narrativeData.value?.laneMatchups?.find((m) => m.role === role)
    if (matchup) {
      trackLaneExpand(role, isUserRole(role), matchup.laneWinner)
    }
  }
}

function isUserRole(role) {
  return narrativeData.value?.userRole === role
}

// ARAM has no roles, so the user is flagged on the participant
function isUserChampion(participant) {
  return participant?.isUserParticipant === true
}

function winnerText(winner) {
  if (winner === 'ally') return 'Won lane'
  if (winner === 'enemy') return 'Lost lane'
  return 'Even'
}

function winnerClass(winner) {
  if (winner === 'ally') return 'mp-up'
  if (winner === 'enemy') return 'mp-down'
  return 'winner-badge--even'
}

const aramTeams = computed(() => {
  const matchups = narrativeData.value?.isAram ? narrativeData.value.laneMatchups || [] : []
  const byShare = (a, b) => b.damageShare - a.damageShare
  return [
    { key: 'ally', title: 'Your team', players: matchups.map((m) => m.allyParticipant).sort(byShare) },
    { key: 'enemy', title: 'Enemy team', players: matchups.map((m) => m.enemyParticipant).sort(byShare) }
  ]
})
</script>

<style scoped>
.narrative {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.narrative__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.narrative__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.narrative__list {
  list-style: none;
}

.narrative__skeleton-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  min-height: 3.75rem;
  border-top: 1px solid var(--color-border);
}

.narrative__none {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.lane-row {
  border-top: 1px solid var(--color-border);
}

.lane-header {
  display: grid;
  grid-template-columns: minmax(6.5rem, 1fr) auto auto auto 5.5rem 1.25rem;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  min-height: 3.75rem;
  padding: 0.5rem 0;
  border: none;
  background: transparent;
  font: inherit;
  color: var(--color-text);
  text-align: left;
  cursor: pointer;
}

.lane-header:focus-visible {
  outline: none;
  border-radius: 0.75rem;
  box-shadow: var(--shadow-focus);
}

.lane-role {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  font-size: 0.875rem;
  font-weight: 600;
}

.role-icon {
  width: 1.25rem;
  height: 1.25rem;
  object-fit: contain;
  filter: brightness(0) invert(1);
  opacity: 0.7;
}

.lane-champion {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.lane-vs {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.narrative__icon {
  width: 2.25rem;
  height: 2.25rem;
  flex-shrink: 0;
  border-radius: 0.75rem;
  background: var(--color-track);
}

.narrative__kda {
  min-width: 3.5rem;
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.lane-champion--enemy .narrative__kda {
  text-align: right;
}

.winner-badge {
  font-size: 0.8125rem;
  font-weight: 700;
  text-align: right;
}

.winner-badge--even {
  color: var(--color-text-secondary);
}

.lane-chevron {
  color: var(--color-text-secondary);
}

.expanded .lane-chevron {
  transform: rotate(180deg);
  color: var(--color-text);
}

.lane-header:hover .lane-role {
  color: var(--color-positive-text-strong);
}

.lane-details {
  padding: 0.25rem 0 1.25rem;
}

/* ARAM */
.aram-players {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.25rem;
}

.team-header {
  padding-bottom: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.team-header.ally { color: var(--color-positive-text); }
.team-header.enemy { color: var(--color-warn-text); }

.aram-player-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 3.25rem;
  border-top: 1px solid var(--color-border);
}

.narrative__name {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-grow: 1;
  min-width: 0;
  font-size: 0.875rem;
  font-weight: 600;
}

.narrative__share {
  font-size: 0.8125rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

@media (max-width: 599px) {
  .lane-header {
    grid-template-columns: minmax(0, 1fr) auto auto 1.25rem;
    row-gap: 0.25rem;
  }

  .lane-role {
    grid-column: 1 / -1;
  }

  .lane-vs {
    display: none;
  }

  .winner-badge {
    grid-column: 3;
  }

  .aram-players {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
