<template>
  <SoloCard
    test-id="solo-stat-trends"
    title-id="solo-stat-trends-title"
    :title="title"
    :caption="titleIsCaption ? null : caption"
    error-title="Match-deciding stats"
    loading-label="Loading your stat trends"
    :loading="loading"
    :error="error"
    :empty="empty"
    @retry="$emit('retry')"
  >
    <template #skeleton>
      <BaseSkeleton variant="title" />
      <ul class="mp-stat-grid stat-trends__grid">
        <li v-for="n in 6" :key="n" class="mp-stat stat-trends__skeleton-tile">
          <BaseSkeleton width="50%" />
          <BaseSkeleton width="35%" height="1.5rem" />
          <BaseSkeleton variant="block" height="3.5rem" />
        </li>
      </ul>
    </template>

    <template #empty-action>
      <BaseButton
        v-if="empty?.showAllQueues"
        variant="secondary"
        data-testid="solo-stat-trends-show-all"
        @click="$emit('show-all-queues')"
      >Show all queues</BaseButton>
      <BaseButton v-else :disabled="syncing" data-testid="solo-stat-trends-sync" @click="$emit('sync')">
        <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
        Sync matches
      </BaseButton>
    </template>

    <template #aside>
      <ul class="stat-trends__key" aria-label="Chart key">
        <li><span class="stat-trends__key-line" aria-hidden="true" />Your average</li>
        <li><span class="stat-trends__key-dot" aria-hidden="true" />One match</li>
        <li v-if="benchmarkText"><span class="stat-trends__key-dash" aria-hidden="true" />{{ benchmarkText }}</li>
      </ul>
    </template>

    <ul class="mp-stat-grid stat-trends__grid" data-testid="solo-stat-trends-tiles">
      <BaseTrendTile
        v-for="tile in tiles"
        :key="tile.key"
        :label="tile.label"
        :value="tile.value"
        :unit="tile.unit"
        :was="tile.was"
        :verdict="tile.verdict.word"
        :tone="tile.verdict.tone"
        :arrow="tile.verdict.arrow"
        :values="tile.values"
        :rolling="tile.rolling"
        :length="data.matches"
        :benchmark="tile.benchmark"
        :description="tile.description"
        :data-testid="`trend-tile-${tile.key}`"
      />
    </ul>
  </SoloCard>
</template>

<script setup>
/**
 * "n of 6 match-deciding stats improved": six trend tiles (features/solo-trends.spec.md FR 13–17).
 */
import { computed } from 'vue'
import SoloCard from './SoloCard.vue'
import BaseTrendTile from '../base/BaseTrendTile.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import {
  STATS,
  benchmarkLabel,
  buildNoMatchesEmpty,
  buildStatTrendsCaption,
  buildStatTrendsTitle,
  describeStatTrend,
  formatStatNumber,
  statLabel,
  statVerdict
} from '@/utils/soloSummary'

const props = defineProps({
  /** stat-trends response, or null before the first load */
  data: { type: Object, default: null },
  /** Show the skeleton */
  loading: { type: Boolean, default: false },
  /** The last request failed */
  error: { type: Boolean, default: false },
  /** The selected queue, for the empty state */
  queue: { type: String, default: null },
  /** A sync is running (disables "Sync matches") */
  syncing: { type: Boolean, default: false }
})

defineEmits(['retry', 'show-all-queues', 'sync'])

const empty = computed(() => (props.data && props.data.matches === 0 ? buildNoMatchesEmpty(props.queue) : null))

const caption = computed(() => (props.data ? buildStatTrendsCaption(props.data.range, props.data.matches) : null))
const verdictTitle = computed(() => buildStatTrendsTitle(props.data?.stats))
// FR 17: with no verdicts at all, the caption becomes the title
const titleIsCaption = computed(() => !verdictTitle.value)
const title = computed(() => verdictTitle.value ?? caption.value ?? '')

const benchmarkText = computed(() => benchmarkLabel(props.data?.stats?.find((s) => s.benchmark)?.benchmark))

const tiles = computed(() =>
  (props.data?.stats ?? [])
    .filter((stat) => STATS[stat.key])
    .map((stat) => ({
      key: stat.key,
      label: statLabel(stat.key),
      value: formatStatNumber(stat.key, stat.now),
      unit: stat.now === null ? '' : STATS[stat.key].unit,
      was: stat.verdict ? formatStatNumber(stat.key, stat.was) : null,
      verdict: statVerdict(stat),
      values: stat.values,
      rolling: stat.rolling ?? [],
      benchmark: stat.benchmark?.value ?? null,
      description: describeStatTrend(stat, props.data.range, props.data.matches)
    }))
)
</script>

<style scoped>
.stat-trends__grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.stat-trends__skeleton-tile {
  gap: 0.5rem;
}

.stat-trends__key {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.stat-trends__key li {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
}

.stat-trends__key-line {
  width: 1rem;
  height: 3px;
  border-radius: 999px;
  background: var(--color-primary-accent);
}

.stat-trends__key-dot {
  width: 0.375rem;
  height: 0.375rem;
  border-radius: 999px;
  background: var(--color-track-strong);
}

.stat-trends__key-dash {
  width: 1rem;
  border-top: 1px dashed var(--color-text-secondary);
}

@media (max-width: 899px) {
  .stat-trends__grid {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
