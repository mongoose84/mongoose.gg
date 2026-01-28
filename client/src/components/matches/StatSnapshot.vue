<template>
  <div class="stat-snapshot">
    <button type="button" class="section-header" @click="expanded = !expanded">
      <h3 class="section-title">Stats</h3>
      <span class="expand-icon" :class="{ expanded }">›</span>
    </button>
    <div v-if="expanded" class="stats-grid">
      <div class="stat-item" v-for="stat in stats" :key="stat.label" :class="stat.trend">
        <div class="stat-header">
          <span class="stat-label">{{ stat.label }}</span>
          <span v-if="stat.trend" class="trend-arrow" :class="stat.trend">
            {{ stat.trend === 'up' ? '↑' : stat.trend === 'down' ? '↓' : '' }}
          </span>
        </div>
        <span class="stat-value">{{ stat.value }}</span>
        <span v-if="stat.comparison" class="stat-comparison" :class="stat.trend">
          {{ stat.comparison }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const expanded = ref(false)

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
      return `${pctDiff >= 0 ? '+' : ''}${pctDiff.toFixed(0)}% vs avg`
    } else if (format === 'diff' && Math.abs(diff) >= threshold) {
      return `${diff >= 0 ? '+' : ''}${diff.toFixed(1)} vs avg`
    } else if (format === 'int' && Math.abs(diff) >= threshold) {
      return `${diff >= 0 ? '+' : ''}${Math.round(diff)} vs avg`
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
      return `${pctDiff >= 0 ? '+' : ''}${pctDiff.toFixed(0)}% vs expected`
    } else if (format === 'diff' && Math.abs(diff) >= threshold) {
      return `${diff >= 0 ? '+' : ''}${diff.toFixed(1)} vs expected`
    } else if (format === 'int' && Math.abs(diff) >= threshold) {
      return `${diff >= 0 ? '+' : ''}${Math.round(diff)} vs expected`
    }
    return null
  }

  const kda = getKda()

  return [
    {
      label: 'KDA',
      value: `${m.kills}/${m.deaths}/${m.assists}`,
      trend: null,
      comparison: null
    },
    {
      label: 'KDA Ratio',
      value: kda.toFixed(2),
      trend: b ? getTrend(kda, b.avgKda, 0.15) : null,
      comparison: b ? getComparison(kda, b.avgKda, 0.3, 'diff') : null
    },
    {
      label: 'Kill Part.',
      value: `${m.killParticipation.toFixed(0)}%`,
      trend: b ? getTrend(m.killParticipation, b.avgKillParticipation, 0.1) : null,
      comparison: b ? getComparison(m.killParticipation, b.avgKillParticipation, 5, 'int') : null
    },
    {
      label: 'Deaths <10m',
      value: m.deathsPre10.toString(),
      trend: m.deathsPre10 === 0 ? 'up' : m.deathsPre10 >= 2 ? 'down' : null,
      comparison: m.deathsPre10 === 0 ? 'Clean early game' : null
    },
    {
      label: 'Damage Dealt',
      value: formatNumber(m.damageDealt),
      trend: b ? getTrendDurationAdjusted(m.damageDealt, b.avgDamageDealt, 0.15) : null,
      comparison: b ? getComparisonDurationAdjusted(m.damageDealt, b.avgDamageDealt, 10, 'pct') : null
    },
    {
      label: 'Dmg Share',
      value: `${m.damageShare.toFixed(0)}%`,
      trend: m.damageShare >= 25 ? 'up' : m.damageShare < 15 ? 'down' : null,
      comparison: m.damageShare >= 25 ? 'Carry performance' : null
    },
    {
      label: 'CS',
      value: m.creepScore.toString(),
      trend: b ? getTrendDurationAdjusted(m.creepScore, b.avgCreepScore, 0.1) : null,
      comparison: b ? getComparisonDurationAdjusted(m.creepScore, b.avgCreepScore, 10, 'int') : null
    },
    {
      label: 'CS/min',
      value: m.csPerMin.toFixed(1),
      trend: b ? getTrend(m.csPerMin, b.avgCsPerMin, 0.1) : null,
      comparison: b ? getComparison(m.csPerMin, b.avgCsPerMin, 0.3, 'diff') : null
    },
    {
      label: 'Gold',
      value: formatNumber(m.goldEarned),
      trend: b ? getTrendDurationAdjusted(m.goldEarned, b.avgGoldEarned, 0.1) : null,
      comparison: b ? getComparisonDurationAdjusted(m.goldEarned, b.avgGoldEarned, 10, 'pct') : null
    },
    {
      label: 'Gold/min',
      value: m.goldPerMin.toFixed(0),
      trend: b ? getTrend(m.goldPerMin, b.avgGoldPerMin, 0.1) : null,
      comparison: b ? getComparison(m.goldPerMin, b.avgGoldPerMin, 15, 'int') : null
    },
    {
      label: 'Vision Score',
      value: m.visionScore.toString(),
      trend: b ? getTrendDurationAdjusted(m.visionScore, b.avgVisionScore, 0.15) : null,
      comparison: b ? getComparisonDurationAdjusted(m.visionScore, b.avgVisionScore, 3, 'int') : null
    },
    {
      label: 'Dmg Taken',
      value: formatNumber(m.damageTaken),
      trend: b ? getTrendDurationAdjusted(m.damageTaken, b.avgDamageTaken, 0.15) : null,
      comparison: b ? getComparisonDurationAdjusted(m.damageTaken, b.avgDamageTaken, 10, 'pct') : null
    }
  ]
})

function formatNumber(num) {
  if (num >= 1000) {
    return (num / 1000).toFixed(1) + 'k'
  }
  return num.toString()
}
</script>

<style scoped>
.stat-snapshot {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 0;
  background: none;
  border: none;
  cursor: pointer;
  text-align: left;
}

.section-header:hover .section-title {
  color: var(--color-text);
}

.section-title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  margin: 0;
  transition: color 0.15s ease;
}

.expand-icon {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  transition: transform 0.2s ease;
  transform: rotate(0deg);
}

.expand-icon.expanded {
  transform: rotate(90deg);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-xs);
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.stat-item.up {
  border-color: rgba(34, 197, 94, 0.3);
  background: rgba(34, 197, 94, 0.05);
}

.stat-item.down {
  border-color: rgba(239, 68, 68, 0.3);
  background: rgba(239, 68, 68, 0.05);
}

.stat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.trend-arrow {
  font-size: 10px;
  font-weight: var(--font-weight-bold);
}

.trend-arrow.up {
  color: #22c55e;
}

.trend-arrow.down {
  color: #ef4444;
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.stat-comparison {
  font-size: 10px;
  color: var(--color-text-secondary);
}

.stat-comparison.up {
  color: #22c55e;
}

.stat-comparison.down {
  color: #ef4444;
}
</style>

