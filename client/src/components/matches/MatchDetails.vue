<template>
  <div class="match-details">
    <!-- Empty state when no match selected -->
    <div v-if="!match" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="empty-icon">
        <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm8.706-1.442c1.146-.573 2.437.463 2.126 1.706l-.709 2.836.042-.02a.75.75 0 01.67 1.34l-.04.022c-1.147.573-2.438-.463-2.127-1.706l.71-2.836-.042.02a.75.75 0 11-.671-1.34l.041-.022zM12 9a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
      </svg>
      <span class="empty-text">Select a match to view details</span>
    </div>

    <!-- Match details content -->
    <div v-else class="details-content">
      <MatchHeader :match="match" />

      <div class="details-sections">
        <TeamComparison :match="match" />
        <div class="impact-card">
          <ImpactStats :match="match" />
          <MatchActions />
        </div>
        <MatchNarrative :matchId="match?.matchId" />
        <StatSnapshot :match="match" :baseline="baseline" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { watch } from 'vue'
import MatchHeader from './MatchHeader.vue'
import TeamComparison from './TeamComparison.vue'
import ImpactStats from './ImpactStats.vue'
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

// Expose download function to parent component
defineExpose({ downloadMatchData })
</script>

<style scoped>
.match-details {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.details-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  overflow-y: auto;
  padding-right: var(--spacing-xs);
}

.details-sections {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
}

/* Impact Card */
.impact-card {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-md);
  height: 100%;
  min-height: 300px;
  text-align: center;
  padding: var(--spacing-2xl);
}

.empty-icon {
  width: 48px;
  height: 48px;
  color: var(--color-text-secondary);
  opacity: 0.3;
}

.empty-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>

