<template>
  <div class="team-comparison">
    <h3 class="section-title">Team Summary</h3>
    <div class="comparison-grid">
      <!-- Total Damage -->
      <div class="comparison-row damage-row">
        <span class="metric-label">Total Damage</span>
        <div class="bar-wrapper team-bar">
          <div class="bar team" :style="{ width: teamDamagePercent + '%' }"></div>
        </div>
        <div class="damage-values">
          <span class="bar-value team-value">{{ formatNumber(match.teamTotalDamage) }}</span>
          <span class="bar-value enemy-value">{{ formatNumber(match.enemyTeamTotalDamage) }}</span>
        </div>
        <div class="bar-wrapper enemy-bar">
          <div class="bar enemy" :style="{ width: enemyDamagePercent + '%' }"></div>
        </div>
      </div>

      <!-- Gold @ 15 -->
      <div class="comparison-row">
        <span class="metric-label">Gold @ 15</span>
        <div class="value-cell" :class="teamHasGoldLead ? 'positive' : 'empty'">
          <span v-if="teamHasGoldLead">{{ formatGoldLead(match.teamGoldLeadAt15) }}</span>
        </div>
        <div class="value-cell" :class="enemyHasGoldLead ? 'positive' : 'empty'">
          <span v-if="enemyHasGoldLead">{{ formatGoldLead(-match.teamGoldLeadAt15) }}</span>
        </div>
      </div>

      <!-- Objective Control -->
      <div class="comparison-row">
        <span class="metric-label">Objectives</span>
        <div class="objectives-cell">
          <span class="obj-item" :title="`Dragons killed: ${match.teamDragons}`">
            <svg class="obj-icon dragon" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/></svg>
            <span class="obj-count">{{ match.teamDragons }}</span>
          </span>
          <span class="obj-item" :title="`Barons killed: ${match.teamBarons}`">
            <svg class="obj-icon baron" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
            <span class="obj-count">{{ match.teamBarons }}</span>
          </span>
          <span class="obj-item" :title="`Towers destroyed: ${match.teamTowers}`">
            <svg class="obj-icon tower" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2L4 5v6.09c0 5.05 3.41 9.76 8 10.91 4.59-1.15 8-5.86 8-10.91V5l-8-3zm6 9.09c0 4-2.55 7.7-6 8.83-3.45-1.13-6-4.82-6-8.83V6.31l6-2.25 6 2.25v4.78z"/></svg>
            <span class="obj-count">{{ match.teamTowers }}</span>
          </span>
        </div>
        <div class="objectives-cell">
          <span class="obj-item" :title="`Dragons killed: ${match.enemyTeamDragons}`">
            <svg class="obj-icon dragon" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/></svg>
            <span class="obj-count">{{ match.enemyTeamDragons }}</span>
          </span>
          <span class="obj-item" :title="`Barons killed: ${match.enemyTeamBarons}`">
            <svg class="obj-icon baron" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
            <span class="obj-count">{{ match.enemyTeamBarons }}</span>
          </span>
          <span class="obj-item" :title="`Towers destroyed: ${match.enemyTeamTowers}`">
            <svg class="obj-icon tower" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2L4 5v6.09c0 5.05 3.41 9.76 8 10.91 4.59-1.15 8-5.86 8-10.91V5l-8-3zm6 9.09c0 4-2.55 7.7-6 8.83-3.45-1.13-6-4.82-6-8.83V6.31l6-2.25 6 2.25v4.78z"/></svg>
            <span class="obj-count">{{ match.enemyTeamTowers }}</span>
          </span>
        </div>
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

// Damage bar percentages
const totalDamage = computed(() => props.match.teamTotalDamage + props.match.enemyTeamTotalDamage)
const teamDamagePercent = computed(() => 
  totalDamage.value > 0 ? (props.match.teamTotalDamage / totalDamage.value) * 100 : 50
)
const enemyDamagePercent = computed(() => 
  totalDamage.value > 0 ? (props.match.enemyTeamTotalDamage / totalDamage.value) * 100 : 50
)

// Gold lead - only show the positive side
const teamHasGoldLead = computed(() => {
  const gold = props.match.teamGoldLeadAt15
  return gold !== null && gold !== undefined && gold > 0
})
const enemyHasGoldLead = computed(() => {
  const gold = props.match.teamGoldLeadAt15
  return gold !== null && gold !== undefined && gold < 0
})

// Formatters
const formatNumber = (num) => {
  if (num >= 1000) return (num / 1000).toFixed(1) + 'k'
  return num?.toString() ?? '0'
}

const formatGoldLead = (gold) => {
  if (gold === null || gold === undefined) return 'N/A'
  const sign = gold >= 0 ? '+' : ''
  if (Math.abs(gold) >= 1000) return sign + (gold / 1000).toFixed(1) + 'k'
  return sign + gold
}
</script>

<style scoped>
.team-comparison {
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

.comparison-grid {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: var(--spacing-sm);
}

.comparison-row {
  display: grid;
  grid-template-columns: 100px 1fr 1fr;
  gap: var(--spacing-sm);
  align-items: center;
}

/* Damage row uses 4-column layout: label | bar | values | bar */
.comparison-row.damage-row {
  grid-template-columns: 100px 1fr auto 1fr;
}

.metric-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.bar-wrapper {
  height: 8px;
  background: var(--color-border);
  border-radius: 4px;
  overflow: hidden;
}

.bar-wrapper.team-bar .bar {
  float: right;
}

.bar {
  height: 100%;
  border-radius: 4px;
  transition: width 0.3s ease;
}

.bar.team { background: #3b82f6; }
.bar.enemy { background: #ef4444; }

.damage-values {
  display: flex;
  gap: var(--spacing-xs);
  justify-content: center;
}

.bar-value {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  min-width: 40px;
}

.team-value { color: #3b82f6; text-align: right; }
.enemy-value { color: #ef4444; text-align: left; }

.value-cell {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  text-align: center;
  padding: var(--spacing-xs);
  border-radius: var(--radius-sm);
}

.value-cell.positive { color: #22c55e; background: rgba(34, 197, 94, 0.1); }
.value-cell.negative { color: #ef4444; background: rgba(239, 68, 68, 0.1); }
.value-cell.neutral { color: var(--color-text-secondary); }
.value-cell.empty { background: transparent; }

.objectives-cell {
  display: flex;
  gap: var(--spacing-md);
  justify-content: center;
  align-items: center;
}

.obj-item {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: default;
}

.obj-icon {
  width: 18px;
  height: 18px;
}

.obj-icon.dragon { color: #f59e0b; }
.obj-icon.baron { color: #a855f7; }
.obj-icon.tower { color: #6b7280; }

.obj-count {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}
</style>

