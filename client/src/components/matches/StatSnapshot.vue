<template>
  <section class="mp-card stat-snapshot" aria-labelledby="snapshot-title" data-testid="stat-snapshot">
    <header class="stat-snapshot__header">
      <h3 id="snapshot-title" class="mp-card-title section-title" data-testid="snapshot-title">{{ title }}</h3>
      <p class="stat-snapshot__caption">All {{ stats.length }} of your stats from this match</p>
    </header>
    <ul class="stats-grid">
      <li v-for="stat in stats" :key="stat.label" class="stat-item" :class="stat.trend">
        <span class="stat-label">{{ stat.label }}</span>
        <span class="stat-value">{{ stat.value }}</span>
        <span v-if="stat.comparison" class="stat-comparison" :class="stat.trend">
          <span v-if="stat.trend" class="trend-arrow" aria-hidden="true">{{ stat.trend === 'up' ? '▲' : '▼' }} </span>{{ stat.comparison }}
        </span>
      </li>
    </ul>
  </section>
</template>

<script setup>
/**
 * Every stat of the open match with how it compares to the player's recent matches in the
 * role (adjusted for match length where it matters). Up is purple ▲, down orange ▼.
 */
import { computed } from 'vue'
import { formatNumber } from '@/utils/formatters'
import { formatSigned } from '@/utils/matchesSummary'

const props = defineProps({
  match: {
    type: Object,
    required: true
  },
  baseline: {
    type: Object,
    default: null
  }
})

