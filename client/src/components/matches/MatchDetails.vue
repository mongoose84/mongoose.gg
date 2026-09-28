<template>
  <div class="match-details" data-testid="match-details">
    <!-- Loading: the detail frame with skeletons in the layout of the content -->
    <div v-if="loading" class="loading-state match-details__loading" aria-busy="true" data-testid="match-details-loading">
      <span class="visually-hidden">Loading the match</span>
      <div class="mp-card match-details__skeleton-card">
        <div class="match-details__skeleton-top">
          <BaseSkeleton variant="portrait" width="4rem" height="4rem" />
          <div class="match-details__skeleton-text">
            <BaseSkeleton width="5rem" />
            <BaseSkeleton variant="title" width="40%" />
            <BaseSkeleton width="60%" />
          </div>
        </div>
      </div>
      <div class="mp-card match-details__skeleton-grid">
        <BaseSkeleton v-for="n in 6" :key="n" variant="block" width="100%" height="5.5rem" class="match-details__skeleton-tile" />
      </div>
    </div>

    <!-- A failed request: say so and offer a retry -->
    <div v-else-if="error === 'failed'" class="mp-card error-state" data-testid="match-details-error">
      <div class="mp-message mp-message--error" role="alert">
        <BaseIcon name="triangle-alert" :size="20" />
        <div class="mp-message__body">
          <p>We couldn't load this match. Try again in a minute.</p>
          <button type="button" class="mp-message__action" data-testid="match-details-retry" @click="$emit('retry')">Try again</button>
        </div>
      </div>
    </div>

    <BaseEmptyState
      v-else-if="error"
      class="error-state"
      :title="errorCopy.title"
      :description="errorCopy.description"
      data-testid="match-details-unavailable"
    />

    <BaseEmptyState
      v-else-if="!match"
      class="empty-state"
      title="Pick a match to see how it went"
      description="Choose a match from the list and its stats, lanes and team summary show up here."
      data-testid="match-details-empty"
    />

    <div v-else class="details-content">
      <MatchHeader :match="match" :badge="badge" @download="downloadMatchData" />
      <WinPredictionStats :match="match" :baseline="baseline" />
      <MatchNarrative :match-id="match?.matchId" :account-id="accountId" />
      <TeamComparison :match="match" />
      <StatSnapshot :match="match" :baseline="baseline" />
      <MatchActions :match="match" />
    </div>
  </div>
</template>

<script setup>
/**
 * The open match, in the design-system order: summary card → the stats that decide matches →
 * lane by lane → team summary → every stat → next step. Owns loading, error and empty states.
 */
import { computed, watch } from 'vue'
import BaseIcon from '../base/BaseIcon.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseEmptyState from '../base/BaseEmptyState.vue'
import MatchHeader from './MatchHeader.vue'
import TeamComparison from './TeamComparison.vue'
import WinPredictionStats from './WinPredictionStats.vue'
import StatSnapshot from './StatSnapshot.vue'
import MatchNarrative from './MatchNarrative.vue'
import MatchActions from './MatchActions.vue'
import { trackMatchDetailsView } from '../../services/analyticsApi'

const props = defineProps({
  match: {
    type: Object,
    default: null
  },
  baseline: {
    type: Object,
    default: null
    // Expected: RoleBaseline for the selected match's role
  },
  accountId: {
    type: String,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  },
  /** failed (retry offered), not-found or no-account */
  error: {
    type: String,
    default: null
  },
  /** The match's standout finding from the list ({ text, type }) */
  badge: {
    type: Object,
    default: null
  }
})

defineEmits(['retry'])

const errorCopy = computed(() => {
  if (props.error === 'no-account') {
    return {
      title: 'We can\'t tell which Riot account played this match',
      description: 'Pick the account in the avatar menu, or link it in Settings, and open the match again.'
    }
  }
  return {
    title: 'We couldn\'t find this match',
    description: 'It may belong to a Riot account that is no longer linked. Pick another match from the list.'
  }
})

// Track when match details are viewed
watch(
  () => props.match?.matchId,
  (matchId) => {
    if (matchId && props.match) {
      trackMatchDetailsView(matchId, props.match.role, props.match.win)
    }
  },
  { immediate: true }
)

/**
 * Calculate trend direction based on value vs baseline average
 */
function calculateTrend(value, avgValue, threshold = 0.1) {
  if (!avgValue || avgValue === 0) return 'neutral'
  const diff = (value - avgValue) / avgValue
  if (diff >= threshold) return 'above'
  if (diff <= -threshold) return 'below'
  return 'neutral'
}

/**
 * Calculate percentage difference from baseline
 */
function calculatePercentDiff(value, avgValue) {
  if (!avgValue || avgValue === 0) return 0
  return ((value - avgValue) / avgValue) * 100
}

/**
 * Build match data object with trend comparisons for download
 */
