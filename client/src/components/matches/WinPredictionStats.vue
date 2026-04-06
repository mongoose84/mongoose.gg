<template>
  <section class="win-prediction-stats" data-testid="win-prediction-stats">
    <header class="stats-header">
      <h3 class="section-title">Key Performance Indicators</h3>
      <span class="subtitle">Metrics that most predict winning</span>
    </header>
    <div class="kpi-grid">
      <!-- Deaths -->
      <div class="kpi-tile" :class="deathsSentiment" data-testid="kpi-tile-deaths">
        <div class="kpi-header">
          <span class="kpi-label">Deaths</span>
          <span v-if="deathsSentiment !== 'neutral'" class="sentiment-indicator" :class="deathsSentiment">
            {{ deathsSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ match.deaths }}</span>
        <span v-if="deathsComparison" class="kpi-description">{{ deathsComparison }}</span>
      </div>

      <!-- Gold @15 -->
      <div class="kpi-tile" :class="goldSentiment" data-testid="kpi-tile-gold15">
        <div class="kpi-header">
          <span class="kpi-label">Gold @15</span>
          <span v-if="goldSentiment !== 'neutral'" class="sentiment-indicator" :class="goldSentiment">
            {{ goldSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ goldValue }}</span>
        <span class="kpi-description">{{ goldDescription }}</span>
      </div>

      <!-- Dragon Participation -->
      <div class="kpi-tile" :class="dragonSentiment" data-testid="kpi-tile-dragon">
        <div class="kpi-header">
          <span class="kpi-label">Dragon Part.</span>
          <span v-if="dragonSentiment !== 'neutral'" class="sentiment-indicator" :class="dragonSentiment">
            {{ dragonSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ dragonValue }}</span>
        <span v-if="dragonDescription" class="kpi-description">{{ dragonDescription }}</span>
      </div>

      <!-- CS/min -->
      <div class="kpi-tile" :class="csSentiment" data-testid="kpi-tile-cspm">
        <div class="kpi-header">
          <span class="kpi-label">CS/min</span>
          <span v-if="csSentiment !== 'neutral'" class="sentiment-indicator" :class="csSentiment">
            {{ csSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ match.csPerMin.toFixed(1) }}</span>
        <span v-if="csComparison" class="kpi-description">{{ csComparison }}</span>
      </div>

      <!-- Vision Score -->
      <div class="kpi-tile" :class="visionSentiment" data-testid="kpi-tile-vision">
        <div class="kpi-header">
          <span class="kpi-label">Vision Score</span>
          <span v-if="visionSentiment !== 'neutral'" class="sentiment-indicator" :class="visionSentiment">
            {{ visionSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ match.visionScore }}</span>
        <span v-if="visionComparison" class="kpi-description">{{ visionComparison }}</span>
      </div>

      <!-- Deaths Before 10m -->
      <div class="kpi-tile" :class="earlyDeathsSentiment" data-testid="kpi-tile-deaths-pre10">
        <div class="kpi-header">
          <span class="kpi-label">Deaths &lt;10m</span>
          <span v-if="earlyDeathsSentiment !== 'neutral'" class="sentiment-indicator" :class="earlyDeathsSentiment">
            {{ earlyDeathsSentiment === 'positive' ? '↑' : '↓' }}
          </span>
        </div>
        <span class="kpi-value">{{ match.deathsPre10 }}</span>
        <span v-if="earlyDeathsDescription" class="kpi-description">{{ earlyDeathsDescription }}</span>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  match: { type: Object, required: true },
  baseline: { type: Object, default: null }
})

const isSupport = computed(() => {
  const role = props.match.role?.toUpperCase()
  return role === 'UTILITY' || role === 'SUPPORT'
})

// Deaths
const deathsSentiment = computed(() => {
  if (!props.baseline) return 'neutral'
  const avg = props.baseline.avgDeaths
  if (props.match.deaths < avg - 1) return 'positive'
  if (props.match.deaths > avg + 1) return 'negative'
  return 'neutral'
})

const deathsComparison = computed(() => {
  if (!props.baseline) return null
  const diff = props.match.deaths - props.baseline.avgDeaths
  return `${diff >= 0 ? '+' : ''}${diff} vs avg`
})

// Gold @15
const hasGoldDiff = computed(() => props.match.goldDiffAt15 !== null && props.match.goldDiffAt15 !== undefined)
const gameEndedEarly = computed(() => props.match.gameDurationSec < 15 * 60)

