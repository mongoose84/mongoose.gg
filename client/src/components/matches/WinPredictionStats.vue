<template>
  <section class="mp-card kpi" aria-labelledby="kpi-title" data-testid="win-prediction-stats">
    <header class="kpi__header">
      <h3 id="kpi-title" class="mp-card-title section-title">{{ title }}</h3>
      <p class="kpi__caption subtitle">The stats that decide most matches, against your recent matches in this role</p>
    </header>

    <ul class="mp-stat-grid kpi-grid">
      <li
        v-for="tile in tiles"
        :key="tile.key"
        class="mp-stat kpi-tile"
        :class="tile.sentiment"
        :data-testid="`kpi-tile-${tile.key}`"
      >
        <span class="mp-stat__label kpi-label">{{ tile.label }}</span>
        <span class="mp-stat__value kpi-value">{{ tile.value }}</span>
        <span v-if="tile.description" class="mp-stat__note kpi-description" :class="sentimentText(tile.sentiment)">
          <span v-if="tile.sentiment !== 'neutral'" aria-hidden="true">{{ tile.sentiment === 'positive' ? '▲' : '▼' }} </span>{{ tile.description }}
        </span>
      </li>
    </ul>
  </section>
</template>

<script setup>
/**
 * The six stats that predict a win, each marked good (purple ▲) or needs work (orange ▼)
 * against the player's own baseline. The title states the takeaway.
 */
import { computed } from 'vue'
import { formatSigned } from '@/utils/matchesSummary'

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
  return `${formatSigned(props.match.deaths - props.baseline.avgDeaths, 1)} vs your average`
})

// Gold lead at 15
const hasGoldDiff = computed(() => props.match.goldDiffAt15 !== null && props.match.goldDiffAt15 !== undefined)
const matchEndedEarly = computed(() => props.match.gameDurationSec < 15 * 60)

const goldSentiment = computed(() => {
  if (!hasGoldDiff.value) return 'neutral'
  if (props.match.goldDiffAt15 >= 500) return 'positive'
  if (props.match.goldDiffAt15 <= -500) return 'negative'
  return 'neutral'
})

const goldValue = computed(() => (hasGoldDiff.value ? formatSigned(props.match.goldDiffAt15) : '—'))

const goldDescription = computed(() => {
  if (!hasGoldDiff.value) return matchEndedEarly.value ? 'Ended before 15 minutes' : 'Not recorded'
  if (props.match.goldDiffAt15 >= 500) return 'Won lane'
  if (props.match.goldDiffAt15 <= -500) return 'Lost lane'
  return 'Even lane'
})

// Dragon participation
const dragonParticipationRate = computed(() => {
  const { teamDragons, dragonsParticipated } = props.match
  if (!teamDragons) return 0
  return dragonsParticipated / teamDragons
})

const dragonSentiment = computed(() => {
  if (!props.match.teamDragons) return 'neutral'
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
  if (!props.match.teamDragons) return null
  if (dragonParticipationRate.value >= 2 / 3) return 'High involvement'
  if (dragonParticipationRate.value === 0) return 'Low involvement'
  return null
})

// CS per minute
const csSentiment = computed(() => {
  if (isSupport.value || !props.baseline) return 'neutral'
  const diff = props.match.csPerMin - props.baseline.avgCsPerMin
  if (diff > 0.5) return 'positive'
  if (diff < -0.5) return 'negative'
  return 'neutral'
})

const csComparison = computed(() => {
  if (isSupport.value || !props.baseline) return null
  return `${formatSigned(props.match.csPerMin - props.baseline.avgCsPerMin, 1)} vs your average`
})

// Vision score (adjusted for match length)
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
  return `${formatSigned(visionDiff.value)} vs your average`
})

// Deaths before 10 minutes
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

const tiles = computed(() => [
  { key: 'deaths', label: 'Deaths', value: props.match.deaths, sentiment: deathsSentiment.value, description: deathsComparison.value },
  { key: 'gold15', label: 'Gold lead at 15', value: goldValue.value, sentiment: goldSentiment.value, description: goldDescription.value },
  { key: 'dragon', label: 'Dragons you joined', value: dragonValue.value, sentiment: dragonSentiment.value, description: dragonDescription.value },
  { key: 'cspm', label: 'CS per minute', value: props.match.csPerMin.toFixed(1), sentiment: csSentiment.value, description: csComparison.value },
  { key: 'vision', label: 'Vision score', value: props.match.visionScore, sentiment: visionSentiment.value, description: visionComparison.value },
  { key: 'deaths-pre10', label: 'Deaths before 10 min', value: props.match.deathsPre10, sentiment: earlyDeathsSentiment.value, description: earlyDeathsDescription.value }
])

const title = computed(() => {
  const good = tiles.value.filter((t) => t.sentiment === 'positive').length
  const bad = tiles.value.filter((t) => t.sentiment === 'negative').length
  if (good) return `${good} of 6 match-deciding stats went your way`
  if (bad) return 'None of the 6 match-deciding stats went your way'
  return 'Your match-deciding stats were close to usual'
})

function sentimentText(sentiment) {
  if (sentiment === 'positive') return 'mp-up'
  if (sentiment === 'negative') return 'mp-down'
  return null
}
</script>

<style scoped>
.kpi {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.kpi__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.kpi__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

/* Good notes purple, needs-work notes orange (over the StatTile's neutral note colour) */
.kpi-description.mp-up { color: var(--color-positive-text); }
.kpi-description.mp-down { color: var(--color-warn-text); }
</style>
