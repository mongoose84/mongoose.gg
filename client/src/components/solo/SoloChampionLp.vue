<template>
  <SoloCard
    test-id="solo-champion-lp"
    title-id="solo-champion-lp-title"
    :title="title"
    :caption="caption"
    error-title="Your champions"
    loading-label="Loading your champions"
    :loading="loading"
    :error="error"
    :empty="empty"
    @retry="$emit('retry')"
  >
    <template #skeleton>
      <BaseSkeleton variant="title" />
      <div v-for="n in 4" :key="n" class="champion-lp__skeleton-row">
        <BaseSkeleton variant="block" width="2.25rem" height="2.25rem" />
        <BaseSkeleton variant="block" height="0.75rem" />
      </div>
    </template>

    <template #empty-action>
      <BaseButton :disabled="syncing" data-testid="solo-champion-lp-sync" @click="$emit('sync')">
        <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
        Sync matches
      </BaseButton>
    </template>

    <ul class="champion-lp__rows" data-testid="solo-champion-lp-rows">
      <li
        v-for="champion in data.champions"
        :key="champion.championId"
        class="champion-lp__row"
        :data-testid="`champion-lp-${champion.championId}`"
      >
        <img :src="getChampionIconUrl(champion.championName)" alt="" class="champion-lp__icon" />
        <div class="champion-lp__name">
          <span class="champion-lp__champion">{{ champion.championName }}</span>
          <span class="champion-lp__meta">{{ meta(champion) }}</span>
        </div>
        <BaseDivergingBar :value="champion.value" :max="maxValue" :description="describeChampionLp(champion, data.mode)" />
        <span
          class="champion-lp__value"
          :class="{ 'mp-up': champion.value > 0, 'mp-down': champion.value < 0 }"
          data-testid="champion-lp-value"
        >{{ formatChampionValue(champion.value, data.mode) }}</span>
      </li>
    </ul>
    <p v-if="leftOut" class="champion-lp__note" data-testid="solo-champion-lp-left-out">{{ leftOut }}</p>
  </SoloCard>
</template>

<script setup>
/**
 * LP per champion (features/solo-trends.spec.md FR 24–25): up to five champions with 3 or more
 * matches, LP won or lost (or net wins) as bars around zero, and the names left out.
 */
import { computed } from 'vue'
import SoloCard from './SoloCard.vue'
import BaseDivergingBar from '../base/BaseDivergingBar.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import {
  buildChampionLpCaption,
  buildChampionLpTitle,
  buildLeftOutNote,
  buildNoMatchesEmpty,
  describeChampionLp,
  formatChampionValue
} from '@/utils/soloSummary'

const props = defineProps({
  /** climb response (champions live there), or null before the first load */
  data: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: Boolean, default: false },
  queue: { type: String, default: null },
  syncing: { type: Boolean, default: false }
})

defineEmits(['retry', 'sync'])

const empty = computed(() => {
  if (!props.data) return null
  if (props.data.matches === 0) return buildNoMatchesEmpty(props.queue)
  if (!props.data.champions.length) {
    return {
      title: 'No champion has 3 matches in this range yet',
      description: 'A champion shows up here once you have played 3 matches on it.'
    }
  }
  return null
})

const title = computed(() => buildChampionLpTitle(props.data) ?? '')
const caption = computed(() => (props.data ? buildChampionLpCaption(props.data) : null))
const leftOut = computed(() => buildLeftOutNote(props.data?.championsLeftOut))
const maxValue = computed(() => Math.max(1, ...(props.data?.champions ?? []).map((c) => Math.abs(c.value))))

function meta(champion) {
  const rate = Math.round((champion.wins / champion.matches) * 100)
  return `${champion.matches} matches · ${rate}% win rate`
}
</script>

<style scoped>
.champion-lp__rows {
  margin: 0;
  padding: 0;
  list-style: none;
}

.champion-lp__row,
.champion-lp__skeleton-row {
  display: grid;
  grid-template-columns: 2.25rem minmax(0, 9rem) minmax(0, 1fr) 4.5rem;
  align-items: center;
  gap: 0.75rem;
  min-height: 3.5rem;
  border-top: 1px solid var(--color-border);
}

.champion-lp__skeleton-row {
  grid-template-columns: 2.25rem minmax(0, 1fr);
}

.champion-lp__icon {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.75rem;
}

.champion-lp__name {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.champion-lp__champion {
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
}

.champion-lp__meta {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
  white-space: nowrap;
}

.champion-lp__value {
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  text-align: right;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.champion-lp__note {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

@media (max-width: 599px) {
  .champion-lp__row {
    grid-template-columns: 2.25rem minmax(0, 1fr) 4.5rem;
  }

  .champion-lp__value {
    grid-row: 1;
    grid-column: 3;
  }

  .champion-lp__row :deep(.diverging-bar) {
    grid-row: 2;
    grid-column: 2 / -1;
    margin-bottom: 0.5rem;
  }
}
</style>
