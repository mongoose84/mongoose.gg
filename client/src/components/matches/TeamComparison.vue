<template>
  <section class="mp-card team" aria-labelledby="team-title" data-testid="team-comparison">
    <header class="team__header">
      <h3 id="team-title" class="mp-card-title" data-testid="team-title">{{ title }}</h3>
      <p class="team__caption" data-testid="team-gold-lead">{{ goldLeadLine }}</p>
    </header>

    <div v-if="hasDamageData" class="team__damage" data-testid="team-damage">
      <div class="team__damage-labels">
        <span><span class="team__key team__key--ally" aria-hidden="true" />Your team {{ formatNumber(match.teamTotalDamage) }}</span>
        <span>Enemy team {{ formatNumber(match.enemyTeamTotalDamage) }}<span class="team__key team__key--enemy" aria-hidden="true" /></span>
      </div>
      <div
        class="team__bar"
        role="img"
        :aria-label="`Damage: your team ${teamDamagePercent}%, enemy team ${100 - teamDamagePercent}%`"
      >
        <span class="team__bar-ally" :style="{ width: `${teamDamagePercent}%` }" />
        <span class="team__bar-enemy" />
      </div>
    </div>

    <table class="team__table" data-testid="team-objectives">
      <caption class="visually-hidden">Objectives taken by each team</caption>
      <thead>
        <tr>
          <th scope="col">Objective</th>
          <th scope="col">Your team</th>
          <th scope="col">Enemy team</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in objectives" :key="row.key" :data-testid="`team-objective-${row.key}`">
          <th scope="row">
            {{ row.label }}
          </th>
          <td :class="{ 'team__cell--lead': row.ally > row.enemy }">{{ row.ally }}</td>
          <td :class="{ 'team__cell--lead': row.enemy > row.ally }">{{ row.enemy }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
/**
 * Team summary for the open match: damage split, gold lead at 15 and objectives.
 * Your team is purple, the enemy team orange; every number is also written out.
 */
import { computed } from 'vue'
import { formatNumber } from '@/utils/formatters'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

const hasDamageData = computed(() => {
  const team = props.match.teamTotalDamage
  const enemy = props.match.enemyTeamTotalDamage
  return team != null && enemy != null && (team > 0 || enemy > 0)
})

const teamDamagePercent = computed(() => {
  const team = props.match.teamTotalDamage || 0
  const total = team + (props.match.enemyTeamTotalDamage || 0)
  return total > 0 ? Math.round((team / total) * 100) : 50
})

const title = computed(() =>
  hasDamageData.value ? `Your team dealt ${teamDamagePercent.value}% of the damage` : 'Team summary'
)

const goldLeadLine = computed(() => {
  const lead = props.match.teamGoldLeadAt15
  if (lead === null || lead === undefined) return 'No gold lead recorded at 15 minutes.'
  const amount = Math.abs(lead).toLocaleString('en-US')
  if (lead > 0) return `Your team led by ${amount} gold at 15 minutes.`
  if (lead < 0) return `The enemy team led by ${amount} gold at 15 minutes.`
  return 'Gold was even at 15 minutes.'
})

const objectives = computed(() => [
  { key: 'dragons', label: 'Dragons', ally: props.match.teamDragons ?? 0, enemy: props.match.enemyTeamDragons ?? 0 },
  { key: 'barons', label: 'Barons', ally: props.match.teamBarons ?? 0, enemy: props.match.enemyTeamBarons ?? 0 },
  { key: 'towers', label: 'Towers', ally: props.match.teamTowers ?? 0, enemy: props.match.enemyTeamTowers ?? 0 }
])
</script>

<style scoped>
.team {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.team__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.team__caption {
  font-size: 0.875rem;
  color: var(--color-ink-soft);
}

.team__damage {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.team__damage-labels {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.8125rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.team__damage-labels > span {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
}

.team__key {
  width: 0.625rem;
  height: 0.625rem;
  border-radius: 999px;
}

.team__key--ally { background: var(--color-primary-accent); }
.team__key--enemy { background: var(--color-warn); }

.team__bar {
  display: flex;
  gap: 0.25rem;
  height: 0.625rem;
}

.team__bar-ally,
.team__bar-enemy {
  border-radius: 999px;
}

.team__bar-ally { background: var(--color-primary-accent); }
.team__bar-enemy { flex-grow: 1; background: var(--color-warn); }

.team__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.team__table th,
.team__table td {
  padding: 0.75rem 0;
  border-top: 1px solid var(--color-border);
  text-align: right;
}

.team__table thead th {
  border-top: none;
  padding-top: 0;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.team__table th:first-child {
  text-align: left;
}

.team__table tbody th {
  font-weight: 500;
  color: var(--color-ink-soft);
}

.team__table td {
  font-family: var(--font-display);
  font-size: 1.0625rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.team__table td.team__cell--lead {
  color: var(--color-text);
}
</style>