const goldSentiment = computed(() => {
  if (!hasGoldDiff.value) return 'neutral'
  if (props.match.goldDiffAt15 >= 500) return 'positive'
  if (props.match.goldDiffAt15 <= -500) return 'negative'
  return 'neutral'
})

const goldValue = computed(() => {
  if (!hasGoldDiff.value) return 'N/A'
  const diff = props.match.goldDiffAt15
  return `${diff >= 0 ? '+' : ''}${diff.toLocaleString()}`
})

const goldDescription = computed(() => {
  if (!hasGoldDiff.value) return gameEndedEarly.value ? 'Game ended early' : 'No data'
  if (props.match.goldDiffAt15 >= 500) return 'Won lane'
  if (props.match.goldDiffAt15 <= -500) return 'Lost lane'
  return 'Even lane'
})

// Dragon Participation
const dragonParticipationRate = computed(() => {
  const { teamDragons, dragonsParticipated } = props.match
  if (!teamDragons) return 0
  return dragonsParticipated / teamDragons
})

const dragonSentiment = computed(() => {
  const { teamDragons } = props.match
  if (!teamDragons) return 'neutral'
  if (dragonParticipationRate.value >= 2 / 3) return 'positive'
  if (dragonParticipationRate.value === 0) return 'negative'
  return 'neutral'
})

const dragonValue = computed(() => {
  const { teamDragons, dragonsParticipated } = props.match
  if (!teamDragons) return 'No dragons'
  const pct = Math.round(dragonParticipationRate.value * 100)
  return `${dragonsParticipated}/${teamDragons} (${pct}%)`
})

const dragonDescription = computed(() => {
  const { teamDragons } = props.match
  if (!teamDragons) return null
  if (dragonParticipationRate.value >= 2 / 3) return 'High involvement'
  if (dragonParticipationRate.value === 0) return 'Low involvement'
  return null
})

// CS/min
const csSentiment = computed(() => {
  if (isSupport.value || !props.baseline) return 'neutral'
  const diff = props.match.csPerMin - props.baseline.avgCsPerMin
  if (diff > 0.5) return 'positive'
  if (diff < -0.5) return 'negative'
  return 'neutral'
})

const csComparison = computed(() => {
  if (isSupport.value || !props.baseline) return null
  const diff = props.match.csPerMin - props.baseline.avgCsPerMin
  return `${diff >= 0 ? '+' : ''}${diff.toFixed(1)} vs avg`
})

// Vision Score (duration-adjusted)
const visionExpected = computed(() => {
  if (!props.baseline || !props.baseline.avgGameDurationSec) return null
  return props.baseline.avgVisionScore * (props.match.gameDurationSec / props.baseline.avgGameDurationSec)
})

const visionDiff = computed(() => {
  if (visionExpected.value === null) return 0
  return props.match.visionScore - visionExpected.value
})

const visionSentiment = computed(() => {
  if (!props.baseline || visionExpected.value === null) return 'neutral'
  const pct = visionExpected.value > 0 ? visionDiff.value / visionExpected.value : 0
  if (pct > 0.15) return 'positive'
  if (pct < -0.15) return 'negative'
  return 'neutral'
})

const visionComparison = computed(() => {
  if (!props.baseline || visionExpected.value === null) return null
  const diff = Math.round(visionDiff.value)
  return `${diff >= 0 ? '+' : ''}${diff} vs avg`
})

// Deaths Before 10m
const earlyDeathsSentiment = computed(() => {
  const d = props.match.deathsPre10
  if (d === 0) return 'positive'
  if (d >= 2) return 'negative'
  return 'neutral'
})

const earlyDeathsDescription = computed(() => {
  const d = props.match.deathsPre10
  if (d === 0) return 'Safe early game'
  if (d >= 2) return 'Risky early game'
  return null
})
</script>

<style scoped>
.win-prediction-stats {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.stats-header {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.section-title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.subtitle {
  font-size: 10px;
  color: var(--color-text-secondary);
}

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-xs);
}

.kpi-tile {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--spacing-sm);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.kpi-tile.positive {
  border-color: rgba(34, 197, 94, 0.3);
  background: rgba(34, 197, 94, 0.05);
}

.kpi-tile.negative {
  border-color: rgba(239, 68, 68, 0.3);
  background: rgba(239, 68, 68, 0.05);
}

.kpi-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.kpi-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.sentiment-indicator {
  font-size: 10px;
  font-weight: var(--font-weight-bold);
}

.sentiment-indicator.positive {
  color: var(--color-success);
}

.sentiment-indicator.negative {
  color: var(--color-error);
}

.kpi-value {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.kpi-description {
  font-size: 10px;
  color: var(--color-text-secondary);
}
</style>

