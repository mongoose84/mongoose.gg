<template>
  <div class="impact-stats">
    <h3 class="section-title">Personal Impact</h3>
    <div class="impact-grid">
      <div
        v-for="stat in impactStats"
        :key="stat.label"
        class="impact-item"
        :class="stat.sentiment"
      >
        <div class="impact-header">
          <span class="impact-label">{{ stat.label }}</span>
          <span v-if="stat.sentiment" class="sentiment-indicator" :class="stat.sentiment">
            {{ stat.sentiment === 'positive' ? '↑' : stat.sentiment === 'negative' ? '↓' : '' }}
          </span>
        </div>
        <span class="impact-value">{{ stat.value }}</span>
        <span class="impact-description">{{ stat.description }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

// Check if role is support/utility
const isSupport = computed(() => {
  const role = props.match.role?.toUpperCase()
  return role === 'UTILITY' || role === 'SUPPORT'
})

const impactStats = computed(() => {
  const m = props.match

  // 2. Gold Diff @15 - "Who won laning phase?" (same for all roles)
  // Note: goldDiffAt15 can be null if:
  // - No lane opponent detected
  // - Game ended before 15 minutes
  // - 15m checkpoint wasn't persisted
  const goldDiff = m.goldDiffAt15
  const hasGoldDiff = goldDiff !== null && goldDiff !== undefined
  const gameEndedEarly = m.gameDurationSec < 15 * 60
  const goldDiffSentiment = !hasGoldDiff
    ? 'neutral'
    : goldDiff >= 500
      ? 'positive'
      : goldDiff <= -500
        ? 'negative'
        : 'neutral'
  const goldDiffDescription = !hasGoldDiff
    ? (gameEndedEarly ? 'Game ended early' : 'No data')
    : goldDiff >= 500
      ? 'Won lane'
      : goldDiff <= -500
        ? 'Lost lane'
        : 'Even lane'

  const goldStat = {
    label: 'Gold @15',
    value: hasGoldDiff
      ? `${goldDiff >= 0 ? '+' : ''}${goldDiff.toLocaleString()}`
      : 'N/A',
    sentiment: goldDiffSentiment,
    description: goldDiffDescription
  }

  if (isSupport.value) {
    // SUPPORT METRICS

    // 1. Assist Participation - "Did I enable my team?"
    // Assists / Team Kills (how many team kills did I assist?)
    const assistPart = m.teamKills > 0
      ? (m.assists / m.teamKills) * 100
      : 0
    const assistSentiment = assistPart >= 60 ? 'positive' : assistPart < 40 ? 'negative' : 'neutral'
    const assistDescription = assistPart >= 60
      ? 'High enabler'
      : assistPart < 40
        ? 'Low involvement'
        : 'Average involvement'

    // 3. Vision Score - "Did I control vision?"
    // Vision score per minute is a good metric
    const gameMins = m.gameDurationSec / 60
    const visionPerMin = gameMins > 0 ? m.visionScore / gameMins : 0
    // Good support: 2.5+ vision/min, Average: 1.5-2.5, Low: <1.5
    const visionSentiment = visionPerMin >= 2.5 ? 'positive' : visionPerMin < 1.5 ? 'negative' : 'neutral'
    const visionDescription = visionPerMin >= 2.5
      ? 'Great vision'
      : visionPerMin < 1.5
        ? 'Low vision'
        : 'Average vision'

    return [
      {
        label: 'Assist Part.',
        value: `${assistPart.toFixed(0)}%`,
        sentiment: assistSentiment,
        description: assistDescription
      },
      goldStat,
      {
        label: 'Vision/min',
        value: visionPerMin.toFixed(1),
        sentiment: visionSentiment,
        description: visionDescription
      }
    ]
  } else {
    // NON-SUPPORT METRICS

    // 1. Kill Participation - "Was I part of the plays?"
    // Typical KP is ~20% (1/5 players). 30%+ is high, <25% is low.
    const kp = m.killParticipation
    const kpSentiment = kp >= 30 ? 'positive' : kp < 25 ? 'negative' : 'neutral'
    const kpDescription = kp >= 30
      ? 'High involvement'
      : kp < 25
        ? 'Low involvement'
        : 'Average involvement'

    // 3. Damage/Gold Efficiency - "Did I carry with my gold?"
    const damageGoldRatio = m.goldEarned > 0
      ? (m.damageDealt / m.goldEarned)
      : 0
    const efficiencySentiment = damageGoldRatio >= 1.5
      ? 'positive'
      : damageGoldRatio < 0.8
        ? 'negative'
        : 'neutral'
    const efficiencyDescription = damageGoldRatio >= 1.5
      ? 'Efficient carry'
      : damageGoldRatio < 0.8
        ? 'Low output'
        : 'Average output'

    return [
      {
        label: 'Kill Participation',
        value: `${kp.toFixed(0)}%`,
        sentiment: kpSentiment,
        description: kpDescription
      },
      goldStat,
      {
        label: 'Dmg/Gold',
        value: damageGoldRatio.toFixed(2),
        sentiment: efficiencySentiment,
        description: efficiencyDescription
      }
    ]
  }
})
</script>

<style scoped>
.impact-stats {
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

.impact-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-xs);
}

.impact-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--spacing-sm);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.impact-item.positive {
  border-color: rgba(34, 197, 94, 0.3);
  background: rgba(34, 197, 94, 0.05);
}

.impact-item.negative {
  border-color: rgba(239, 68, 68, 0.3);
  background: rgba(239, 68, 68, 0.05);
}

.impact-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.impact-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.sentiment-indicator {
  font-size: 10px;
  font-weight: var(--font-weight-bold);
}

.sentiment-indicator.positive {
  color: #22c55e;
}

.sentiment-indicator.negative {
  color: #ef4444;
}

.impact-value {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.impact-description {
  font-size: 10px;
  color: var(--color-text-secondary);
}
</style>

