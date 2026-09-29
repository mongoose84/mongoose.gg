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
        <BaseSkeleton variant="block" width="2.25rem" height="2.25rem" />
        <BaseSkeleton width="100%" />
        <BaseSkeleton variant="block" width="2.25rem" height="2.25rem" />
        <BaseSkeleton width="3rem" />
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

    <!-- Lanes: one LaneBar per role (gold difference at 10), opens to the lane details -->
    <ul v-else class="lane-matchups narrative__list">
      <li
        v-for="lane in lanes"
        :key="lane.role"
        class="lane-row"
        :class="{ expanded: expandedRole === lane.role, 'user-role': lane.isYou }"
        data-testid="lane-row"
      >
        <button
          type="button"
          class="mp-lane-bar lane-header"
          :aria-expanded="expandedRole === lane.role ? 'true' : 'false'"
          :aria-controls="`lane-details-${lane.role}`"
          :aria-label="lane.ariaLabel"
          :data-testid="`lane-toggle-${lane.role}`"
          @click="toggleExpand(lane.role)"
        >
          <img
            :src="lane.matchup.allyParticipant.championIconUrl"
            alt=""
            :class="{ 'is-you': lane.isYou }"
          />
          <span class="mp-lane-bar__track" aria-hidden="true">
            <span class="mp-lane-bar__behind"><span :style="{ width: lane.behindWidth }" /></span>
            <span class="mp-lane-bar__axis" />
            <span class="mp-lane-bar__ahead"><span :style="{ width: lane.aheadWidth }" /></span>
          </span>
          <img
            :src="lane.matchup.enemyParticipant.championIconUrl"
            alt=""
            class="lane-bar__enemy"
          />
          <span
            class="mp-lane-bar__diff"
            :class="lane.diffClass"
            data-testid="lane-result"
          >{{ lane.diffText }}</span>
        </button>

        <div
          v-if="expandedRole === lane.role"
          :id="`lane-details-${lane.role}`"
          class="lane-details"
        >
          <LaneMatchupDetails :matchup="lane.matchup" />
        </div>
      </li>
    </ul>
  </section>
</template>

<script setup>
/**
 * Lane by lane for the open match: one LaneBar per role with the gold difference at 10 minutes,
 * opening to the lane details. ARAM lists both teams by damage share instead.
 */
import { ref, watch, computed } from 'vue'
import BaseIcon from '../base/BaseIcon.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import LaneMatchupDetails from './LaneMatchupDetails.vue'
import { getMatchNarrative } from '../../services/matchesApi'
import { trackLaneExpand } from '../../services/analyticsApi'
import { formatRole, formatKdaFromParticipant as formatKda, formatPercent } from '@/utils/formatters'
import { laneGoldDiffAt10, laneBar } from '@/utils/matchesSummary'

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
  // A request still in flight is now stale and won't reset this itself
  loading.value = false

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

const lanes = computed(() => (narrativeData.value?.laneMatchups || []).map((matchup) => {
  const isYou = isUserRole(matchup.role)
  const bar = laneBar(laneGoldDiffAt10(matchup))
  const roleName = formatRole(matchup.role)
  return {
    role: matchup.role,
    matchup,
    isYou,
    ...bar,
    diffClass: bar.result === 'won' ? 'mp-up' : bar.result === 'lost' ? 'mp-down' : 'lane-bar__diff--even',
    ariaLabel: `${roleName}${isYou ? ' (you)' : ''}: ${bar.description}`
  }
}))

const lanesWon = computed(() => lanes.value.filter((lane) => lane.result === 'won').length)

const title = computed(() => {
  const data = narrativeData.value
  if (!data || !data.laneMatchups?.length) return 'Lane by lane'
  if (data.isAram) return 'Both teams by damage'
  return `${lanesWon.value} of ${data.laneMatchups.length} lanes won`
})

const caption = computed(() =>
  narrativeData.value?.isAram
    ? 'Each player with their share of their team\'s damage.'
    : 'Gold at 10 min'
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
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.25rem 1rem;
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

/* LaneBar: icons sit on track until they load; the enemy side is quieter */
.mp-lane-bar img {
  background: var(--color-track);
}

.lane-bar__enemy {
  opacity: 0.85;
}

.lane-bar__diff--even {
  color: var(--color-text-secondary);
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
  .mp-lane-bar {
    grid-template-columns: 2rem minmax(0, 1fr) 2rem 3.25rem;
    gap: 0.625rem;
    min-height: 3.25rem;
  }

  .mp-lane-bar img {
    width: 2rem;
    height: 2rem;
    border-radius: 0.75rem;
  }

  .aram-players {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
