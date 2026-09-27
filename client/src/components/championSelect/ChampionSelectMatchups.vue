<template>
  <section
    class="mp-card matchups"
    aria-labelledby="matchups-title"
    :aria-busy="status === 'loading' ? 'true' : undefined"
    data-testid="champion-select-matchups"
  >
    <header class="matchups__header">
      <h2 id="matchups-title" class="mp-card-title">{{ championName }} in lane</h2>
      <p class="matchups__caption">Champions you've met {{ MIN_LANE_MATCHES }} or more times in lane</p>
    </header>

    <div v-if="status === 'loading'" class="matchups__columns" data-testid="matchups-loading">
      <span class="visually-hidden">Loading your matchups</span>
      <div v-for="n in 2" :key="n" class="matchups__column">
        <BaseSkeleton width="6rem" />
        <div v-for="row in 3" :key="row" class="matchups__row">
          <BaseSkeleton variant="block" width="2.75rem" height="2.75rem" class="matchups__icon" />
          <BaseSkeleton width="50%" />
        </div>
      </div>
    </div>

    <div v-else-if="status === 'error'" class="mp-message mp-message--error" role="alert" data-testid="matchups-error">
      <BaseIcon name="triangle-alert" :size="20" />
      <div class="mp-message__body">
        <p>We couldn't load your matchups. Try again in a minute.</p>
        <button type="button" class="mp-message__action" data-testid="matchups-retry" @click="$emit('retry')">Try again</button>
      </div>
    </div>

    <BaseEmptyState
      v-else-if="!strong.length && !weak.length"
      title="No lane matchups yet"
      :description="`Matchups show up once you've met a champion ${MIN_LANE_MATCHES} times in lane on ${championName}.`"
      data-testid="matchups-empty"
    />

    <div v-else class="matchups__columns">
      <div
        v-for="side in sides"
        :key="side.key"
        class="matchups__column"
        :data-testid="`matchups-${side.key}`"
      >
        <h3 class="matchups__side-title" :class="side.textClass">
          <BaseIcon :name="side.icon" :size="16" />
          {{ side.title }}
        </h3>
        <ul v-if="side.items.length" class="matchups__list">
          <li
            v-for="opponent in side.items"
            :key="opponent.championId"
            class="matchups__row"
            data-testid="matchups-row"
          >
            <img
              v-if="!failedIcons.has(opponent.championName)"
              :src="getChampionIconUrl(opponent.championName)"
              alt=""
              class="matchups__icon"
              @error="failedIcons.add(opponent.championName)"
            />
            <span v-else class="matchups__icon" aria-hidden="true" />
            <span class="matchups__name">
              <span class="matchups__champion">{{ opponent.championName }}</span>
              <span class="matchups__record">{{ formatRecord(opponent.wins, opponent.losses) }} in lane</span>
            </span>
            <span class="matchups__winrate" :class="side.textClass" data-testid="matchups-winrate">
              {{ Math.round(opponent.winRate) }}%<span class="visually-hidden"> lane win rate</span>
            </span>
          </li>
        </ul>
        <p v-else class="matchups__none" data-testid="matchups-none">{{ side.none }}</p>
      </div>
    </div>
  </section>
</template>

<script setup>
/**
 * Lane matchups for the selected pick on Champion Select: the opponents you beat most
 * (purple) and the ones you struggle into (orange), each with the lane record behind it.
 */
import { computed, reactive } from 'vue'
import BaseIcon from '../base/BaseIcon.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseEmptyState from '../base/BaseEmptyState.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import { formatRecord, MIN_LANE_MATCHES } from '@/utils/championSelectSummary'

const props = defineProps({
  /** The selected pick */
  championName: {
    type: String,
    required: true
  },
  /** Opponents you win into, best first */
  strong: {
    type: Array,
    default: () => []
  },
  /** Opponents you lose into, worst first */
  weak: {
    type: Array,
    default: () => []
  },
  /** loading, error or ready */
  status: {
    type: String,
    default: 'ready',
    validator: (value) => ['loading', 'error', 'ready'].includes(value)
  }
})

defineEmits(['retry'])

const failedIcons = reactive(new Set())

const sides = computed(() => [
  {
    key: 'strong',
    title: 'Strong into',
    icon: 'trending-up',
    textClass: 'mp-up',
    items: props.strong,
    none: 'No winning lane matchups yet.'
  },
  {
    key: 'weak',
    title: 'Weak into',
    icon: 'trending-down',
    textClass: 'mp-down',
    items: props.weak,
    none: 'No losing lane matchups yet.'
  }
])
</script>

<style scoped>
.matchups {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.matchups__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.matchups__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.matchups__columns {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.25rem;
}

.matchups__column {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-width: 0;
}

.matchups__side-title {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  padding-bottom: 0.25rem;
  font-size: 0.8125rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.matchups__list {
  list-style: none;
}

.matchups__row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 4rem;
  border-top: 1px solid var(--color-border);
}

.matchups__icon {
  width: 2.75rem;
  height: 2.75rem;
  flex-shrink: 0;
  border-radius: 0.75rem;
  background: var(--color-track);
}

.matchups__name {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  min-width: 0;
}

.matchups__champion {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
}

.matchups__record {
  font-size: 0.75rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.matchups__winrate {
  font-family: var(--font-display);
  font-size: 1.0625rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.matchups__none {
  padding-top: 0.75rem;
  border-top: 1px solid var(--color-border);
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

@media (max-width: 899px) {
  .matchups__columns {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
