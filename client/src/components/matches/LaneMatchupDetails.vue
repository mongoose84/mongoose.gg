<template>
  <div class="lane-matchup-details">
    <p class="insight-text" data-testid="lane-insight">{{ contextualInsight }}</p>

    <div class="lane-phases">
      <div v-for="phase in phases" :key="phase.key" class="phase-section">
        <h4 class="phase-title mp-eyebrow">{{ phase.title }}</h4>
        <table class="phase-table">
          <thead class="visually-hidden">
            <tr>
              <th scope="col">Stat</th>
              <th scope="col">{{ matchup.allyParticipant.championName }}</th>
              <th scope="col">{{ matchup.enemyParticipant.championName }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in phase.rows" :key="row.label" class="stat-row">
              <th scope="row" class="stat-label">{{ row.label }}</th>
              <td class="stat-value ally" :class="row.sentiment">{{ row.ally }}</td>
              <td class="stat-value enemy">{{ row.enemy }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
/**
 * One lane opened up: a sentence on how the lane went, then early laning and match impact
 * for you (or your laner) against the opponent. Leads are purple, deficits orange.
 */
import { computed } from 'vue'
import { formatPercent } from '@/utils/formatters'
import { formatSigned } from '@/utils/matchesSummary'

const props = defineProps({
  matchup: {
    type: Object,
    required: true
  }
})

// Your side's difference; like the server's lane winner, fall back to the opponent's, inverted
function sideDiff(field) {
  const own = props.matchup.allyParticipant[field]
  if (own !== null && own !== undefined) return own
  const theirs = props.matchup.enemyParticipant[field]
  return theirs !== null && theirs !== undefined ? -theirs : 0
}

const goldDiff = computed(() => sideDiff('goldDiffAt10'))
const csDiff = computed(() => sideDiff('csDiffAt10'))

// 300 gold at 10 minutes decides the lane (lower than the 500 used at 15)
const goldDiffSentiment = computed(() => {
  if (goldDiff.value >= 300) return 'positive'
  if (goldDiff.value <= -300) return 'negative'
  return 'neutral'
})

const csDiffSentiment = computed(() => {
  if (csDiff.value >= 10) return 'positive'
  if (csDiff.value <= -10) return 'negative'
  return 'neutral'
})

const phases = computed(() => {
  const ally = props.matchup.allyParticipant
  const enemy = props.matchup.enemyParticipant
  return [
    {
      key: 'early',
      title: 'Early laning (0–10 min)',
      rows: [
        { label: 'Gold lead', ally: formatSigned(goldDiff.value), enemy: formatSigned(-goldDiff.value), sentiment: goldDiffSentiment.value },
        { label: 'CS lead', ally: formatSigned(csDiff.value), enemy: formatSigned(-csDiff.value), sentiment: csDiffSentiment.value },
        { label: 'Deaths', ally: ally.deathsPre10 ?? 0, enemy: enemy.deathsPre10 ?? 0, sentiment: null }
      ]
    },
    {
      key: 'impact',
      title: 'Match impact',
      rows: [
        { label: 'Damage share', ally: formatPercent(ally.damageShare), enemy: formatPercent(enemy.damageShare), sentiment: null },
        { label: 'Kill participation', ally: formatPercent(ally.killParticipation), enemy: formatPercent(enemy.killParticipation), sentiment: null },
        { label: 'Vision score', ally: ally.visionScore, enemy: enemy.visionScore, sentiment: null }
      ]
    }
  ]
})

// One sentence on how the lane went, finding first
const contextualInsight = computed(() => {
  const ally = props.matchup.allyParticipant
  const enemy = props.matchup.enemyParticipant
  const winner = props.matchup.laneWinner
  const lead = Math.abs(goldDiff.value).toLocaleString('en-US')

  if (winner === 'ally') {
    if (ally.killParticipation < enemy.killParticipation) {
      return `${ally.championName} won the lane by ${lead} gold, but ${enemy.championName} had more say in fights with ${formatPercent(enemy.killParticipation)} kill participation.`
    }
    if (ally.visionScore < enemy.visionScore - 5) {
      return `${ally.championName} won the lane by ${lead} gold, but ${enemy.championName} warded more (${enemy.visionScore} to ${ally.visionScore} vision score).`
    }
    return `${ally.championName} won the lane by ${lead} gold and turned it into match impact.`
  }
  if (winner === 'enemy') {
    if (ally.killParticipation > enemy.killParticipation) {
      return `${ally.championName} lost the lane by ${lead} gold but stayed in the fights with ${formatPercent(ally.killParticipation)} kill participation.`
    }
    return `${enemy.championName} won this lane by ${lead} gold at 10 minutes. Fewer early deaths and safer trades close most of that gap.`
  }
  return 'An even lane: both players had a similar share of the match.'
})
</script>

<style scoped>
.lane-matchup-details {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.insight-text {
  max-width: 40.625rem;
  font-size: 0.9375rem;
  line-height: 1.5;
  color: var(--color-ink-soft);
}

.lane-phases {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.25rem;
}

.phase-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-width: 0;
}

/* Section labels are neutral here: the rows carry the meaning */
.phase-title {
  color: var(--color-text-secondary);
}

.phase-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.phase-table th,
.phase-table td {
  padding: 0.5rem 0;
  border-top: 1px solid var(--color-border);
}

.stat-label {
  font-weight: 500;
  text-align: left;
  color: var(--color-ink-soft);
}

.stat-value {
  width: 4.5rem;
  font-family: var(--font-display);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  text-align: right;
  color: var(--color-text);
}

.stat-value.enemy {
  color: var(--color-text-secondary);
}

.stat-value.positive { color: var(--color-positive-text); }
.stat-value.negative { color: var(--color-warn-text); }

@media (max-width: 599px) {
  .lane-phases {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
