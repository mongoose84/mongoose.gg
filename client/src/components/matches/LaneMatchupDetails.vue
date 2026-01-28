<template>
  <div class="lane-matchup-details">
    <!-- Phase 1: Laning (0-15m) -->
    <div class="phase-section">
      <h4 class="phase-title">🏰 Laning Phase (0-15m)</h4>
      <div class="stats-grid">
        <div class="stat-row">
          <span class="stat-label">Gold Diff</span>
          <div class="stat-comparison">
            <span class="stat-value ally" :class="goldDiffSentiment">
              {{ formatGoldDiff(matchup.allyParticipant.goldDiffAt15) }}
            </span>
            <div class="diff-bar-wrapper">
              <div class="diff-bar ally" :style="{ width: allyGoldBarWidth + '%' }"></div>
              <div class="diff-bar enemy" :style="{ width: enemyGoldBarWidth + '%' }"></div>
            </div>
          </div>
        </div>
        <div class="stat-row">
          <span class="stat-label">CS Diff</span>
          <div class="stat-comparison">
            <span class="stat-value" :class="csDiffSentiment">
              {{ formatCsDiff(matchup.allyParticipant.csDiffAt15) }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Phase 2: Game Impact -->
    <div class="phase-section">
      <h4 class="phase-title">⚔️ Game Impact</h4>
      <div class="stats-grid">
        <div class="stat-row">
          <span class="stat-label">Damage Share</span>
          <div class="stat-comparison">
            <span class="stat-value ally">{{ formatPercent(matchup.allyParticipant.damageShare) }}</span>
            <span class="vs-separator">vs</span>
            <span class="stat-value enemy">{{ formatPercent(matchup.enemyParticipant.damageShare) }}</span>
          </div>
        </div>
        <div class="stat-row">
          <span class="stat-label">Kill Part.</span>
          <div class="stat-comparison">
            <span class="stat-value ally">{{ formatPercent(matchup.allyParticipant.killParticipation) }}</span>
            <span class="vs-separator">vs</span>
            <span class="stat-value enemy">{{ formatPercent(matchup.enemyParticipant.killParticipation) }}</span>
          </div>
        </div>
        <div class="stat-row">
          <span class="stat-label">Vision Score</span>
          <div class="stat-comparison">
            <span class="stat-value ally">{{ matchup.allyParticipant.visionScore }}</span>
            <span class="vs-separator">vs</span>
            <span class="stat-value enemy">{{ matchup.enemyParticipant.visionScore }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Contextual Insight -->
    <div class="insight-section">
      <p class="insight-text">{{ contextualInsight }}</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  matchup: {
    type: Object,
    required: true
  }
})

const goldDiff = computed(() => props.matchup.allyParticipant.goldDiffAt15 || 0)
const csDiff = computed(() => props.matchup.allyParticipant.csDiffAt15 || 0)

const goldDiffSentiment = computed(() => {
  if (goldDiff.value >= 500) return 'positive'
  if (goldDiff.value <= -500) return 'negative'
  return 'neutral'
})

const csDiffSentiment = computed(() => {
  if (csDiff.value >= 10) return 'positive'
  if (csDiff.value <= -10) return 'negative'
  return 'neutral'
})

// Gold bar percentages (max bar at ±2000 gold)
const maxGold = 2000
const allyGoldBarWidth = computed(() => {
  if (goldDiff.value > 0) return Math.min((goldDiff.value / maxGold) * 100, 100)
  return 0
})
const enemyGoldBarWidth = computed(() => {
  if (goldDiff.value < 0) return Math.min((Math.abs(goldDiff.value) / maxGold) * 100, 100)
  return 0
})

// Generate contextual insight based on matchup data
const contextualInsight = computed(() => {
  const ally = props.matchup.allyParticipant
  const enemy = props.matchup.enemyParticipant
  const winner = props.matchup.laneWinner

  if (winner === 'ally') {
    if (ally.killParticipation < enemy.killParticipation) {
      return `Won lane (+${goldDiff.value}g), but ${enemy.championName} had higher team impact with ${formatPercent(enemy.killParticipation)} KP.`
    }
    if (ally.visionScore < enemy.visionScore - 5) {
      return `Strong laning phase, but vision control was lacking compared to the opponent.`
    }
    return `Dominated the laning phase with a ${goldDiff.value}g lead. Translated well into game impact.`
  } else if (winner === 'enemy') {
    if (ally.killParticipation > enemy.killParticipation) {
      return `Lost lane, but stayed relevant with ${formatPercent(ally.killParticipation)} kill participation.`
    }
    return `Struggled in lane against ${enemy.championName}. Consider adjusting playstyle or itemization.`
  }
  return `Even lane matchup. Both players had similar impact on the game.`
})

function formatGoldDiff(diff) {
  if (diff === null || diff === undefined) return 'N/A'
  const sign = diff >= 0 ? '+' : ''
  return `${sign}${diff.toLocaleString()}`
}

function formatCsDiff(diff) {
  if (diff === null || diff === undefined) return 'N/A'
  const sign = diff >= 0 ? '+' : ''
  return `${sign}${diff} CS`
}

function formatPercent(value) {
  // Values from database are already percentages (e.g., 25.50 means 25.50%)
  return `${value.toFixed(0)}%`
}
</script>

<style scoped>
.lane-matchup-details {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.phase-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.phase-title {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  margin: 0;
}

.stats-grid {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.stat-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--color-elevated);
  border-radius: var(--radius-sm);
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  min-width: 80px;
}

.stat-comparison {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  flex: 1;
  justify-content: flex-end;
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  min-width: 40px;
  text-align: center;
}

.stat-value.ally { color: #3b82f6; }
.stat-value.enemy { color: #ef4444; }
.stat-value.positive { color: #22c55e; }
.stat-value.negative { color: #ef4444; }
.stat-value.neutral { color: var(--color-text); }

.vs-separator {
  font-size: 10px;
  color: var(--color-text-secondary);
}

.diff-bar-wrapper {
  display: flex;
  width: 80px;
  height: 6px;
  background: var(--color-border);
  border-radius: 3px;
  overflow: hidden;
}

.diff-bar {
  height: 100%;
  transition: width 0.3s ease;
}

.diff-bar.ally { background: #22c55e; }
.diff-bar.enemy { background: #ef4444; }

.insight-section {
  padding: var(--spacing-sm);
  background: var(--color-elevated);
  border-radius: var(--radius-sm);
  border-left: 3px solid var(--color-primary);
}

.insight-text {
  font-size: var(--font-size-xs);
  color: var(--color-text);
  margin: 0;
  line-height: 1.5;
  font-style: italic;
}
</style>