const stats = computed(() => {
  const m = props.match
  const b = props.baseline

  const getKda = () => {
    return m.deaths === 0 ? (m.kills + m.assists) : (m.kills + m.assists) / m.deaths
  }

  // Calculate duration ratio for adjusting baseline expectations
  const getDurationRatio = () => {
    if (!b || b.avgGameDurationSec === 0) return 1
    return m.gameDurationSec / b.avgGameDurationSec
  }

  const getTrend = (value, avgValue, threshold = 0.1) => {
    if (!b || b.gamesCount === 0) return null
    if (avgValue === 0) return null // Avoid division by zero, treat as neutral
    const diff = (value - avgValue) / avgValue
    if (diff >= threshold) return 'up'
    if (diff <= -threshold) return 'down'
    return null
  }

  // Duration-adjusted trend: compares value against baseline scaled by game duration
  const getTrendDurationAdjusted = (value, avgValue, threshold = 0.1) => {
    if (!b || b.gamesCount === 0) return null
    const expectedValue = avgValue * getDurationRatio()
    if (expectedValue === 0) return null
    const diff = (value - expectedValue) / expectedValue
    if (diff >= threshold) return 'up'
    if (diff <= -threshold) return 'down'
    return null
  }

  const getComparison = (value, avgValue, threshold, format = 'diff') => {
    if (!b || b.gamesCount === 0) return null
    const diff = value - avgValue
    const pctDiff = avgValue > 0 ? (diff / avgValue) * 100 : 0

    if (format === 'pct' && Math.abs(pctDiff) >= threshold) {
      return `${formatSigned(pctDiff)}% vs your average`
    } else if (format === 'diff' && Math.abs(diff) >= threshold) {
      return `${formatSigned(diff, 1)} vs your average`
    } else if (format === 'int' && Math.abs(diff) >= threshold) {
      return `${formatSigned(diff)} vs your average`
    }
    return null
  }

  // Duration-adjusted comparison: compares value against baseline scaled by game duration
  const getComparisonDurationAdjusted = (value, avgValue, threshold, format = 'diff') => {
    if (!b || b.gamesCount === 0) return null
    const expectedValue = avgValue * getDurationRatio()
    const diff = value - expectedValue
    const pctDiff = expectedValue > 0 ? (diff / expectedValue) * 100 : 0

    if (format === 'pct' && Math.abs(pctDiff) >= threshold) {
      return `${formatSigned(pctDiff)}% vs your average`
    } else if (format === 'diff' && Math.abs(diff) >= threshold) {
      return `${formatSigned(diff, 1)} vs your average`
    } else if (format === 'int' && Math.abs(diff) >= threshold) {
      return `${formatSigned(diff)} vs your average`
    }
    return null
  }

  const kda = getKda()
  const isSupport = m.role === 'UTILITY' || m.role === 'SUPPORT'

  const dmgGoldRatio = m.goldEarned > 0 ? m.damageDealt / m.goldEarned : 0
  const dmgGoldTrend = isSupport ? null : (dmgGoldRatio >= 1.5 ? 'up' : dmgGoldRatio < 0.8 ? 'down' : null)
  const dmgGoldComparison = isSupport ? null : (dmgGoldRatio >= 1.5 ? 'Efficient carry' : dmgGoldRatio < 0.8 ? 'Low output' : 'Average output')

  const slot10 = isSupport
    ? (() => {
        const gameMins = m.gameDurationSec / 60
        const visionPerMin = gameMins > 0 ? m.visionScore / gameMins : 0
        return {
          label: 'Vision per minute',
          value: visionPerMin.toFixed(1),
          trend: visionPerMin >= 2.5 ? 'up' : visionPerMin < 1.5 ? 'down' : null,
          comparison: visionPerMin >= 2.5 ? 'Great vision' : visionPerMin < 1.5 ? 'Low vision' : 'Average vision'
        }
      })()
    : (() => {
        const dmgPerDeath = m.damageDealt / Math.max(1, m.deaths)
        return {
          label: 'Damage per death',
          value: formatNumber(Math.round(dmgPerDeath)),
          trend: dmgPerDeath >= 8000 ? 'up' : dmgPerDeath < 3000 ? 'down' : null,
          comparison: null
        }
      })()

  return [
    {
      label: 'KDA ratio',
      value: kda.toFixed(2),
      trend: b ? getTrend(kda, b.avgKda, 0.15) : null,
      comparison: b ? getComparison(kda, b.avgKda, 0.3, 'diff') : null
    },
    {
      label: 'Kill participation',
      value: `${m.killParticipation.toFixed(0)}%`,
      trend: b ? getTrend(m.killParticipation, b.avgKillParticipation, 0.1) : null,
      comparison: b ? getComparison(m.killParticipation, b.avgKillParticipation, 5, 'int') : null
    },
    {
      label: 'Damage per gold',
      value: dmgGoldRatio.toFixed(2),
      trend: dmgGoldTrend,
      comparison: dmgGoldComparison
    },
    {
      label: 'Damage dealt',
      value: formatNumber(m.damageDealt),
      trend: isSupport ? null : (b ? getTrendDurationAdjusted(m.damageDealt, b.avgDamageDealt, 0.15) : null),
      comparison: isSupport ? null : (b ? getComparisonDurationAdjusted(m.damageDealt, b.avgDamageDealt, 10, 'pct') : null)
    },
    {
      label: 'Damage share',
      value: `${m.damageShare.toFixed(0)}%`,
      trend: isSupport ? null : (m.damageShare >= 25 ? 'up' : m.damageShare < 15 ? 'down' : null),
      comparison: isSupport ? null : (m.damageShare >= 25 ? 'Carried the damage' : null)
    },
    {
      label: 'Damage taken',
      value: formatNumber(m.damageTaken),
      trend: b ? getTrendDurationAdjusted(m.damageTaken, b.avgDamageTaken, 0.15) : null,
      comparison: b ? getComparisonDurationAdjusted(m.damageTaken, b.avgDamageTaken, 10, 'pct') : null
    },
    {
      label: 'CS',
      value: m.creepScore.toString(),
      trend: isSupport ? null : (b ? getTrendDurationAdjusted(m.creepScore, b.avgCreepScore, 0.1) : null),
      comparison: isSupport ? null : (b ? getComparisonDurationAdjusted(m.creepScore, b.avgCreepScore, 10, 'int') : null)
    },
    {
      label: 'Gold',
      value: formatNumber(m.goldEarned),
      trend: b ? getTrendDurationAdjusted(m.goldEarned, b.avgGoldEarned, 0.1) : null,
      comparison: b ? getComparisonDurationAdjusted(m.goldEarned, b.avgGoldEarned, 10, 'pct') : null
    },
    {
      label: 'Gold per minute',
      value: m.goldPerMin.toFixed(0),
      trend: b ? getTrend(m.goldPerMin, b.avgGoldPerMin, 0.1) : null,
      comparison: b ? getComparison(m.goldPerMin, b.avgGoldPerMin, 15, 'int') : null
    },
    slot10
  ]
})

const title = computed(() => {
  const up = stats.value.filter((s) => s.trend === 'up').length
  const down = stats.value.filter((s) => s.trend === 'down').length
  if (!up && !down) return 'Your stats were close to your average'
  if (!down) return `${up} of your stats beat your average`
  if (!up) return `${down} of your stats fell below your average`
  return `${up} stats above your average, ${down} below`
})
</script>

<style scoped>
.stat-snapshot {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.stat-snapshot__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.stat-snapshot__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.75rem;
  list-style: none;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
  padding: 0.875rem 1rem;
  border-radius: 0.75rem;
  background: var(--color-elevated);
}

.stat-label {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.stat-value {
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.stat-comparison {
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-ink-soft);
}

.stat-comparison.up { color: var(--color-positive-text); }
.stat-comparison.down { color: var(--color-warn-text); }

@media (max-width: 1279px) {
  .stats-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 599px) {
  .stats-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