function buildMatchDataForDownload() {
  const m = props.match
  const b = props.baseline

  const kda = m.deaths === 0 ? (m.kills + m.assists) : (m.kills + m.assists) / m.deaths
  const durationRatio = b && b.avgGameDurationSec > 0
    ? m.gameDurationSec / b.avgGameDurationSec
    : 1
  const hasBaseline = b && b.gamesCount > 0

  return {
    meta: {
      matchId: m.matchId,
      exportedAt: new Date().toISOString(),
      baselineGamesCount: b?.gamesCount || 0,
      baselineRole: b?.role || null
    },
    match: {
      championName: m.championName,
      championId: m.championId,
      role: m.role,
      lane: m.lane,
      queueType: m.queueType,
      queueId: m.queueId,
      result: m.win ? 'Victory' : 'Defeat',
      gameDurationSec: m.gameDurationSec,
      gameDurationMin: (m.gameDurationSec / 60).toFixed(1),
      gameStartTime: m.gameStartTime,
      gameStartDate: new Date(m.gameStartTime).toISOString()
    },
    stats: {
      kda: {
        kills: m.kills, deaths: m.deaths, assists: m.assists,
        ratio: parseFloat(kda.toFixed(2)),
        baseline: hasBaseline ? parseFloat(b.avgKda.toFixed(2)) : null,
        trend: hasBaseline ? calculateTrend(kda, b.avgKda, 0.15) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(kda, b.avgKda).toFixed(1)) : null
      },
      killParticipation: {
        value: parseFloat(m.killParticipation.toFixed(1)),
        baseline: hasBaseline ? parseFloat(b.avgKillParticipation.toFixed(1)) : null,
        trend: hasBaseline ? calculateTrend(m.killParticipation, b.avgKillParticipation, 0.1) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.killParticipation, b.avgKillParticipation).toFixed(1)) : null
      },
      damageDealt: {
        value: m.damageDealt,
        baseline: hasBaseline ? Math.round(b.avgDamageDealt) : null,
        baselineAdjusted: hasBaseline ? Math.round(b.avgDamageDealt * durationRatio) : null,
        trend: hasBaseline ? calculateTrend(m.damageDealt, b.avgDamageDealt * durationRatio, 0.15) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.damageDealt, b.avgDamageDealt * durationRatio).toFixed(1)) : null
      },
      damageShare: { value: parseFloat(m.damageShare.toFixed(1)) },
      damageTaken: {
        value: m.damageTaken,
        baseline: hasBaseline ? Math.round(b.avgDamageTaken) : null,
        baselineAdjusted: hasBaseline ? Math.round(b.avgDamageTaken * durationRatio) : null,
        trend: hasBaseline ? calculateTrend(m.damageTaken, b.avgDamageTaken * durationRatio, 0.15) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.damageTaken, b.avgDamageTaken * durationRatio).toFixed(1)) : null
      },
      creepScore: {
        value: m.creepScore, csPerMin: parseFloat(m.csPerMin.toFixed(1)),
        baseline: hasBaseline ? parseFloat(b.avgCreepScore.toFixed(1)) : null,
        baselineCsPerMin: hasBaseline ? parseFloat(b.avgCsPerMin.toFixed(1)) : null,
        trend: hasBaseline ? calculateTrend(m.csPerMin, b.avgCsPerMin, 0.1) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.csPerMin, b.avgCsPerMin).toFixed(1)) : null
      },
      gold: {
        value: m.goldEarned, goldPerMin: parseFloat(m.goldPerMin.toFixed(1)),
        baseline: hasBaseline ? Math.round(b.avgGoldEarned) : null,
        baselineGoldPerMin: hasBaseline ? parseFloat(b.avgGoldPerMin.toFixed(1)) : null,
        trend: hasBaseline ? calculateTrend(m.goldPerMin, b.avgGoldPerMin, 0.1) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.goldPerMin, b.avgGoldPerMin).toFixed(1)) : null
      },
      visionScore: {
        value: m.visionScore,
        baseline: hasBaseline ? parseFloat(b.avgVisionScore.toFixed(1)) : null,
        baselineAdjusted: hasBaseline ? parseFloat((b.avgVisionScore * durationRatio).toFixed(1)) : null,
        trend: hasBaseline ? calculateTrend(m.visionScore, b.avgVisionScore * durationRatio, 0.15) : null,
        percentDiff: hasBaseline ? parseFloat(calculatePercentDiff(m.visionScore, b.avgVisionScore * durationRatio).toFixed(1)) : null
      },
      earlyGame: { deathsPre10: m.deathsPre10, goldDiffAt15: m.goldDiffAt15 }
    },
    teamComparison: {
      teamKills: m.teamKills, enemyTeamKills: m.enemyTeamKills,
      teamTotalDamage: m.teamTotalDamage, enemyTeamTotalDamage: m.enemyTeamTotalDamage,
      teamGoldLeadAt15: m.teamGoldLeadAt15,
      teamDragons: m.teamDragons, enemyTeamDragons: m.enemyTeamDragons,
      teamBarons: m.teamBarons, enemyTeamBarons: m.enemyTeamBarons,
      teamTowers: m.teamTowers, enemyTeamTowers: m.enemyTeamTowers
    },
    baselineInfo: hasBaseline ? {
      role: b.role, gamesCount: b.gamesCount,
      winRate: parseFloat(b.winRate.toFixed(1)),
      avgGameDurationSec: Math.round(b.avgGameDurationSec)
    } : null
  }
}

/**
 * Trigger download of match data as JSON file
 */
function downloadMatchData() {
  if (!props.match) return

  const data = buildMatchDataForDownload()
  const json = JSON.stringify(data, null, 2)
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = `match-${props.match.matchId}-${props.match.championName}.json`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.match-details,
.details-content,
.match-details__loading {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* Skeletons wait 300ms before showing, so fast loads never flash them */
.match-details__loading {
  animation: match-details-appear 0s linear 300ms both;
}

.match-details__skeleton-top {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.match-details__skeleton-text {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  gap: 0.625rem;
}

.match-details__skeleton-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
}

.match-details__skeleton-tile {
  border-radius: 0.75rem;
}

@keyframes match-details-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}
</style>
