<template>
  <section class="mp-card team" aria-labelledby="team-title" data-testid="team-comparison">
    <header class="team__header">
      <h3 id="team-title" class="mp-card-title" data-testid="team-title">{{ title }}</h3>
      <p class="team__caption" data-testid="team-gold-lead">{{ goldLeadLine }}</p>
    </header>

    <ul class="team__splits">
      <li
        v-for="row in rows"
        :key="row.key"
        class="team__split"
        :data-testid="`team-split-${row.key}`"
      >
        <div class="team__split-labels">
          <span class="team__number" :class="{ 'team__number--lead': row.ally > row.enemy }" data-testid="team-split-ally">{{ row.allyText }}</span>
          <span class="team__label">{{ row.label }}</span>
          <span class="team__number" :class="{ 'team__number--lead': row.enemy > row.ally }" data-testid="team-split-enemy">{{ row.enemyText }}</span>
        </div>
        <div
          v-if="row.total > 0"
          class="mp-split"
          role="img"
          :aria-label="row.ariaLabel"
          data-testid="team-split-bar"
        >
          <span :style="{ width: `${row.allyPercent}%` }" />
          <span />
        </div>
        <div
          v-else
          class="team__split-empty"
          role="img"
          :aria-label="row.ariaLabel"
          data-testid="team-split-bar"
        />
      </li>
    </ul>

    <p class="team__key" aria-hidden="true">
      <span><span class="team__dot team__dot--ally" />Your team</span>
      <span>Enemy team<span class="team__dot team__dot--enemy" /></span>
    </p>
  </section>
</template>

<script setup>
/**
 * Team summary for the open match: SplitBars for damage, dragons, barons and towers, your team
 * (purple, left) against the enemy team (orange, right), with every number written out. The
 * gold lead at 15 is the caption (team gold totals aren't in the API).
 */
import { computed } from 'vue'

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

function share(ally, enemy) {
  const total = ally + enemy
  return total > 0 ? Math.round((ally / total) * 100) : 50
}

/** 54,200 → "54.2k"; small counts stay whole */
function compact(value) {
  if (value >= 1000) return `${(value / 1000).toFixed(1)}k`
  return String(value)
}

const teamDamagePercent = computed(() =>
  share(props.match.teamTotalDamage || 0, props.match.enemyTeamTotalDamage || 0)
)

const title = computed(() =>
  hasDamageData.value ? `Your team dealt ${teamDamagePercent.value}% of the damage` : 'Your team vs theirs'
)

const goldLeadLine = computed(() => {
  const lead = props.match.teamGoldLeadAt15
  if (lead === null || lead === undefined) return 'No gold lead recorded at 15 minutes.'
  const amount = Math.abs(lead).toLocaleString('en-US')
  if (lead > 0) return `Your team led by ${amount} gold at 15 minutes.`
  if (lead < 0) return `The enemy team led by ${amount} gold at 15 minutes.`
  return 'Gold was even at 15 minutes.'
})

function buildRow(key, label, ally, enemy, { percentInLabel = false } = {}) {
  const total = ally + enemy
  const allyPercent = share(ally, enemy)
  let ariaLabel
  if (!total) ariaLabel = `${label}: none taken`
  else if (percentInLabel) ariaLabel = `${label}: your team ${allyPercent}%, enemy team ${100 - allyPercent}%`
  else ariaLabel = `${label}: your team ${ally}, enemy team ${enemy}`
  return {
    key,
    label,
    ally,
    enemy,
    total,
    allyPercent,
    allyText: percentInLabel ? compact(ally) : String(ally),
    enemyText: percentInLabel ? compact(enemy) : String(enemy),
    ariaLabel
  }
}

const rows = computed(() => {
  const m = props.match
  const list = []
  if (hasDamageData.value) {
    list.push(buildRow('damage', 'Damage', m.teamTotalDamage || 0, m.enemyTeamTotalDamage || 0, { percentInLabel: true }))
  }
  list.push(
    buildRow('dragons', 'Dragons', m.teamDragons ?? 0, m.enemyTeamDragons ?? 0),
    buildRow('barons', 'Barons', m.teamBarons ?? 0, m.enemyTeamBarons ?? 0),
    buildRow('towers', 'Towers', m.teamTowers ?? 0, m.enemyTeamTowers ?? 0)
  )
  return list
})
</script>

<style scoped>
.team {
  display: flex;
  flex-direction: column;
  gap: 1.125rem;
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

.team__splits {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  list-style: none;
}

.team__split {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.team__split-labels {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
}

.team__label {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.team__number {
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.team__number--lead {
  color: var(--color-text);
}

/* Nothing taken by either team: an empty track, never a made-up split */
.team__split-empty {
  height: 0.625rem;
  border-radius: 999px;
  background: var(--color-track-strong);
}

.team__key {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.team__key > span {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
}

.team__dot {
  width: 0.625rem;
  height: 0.625rem;
  border-radius: 999px;
}

.team__dot--ally { background: var(--color-primary-accent); }
.team__dot--enemy { background: var(--color-warn); }
</style>
