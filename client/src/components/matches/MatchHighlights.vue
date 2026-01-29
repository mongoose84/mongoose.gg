<template>
  <div class="match-highlights">
    <h3 class="section-title">Key Highlights</h3>
    <div class="highlights-grid">
      <HighlightTile
        v-for="(highlight, index) in computedHighlights"
        :key="index"
        :statName="highlight.statName"
        :insightText="highlight.insightText"
        :type="highlight.type"
        :icon="highlight.icon"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import HighlightTile from './HighlightTile.vue'
import { formatNumber } from '@/utils/formatters'

const props = defineProps({
  match: {
    type: Object,
    required: true
  },
  baseline: {
    type: Object,
    default: null
    // Expected: RoleBaseline { avgKills, avgDeaths, avgDamageDealt, avgCsPerMin, avgVisionScore, ... }
  }
})

// Compute 4 highlights by comparing match stats to baseline
const computedHighlights = computed(() => {
  const m = props.match
  const b = props.baseline
  const highlights = []

  // If no baseline, show raw stats without comparison
  if (!b || b.gamesCount === 0) {
    return [
      { statName: 'KDA', insightText: `${m.kills}/${m.deaths}/${m.assists}`, type: 'neutral', icon: 'kda' },
      { statName: 'Damage', insightText: formatNumber(m.damageDealt), type: 'neutral', icon: 'damage' },
      { statName: 'CS/min', insightText: m.csPerMin.toFixed(1), type: 'neutral', icon: 'cs' },
      { statName: 'Vision', insightText: m.visionScore.toString(), type: 'neutral', icon: 'vision' }
    ]
  }

  // KDA comparison
  const kda = m.deaths === 0 ? (m.kills + m.assists) : (m.kills + m.assists) / m.deaths
  const kdaDiff = kda - b.avgKda
  highlights.push({
    statName: 'KDA',
    insightText: kdaDiff >= 0.5 ? `${kda.toFixed(2)} (+${kdaDiff.toFixed(1)} vs avg)` : `${kda.toFixed(2)} KDA`,
    type: kdaDiff >= 0.5 ? 'positive' : kdaDiff <= -0.5 ? 'negative' : 'neutral',
    icon: 'kda'
  })

  // Damage comparison
  const damageDiff = m.damageDealt - b.avgDamageDealt
  const damagePct = b.avgDamageDealt > 0 ? (damageDiff / b.avgDamageDealt) * 100 : 0
  highlights.push({
    statName: 'Damage',
    insightText: Math.abs(damagePct) >= 10 
      ? `${formatNumber(m.damageDealt)} (${damagePct >= 0 ? '+' : ''}${damagePct.toFixed(0)}%)`
      : formatNumber(m.damageDealt),
    type: damagePct >= 15 ? 'positive' : damagePct <= -15 ? 'negative' : 'neutral',
    icon: 'damage'
  })

  // CS/min comparison
  const csDiff = m.csPerMin - b.avgCsPerMin
  highlights.push({
    statName: 'CS/min',
    insightText: Math.abs(csDiff) >= 0.5 
      ? `${m.csPerMin.toFixed(1)} (${csDiff >= 0 ? '+' : ''}${csDiff.toFixed(1)})`
      : m.csPerMin.toFixed(1),
    type: csDiff >= 0.5 ? 'positive' : csDiff <= -0.5 ? 'negative' : 'neutral',
    icon: 'cs'
  })

  // Vision score comparison
  const visionDiff = m.visionScore - b.avgVisionScore
  highlights.push({
    statName: 'Vision',
    insightText: Math.abs(visionDiff) >= 5 
      ? `${m.visionScore} (${visionDiff >= 0 ? '+' : ''}${visionDiff.toFixed(0)})`
      : m.visionScore.toString(),
    type: visionDiff >= 5 ? 'positive' : visionDiff <= -5 ? 'negative' : 'neutral',
    icon: 'vision'
  })

  return highlights
})
</script>

<style scoped>
.match-highlights {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.highlights-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-sm);
}

@media (max-width: 640px) {
  .highlights-grid {
    grid-template-columns: 1fr;
  }
}
</style>

