<template>
  <div class="team-comparison">
    <div class="comparison-grid">
      <h3 class="section-title">Team Summary</h3>
      <!-- Total Damage -->
      <div class="comparison-row damage-row" v-if="hasDamageData">
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
      <div class="comparison-row" v-else>
        <span class="metric-label">Total Damage</span>
        <div class="value-cell empty">-</div>
        <div class="value-cell empty">-</div>
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
            <img :src="getObjectiveIconUrl('dragon', 'team')" alt="Dragon" class="obj-icon" />
            <span class="obj-count">{{ match.teamDragons }}</span>
          </span>
          <span class="obj-item" :title="`Barons killed: ${match.teamBarons}`">
            <img :src="getObjectiveIconUrl('baron', 'team')" alt="Baron" class="obj-icon" />
            <span class="obj-count">{{ match.teamBarons }}</span>
          </span>
          <span class="obj-item" :title="`Towers destroyed: ${match.teamTowers}`">
            <img :src="getObjectiveIconUrl('tower', 'team')" alt="Tower" class="obj-icon" />
            <span class="obj-count">{{ match.teamTowers }}</span>
          </span>
        </div>
        <div class="objectives-cell">
          <span class="obj-item" :title="`Dragons killed: ${match.enemyTeamDragons}`">
            <img :src="getObjectiveIconUrl('dragon', 'enemy')" alt="Dragon" class="obj-icon" />
            <span class="obj-count">{{ match.enemyTeamDragons }}</span>
          </span>
          <span class="obj-item" :title="`Barons killed: ${match.enemyTeamBarons}`">
            <img :src="getObjectiveIconUrl('baron', 'enemy')" alt="Baron" class="obj-icon" />
            <span class="obj-count">{{ match.enemyTeamBarons }}</span>
          </span>
          <span class="obj-item" :title="`Towers destroyed: ${match.enemyTeamTowers}`">
            <img :src="getObjectiveIconUrl('tower', 'enemy')" alt="Tower" class="obj-icon" />
            <span class="obj-count">{{ match.enemyTeamTowers }}</span>
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { formatNumber, formatGoldDiff as formatGoldLead } from '@/utils/formatters'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

// Community Dragon CDN for official League objective icons
const objectiveIconBaseUrl = 'https://raw.communitydragon.org/latest/plugins/rcp-fe-lol-match-history/global/default'

function getObjectiveIconUrl(objective, team) {
  // 100 = blue team (ally), 200 = red team (enemy)
  const teamSuffix = team === 'team' ? '100' : '200'
  return `${objectiveIconBaseUrl}/${objective}-${teamSuffix}.png`
}

// Check if damage data is available
const hasDamageData = computed(() => {
  const team = props.match.teamTotalDamage
  const enemy = props.match.enemyTeamTotalDamage
  return team != null && enemy != null && (team > 0 || enemy > 0)
})

// Damage bar percentages
const totalDamage = computed(() => (props.match.teamTotalDamage || 0) + (props.match.enemyTeamTotalDamage || 0))
const teamDamagePercent = computed(() =>
  totalDamage.value > 0 ? ((props.match.teamTotalDamage || 0) / totalDamage.value) * 100 : 50
)
const enemyDamagePercent = computed(() =>
  totalDamage.value > 0 ? ((props.match.enemyTeamTotalDamage || 0) / totalDamage.value) * 100 : 50
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

.bar.team { background: var(--color-info); }
.bar.enemy { background: var(--color-error); }

.damage-values {
  display: flex;
  gap: var(--spacing-sm);
  justify-content: center;
  align-items: center;
  white-space: nowrap;
}

.bar-value {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  min-width: 45px;
}

.team-value { color: var(--color-info); text-align: right; }
.enemy-value { color: var(--color-error); text-align: left; }

.value-cell {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  text-align: center;
  padding: var(--spacing-xs);
  border-radius: var(--radius-sm);
}

.value-cell.positive { color: var(--color-success); background: var(--color-success-soft); }
.value-cell.negative { color: var(--color-error); background: var(--color-error-soft); }
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
  width: 24px;
  height: 24px;
  object-fit: contain;
}

.obj-count {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}
</style>

