<template>
  <section class="mp-card stat-snapshot" :class="{ 'stat-snapshot--open': open }" data-testid="stat-snapshot">
    <h3 class="stat-snapshot__heading">
      <button
        type="button"
        class="stat-snapshot__toggle"
        :aria-expanded="open ? 'true' : 'false'"
        aria-controls="stat-snapshot-body"
        data-testid="stat-snapshot-toggle"
        @click="open = !open"
      >
        <span class="section-title stat-snapshot__title" data-testid="snapshot-title">All your stats</span>
        <span v-if="upCount" class="stat-snapshot__count mp-up" data-testid="snapshot-up">
          <span aria-hidden="true">▲ </span>{{ upCount }} above usual
        </span>
        <span v-if="downCount" class="stat-snapshot__count mp-down" data-testid="snapshot-down">
          <span aria-hidden="true">▼ </span>{{ downCount }} below
        </span>
        <BaseIcon name="chevron-down" :size="20" class="stat-snapshot__chevron" />
      </button>
    </h3>

    <div v-if="open" id="stat-snapshot-body" class="stat-snapshot__body" data-testid="stat-snapshot-body">
      <p class="stat-snapshot__caption" data-testid="snapshot-summary">{{ summary }}</p>
      <ul class="mp-stat-grid stats-grid">
        <li v-for="stat in stats" :key="stat.label" class="mp-stat stat-item" :class="stat.trend">
          <span class="mp-stat__label stat-label">{{ stat.label }}</span>
          <span class="mp-stat__value stat-value">{{ stat.value }}</span>
          <span v-if="stat.comparison" class="mp-stat__note stat-comparison" :class="stat.trend">
            <span v-if="stat.trend" class="trend-arrow" aria-hidden="true">{{ stat.trend === 'up' ? '▲' : '▼' }} </span>{{ stat.comparison }}
          </span>
        </li>
      </ul>
      <BaseButton
        variant="secondary"
        size="sm"
        class="stat-snapshot__download"
        data-testid="match-download"
        @click="$emit('download')"
      >
        Download data
      </BaseButton>
    </div>
  </section>
</template>

<script setup>
/**
 * "All your stats": a closed disclosure whose summary row counts the stats above and below
 * the player's recent matches in the role (adjusted for match length where it matters). Open,
 * it lists every stat (up purple ▲, down orange ▼) and offers the match data as a download.
 */
import { computed, ref, watch } from 'vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
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
  },
  /** Start open (closed on the Matches page) */
  defaultOpen: {
    type: Boolean,
    default: false
  }
})

defineEmits(['download'])

// Closed by default; a newly opened match starts closed again
const open = ref(props.defaultOpen)
watch(() => props.match.matchId, () => { open.value = props.defaultOpen })

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

const upCount = computed(() => stats.value.filter((s) => s.trend === 'up').length)
const downCount = computed(() => stats.value.filter((s) => s.trend === 'down').length)

const summary = computed(() => {
  const up = upCount.value
  const down = downCount.value
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
  padding-block: 0;
}

.stat-snapshot__toggle {
  display: flex;
  align-items: center;
  gap: 1rem;
  width: 100%;
  min-height: 4rem;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.stat-snapshot__toggle:focus-visible {
  outline: none;
  border-radius: 0.75rem;
  box-shadow: var(--shadow-focus);
}

.stat-snapshot__title {
  flex-grow: 1;
  font-family: var(--font-display);
  font-size: 1.0625rem;
  font-weight: 600;
  line-height: 1.3;
}

.stat-snapshot__toggle:hover .stat-snapshot__title {
  color: var(--color-positive-text-strong);
}

.stat-snapshot__count {
  font-size: 0.8125rem;
  font-weight: 500;
  white-space: nowrap;
}

.stat-snapshot__chevron {
  color: var(--color-text-secondary);
}

.stat-snapshot--open .stat-snapshot__chevron {
  transform: rotate(180deg);
  color: var(--color-text);
}

.stat-snapshot__body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-bottom: 1.75rem;
}

.stat-snapshot__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

/* Up and down notes: purple for good, orange for needs work */
.stat-comparison.up { color: var(--color-positive-text); }
.stat-comparison.down { color: var(--color-warn-text); }

.stat-snapshot__download {
  align-self: flex-start;
}

@media (min-width: 1280px) {
  .stats-grid {
    grid-template-columns: repeat(5, minmax(0, 1fr));
  }
}

@media (max-width: 599px) {
  .stat-snapshot__toggle {
    gap: 0.625rem;
  }
}
</style>
